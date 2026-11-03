import { useEffect, useRef, useState, type ChangeEvent } from 'react';
import { useLocale } from '../i18n/LocaleProvider';
import {
  createBrowserQrScanner,
  type CameraAvailability,
  type QrCamera,
  type QrScanner,
  type QrScanSession,
} from './qrScanner';

type CaptureState = 'idle' | 'starting' | 'scanning' | 'captured';

function cameraErrorKey(error: unknown): string {
  if (error instanceof DOMException) {
    if (error.name === 'NotAllowedError' || error.name === 'PermissionDeniedError') {
      return 'scanner.permissionDenied';
    }

    if (error.name === 'NotFoundError' || error.name === 'DevicesNotFoundError') {
      return 'scanner.noCamera';
    }
  }

  return 'scanner.cameraUnavailable';
}

export function QrCapture({
  onScan,
  scanner,
}: {
  readonly onScan: (value: string) => void;
  readonly scanner?: QrScanner;
}) {
  const { t } = useLocale();
  const [scannerAdapter] = useState(() => scanner ?? createBrowserQrScanner());
  const videoRef = useRef<HTMLVideoElement>(null);
  const sessionRef = useRef<QrScanSession | null>(null);
  const generationRef = useRef(0);
  const decodedRef = useRef(false);
  const decodePendingRef = useRef(false);
  const [captureState, setCaptureState] = useState<CaptureState>('idle');
  const [availability, setAvailability] = useState<CameraAvailability>(() =>
    scannerAdapter.getCameraAvailability(),
  );
  const [cameras, setCameras] = useState<readonly QrCamera[]>([]);
  const [selectedCameraId, setSelectedCameraId] = useState('');
  const [manualValue, setManualValue] = useState('');
  const [statusKey, setStatusKey] = useState<string | null>(null);
  const [processingImage, setProcessingImage] = useState(false);

  function releaseCamera() {
    sessionRef.current?.stop();
    sessionRef.current = null;
    const video = videoRef.current;
    const stream = video?.srcObject as MediaStream | null;
    stream?.getTracks().forEach((track) => track.stop());
    if (video !== null) {
      video.srcObject = null;
    }
  }

  useEffect(
    () => () => {
      generationRef.current += 1;
      sessionRef.current?.stop();
      const video = videoRef.current;
      const stream = video?.srcObject as MediaStream | null;
      stream?.getTracks().forEach((track) => track.stop());
      if (video !== null) {
        video.srcObject = null;
      }
    },
    [],
  );

  function stopCamera() {
    generationRef.current += 1;
    releaseCamera();
    setCaptureState('idle');
  }

  function handleDecoded(value: string) {
    if (decodedRef.current) {
      return;
    }

    decodedRef.current = true;
    generationRef.current += 1;
    releaseCamera();
    setCaptureState('captured');
    setStatusKey('scanner.captured');
    setManualValue('');
    onScan(value);
  }

  async function startCamera(deviceId: string | undefined = undefined) {
    stopCamera();
    const generation = generationRef.current;
    decodedRef.current = false;
    setStatusKey(null);
    const currentAvailability = scannerAdapter.getCameraAvailability();
    setAvailability(currentAvailability);
    if (currentAvailability !== 'available') {
      setStatusKey(
        currentAvailability === 'insecure-context'
          ? 'scanner.secureContextRequired'
          : 'scanner.unsupported',
      );
      return;
    }

    const video = videoRef.current;
    if (video === null) {
      return;
    }

    setCaptureState('starting');
    try {
      const session = await scannerAdapter.startCamera(
        video,
        deviceId,
        (value) => {
          if (generationRef.current === generation) {
            handleDecoded(value);
          }
        },
        (error) => {
          if (generationRef.current === generation) {
            generationRef.current += 1;
            releaseCamera();
            setCaptureState('idle');
            setStatusKey(cameraErrorKey(error));
          }
        },
      );
      if (generationRef.current !== generation) {
        session.stop();
        return;
      }

      sessionRef.current = session;
      setCaptureState('scanning');
      try {
        const availableCameras = await scannerAdapter.listCameras();
        if (generationRef.current === generation) {
          setCameras(availableCameras);
        }
      } catch {
        if (generationRef.current === generation) {
          setStatusKey('scanner.cameraListUnavailable');
        }
      }
    } catch (error: unknown) {
      if (generationRef.current === generation) {
        releaseCamera();
        setCaptureState('idle');
        setStatusKey(cameraErrorKey(error));
      }
    }
  }

  async function handleImageChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.currentTarget.files?.[0];
    event.currentTarget.value = '';
    if (file === undefined || decodedRef.current || decodePendingRef.current) {
      return;
    }

    if (!file.type.startsWith('image/')) {
      setStatusKey('scanner.invalidImage');
      return;
    }

    stopCamera();
    const generation = generationRef.current;
    decodePendingRef.current = true;
    setProcessingImage(true);
    setStatusKey(null);
    let value: string;
    try {
      value = await scannerAdapter.decodeImage(file);
    } catch {
      if (generationRef.current === generation) {
        decodePendingRef.current = false;
        setProcessingImage(false);
        setStatusKey('scanner.unreadable');
      }
      return;
    }

    if (generationRef.current !== generation) {
      return;
    }

    decodePendingRef.current = false;
    setProcessingImage(false);
    handleDecoded(value);
  }

  function useManualValue() {
    if (manualValue.trim().length === 0 || decodedRef.current || decodePendingRef.current) {
      return;
    }

    handleDecoded(manualValue);
  }

  return (
    <section className="qr-capture" aria-labelledby="scanner-heading">
      <h2 id="scanner-heading">{t('scanner.heading')}</h2>
      <p className="scanner-description">{t('scanner.description')}</p>

      <div className="scanner-camera">
        <video
          ref={videoRef}
          aria-label={t('scanner.cameraPreview')}
          autoPlay
          muted
          playsInline
          hidden={captureState !== 'scanning' && captureState !== 'starting'}
        />
        {captureState === 'idle' && (
          <button
            className="button button-secondary"
            type="button"
            disabled={availability !== 'available' || processingImage}
            onClick={() => void startCamera(selectedCameraId || undefined)}
          >
            {t('scanner.startCamera')}
          </button>
        )}
        {(captureState === 'starting' || processingImage) && (
          <p role="status">
            {processingImage ? t('scanner.decodingImage') : t('scanner.starting')}
          </p>
        )}
        {captureState === 'scanning' && (
          <div className="scanner-controls">
            {cameras.length > 1 && (
              <label className="field">
                <span>{t('scanner.selectCamera')}</span>
                <select
                  aria-label={t('scanner.selectCamera')}
                  value={selectedCameraId}
                  onChange={(event) => {
                    setSelectedCameraId(event.currentTarget.value);
                    void startCamera(event.currentTarget.value || undefined);
                  }}
                >
                  <option value="">{t('scanner.defaultCamera')}</option>
                  {cameras.map((camera) => (
                    <option key={camera.deviceId} value={camera.deviceId}>
                      {camera.label}
                    </option>
                  ))}
                </select>
              </label>
            )}
            <button className="button button-secondary" type="button" onClick={stopCamera}>
              {t('scanner.stopCamera')}
            </button>
          </div>
        )}
        {captureState === 'captured' && (
          <button
            className="button button-secondary"
            type="button"
            onClick={() => {
              decodedRef.current = false;
              setStatusKey(null);
              setCaptureState('idle');
            }}
          >
            {t('scanner.rescan')}
          </button>
        )}
      </div>

      {availability === 'insecure-context' && (
        <p className="scanner-hint">{t('scanner.secureContextRequired')}</p>
      )}
      {availability === 'unsupported' && <p className="scanner-hint">{t('scanner.unsupported')}</p>}
      {statusKey !== null && (
        <p className="scanner-status" role="status">
          {t(statusKey)}
        </p>
      )}

      <label className="field">
        <span>{t('scanner.imageLabel')}</span>
        <input
          type="file"
          accept="image/*"
          disabled={processingImage || captureState === 'captured'}
          onChange={(event) => void handleImageChange(event)}
        />
      </label>

      <div className="scanner-manual-entry">
        <label className="field">
          <span>{t('scanner.manualLabel')}</span>
          <input
            type="password"
            autoComplete="off"
            spellCheck={false}
            value={manualValue}
            disabled={processingImage || captureState === 'captured'}
            onChange={(event) => setManualValue(event.currentTarget.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                event.preventDefault();
              }
            }}
          />
        </label>
        <button
          className="button button-primary"
          type="button"
          disabled={manualValue.trim().length === 0 || captureState === 'captured'}
          onClick={useManualValue}
        >
          {t('scanner.useCode')}
        </button>
      </div>
    </section>
  );
}
