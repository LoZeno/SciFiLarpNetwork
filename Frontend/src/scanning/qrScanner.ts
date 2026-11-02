import { BrowserCodeReader, BrowserQRCodeReader, type IScannerControls } from '@zxing/browser';
import { ChecksumException, FormatException, NotFoundException } from '@zxing/library';

export interface QrCamera {
  readonly deviceId: string;
  readonly label: string;
}

export interface QrScanSession {
  stop(): void;
}

export type CameraAvailability = 'available' | 'insecure-context' | 'unsupported';

export interface QrScanner {
  getCameraAvailability(): CameraAvailability;
  listCameras(): Promise<readonly QrCamera[]>;
  startCamera(
    video: HTMLVideoElement,
    deviceId: string | undefined,
    onDecoded: (value: string) => void,
    onError: (error: Error) => void,
  ): Promise<QrScanSession>;
  decodeImage(file: File): Promise<string>;
}

function isRetryableDecodeError(error: Error): boolean {
  return (
    error instanceof NotFoundException ||
    error instanceof ChecksumException ||
    error instanceof FormatException
  );
}

function createSession(controls: IScannerControls): QrScanSession {
  return { stop: () => controls.stop() };
}

export function createBrowserQrScanner(): QrScanner {
  const reader = new BrowserQRCodeReader();

  return {
    getCameraAvailability() {
      if (typeof window === 'undefined' || !window.isSecureContext) {
        return 'insecure-context';
      }

      if (
        typeof navigator === 'undefined' ||
        typeof navigator.mediaDevices?.getUserMedia !== 'function'
      ) {
        return 'unsupported';
      }

      return 'available';
    },

    async listCameras() {
      const devices = await BrowserCodeReader.listVideoInputDevices();
      return devices.map((device) => ({
        deviceId: device.deviceId,
        label: device.label || 'Camera',
      }));
    },

    async startCamera(video, deviceId, onDecoded, onError) {
      const controls = await reader.decodeFromVideoDevice(deviceId, video, (result, error) => {
        if (result !== undefined) {
          onDecoded(result.getText());
        } else if (error !== undefined && !isRetryableDecodeError(error)) {
          onError(error);
        }
      });

      return createSession(controls);
    },

    async decodeImage(file) {
      const imageUrl = URL.createObjectURL(file);
      try {
        const result = await reader.decodeFromImageUrl(imageUrl);
        return result.getText();
      } finally {
        URL.revokeObjectURL(imageUrl);
      }
    },
  };
}
