import { BrowserCodeReader, BrowserQRCodeReader, type IScannerControls } from '@zxing/browser';
import { BarcodeFormat, Exception, NotFoundException, Result } from '@zxing/library';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { createBrowserQrScanner } from '../../src/scanning/qrScanner';

type DecodeCallback = Parameters<BrowserQRCodeReader['decodeFromVideoDevice']>[2];

const secureContextDescriptor = Object.getOwnPropertyDescriptor(window, 'isSecureContext');

afterEach(() => {
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
  if (secureContextDescriptor === undefined) {
    Reflect.deleteProperty(window, 'isSecureContext');
  } else {
    Object.defineProperty(window, 'isSecureContext', secureContextDescriptor);
  }
});

describe('browser QR scanner adapter', () => {
  it('reports an insecure context as unavailable for camera use', () => {
    const scanner = createBrowserQrScanner();
    Object.defineProperty(window, 'isSecureContext', { configurable: true, value: false });

    expect(scanner.getCameraAvailability()).toBe('insecure-context');
  });

  it('reports missing media-device support as unsupported', () => {
    const scanner = createBrowserQrScanner();
    Object.defineProperty(window, 'isSecureContext', { configurable: true, value: true });
    vi.stubGlobal('navigator', {});

    expect(scanner.getCameraAvailability()).toBe('unsupported');
  });

  it('reports camera support when secure media devices are available', () => {
    const scanner = createBrowserQrScanner();
    Object.defineProperty(window, 'isSecureContext', { configurable: true, value: true });
    vi.stubGlobal('navigator', {
      mediaDevices: { getUserMedia: vi.fn() },
    });

    expect(scanner.getCameraAvailability()).toBe('available');
  });

  it('lists only the browser camera device information needed by the UI', async () => {
    vi.spyOn(BrowserCodeReader, 'listVideoInputDevices').mockResolvedValue([
      { deviceId: 'rear-camera', label: 'Rear camera' } as MediaDeviceInfo,
    ]);

    await expect(createBrowserQrScanner().listCameras()).resolves.toEqual([
      { deviceId: 'rear-camera', label: 'Rear camera' },
    ]);
  });

  it('passes the selected camera through to the decoder', async () => {
    const start = vi
      .spyOn(BrowserQRCodeReader.prototype, 'decodeFromVideoDevice')
      .mockResolvedValue({ stop: vi.fn() });
    const video = document.createElement('video');
    await createBrowserQrScanner().startCamera(video, 'rear-camera', vi.fn(), vi.fn());

    expect(start).toHaveBeenCalledWith('rear-camera', video, expect.any(Function));
  });

  it('forwards decoded strings and ignores retry-noise errors', async () => {
    const controls: IScannerControls = { stop: vi.fn() };
    let decode: DecodeCallback | undefined;
    vi.spyOn(BrowserQRCodeReader.prototype, 'decodeFromVideoDevice').mockImplementation(
      async (_deviceId, _video, callback) => {
        decode = callback;
        return controls;
      },
    );
    const onDecoded = vi.fn();
    const onError = vi.fn();
    await createBrowserQrScanner().startCamera(
      document.createElement('video'),
      'rear-camera',
      onDecoded,
      onError,
    );
    const result = new Result('untrusted-code', new Uint8Array(), 0, [], BarcodeFormat.QR_CODE);
    decode?.(result, undefined, controls);
    decode?.(undefined, NotFoundException.getNotFoundInstance(), controls);

    expect({ decoded: onDecoded.mock.calls, errors: onError.mock.calls }).toEqual({
      decoded: [['untrusted-code']],
      errors: [],
    });
  });

  it('stops the browser controls when its scan session is stopped', async () => {
    const controls: IScannerControls = { stop: vi.fn() };
    vi.spyOn(BrowserQRCodeReader.prototype, 'decodeFromVideoDevice').mockResolvedValue(controls);
    const session = await createBrowserQrScanner().startCamera(
      document.createElement('video'),
      'rear-camera',
      vi.fn(),
      vi.fn(),
    );

    session.stop();

    expect(controls.stop).toHaveBeenCalledOnce();
  });

  it('forwards terminal decoder errors to the scanner view', async () => {
    const controls: IScannerControls = { stop: vi.fn() };
    let decode: DecodeCallback | undefined;
    vi.spyOn(BrowserQRCodeReader.prototype, 'decodeFromVideoDevice').mockImplementation(
      async (_deviceId, _video, callback) => {
        decode = callback;
        return controls;
      },
    );
    const onError = vi.fn();
    await createBrowserQrScanner().startCamera(
      document.createElement('video'),
      undefined,
      vi.fn(),
      onError,
    );

    const error = new Exception('Decoder failed.');
    decode?.(undefined, error, controls);

    expect(onError).toHaveBeenCalledWith(error);
  });

  it('decodes an image locally and always revokes its temporary object URL', async () => {
    const createUrl = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:local-image');
    const revokeUrl = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
    const decodeImage = vi
      .spyOn(BrowserQRCodeReader.prototype, 'decodeFromImageUrl')
      .mockResolvedValue(
        new Result('untrusted-image-code', new Uint8Array(), 0, [], BarcodeFormat.QR_CODE),
      );
    const image = new File(['image'], 'code.png', { type: 'image/png' });

    const decoded = await createBrowserQrScanner().decodeImage(image);

    expect({
      decoded,
      createdUrlFor: createUrl.mock.calls[0]?.[0],
      decodedUrl: decodeImage.mock.calls[0]?.[0],
      revokedUrl: revokeUrl.mock.calls[0]?.[0],
    }).toEqual({
      decoded: 'untrusted-image-code',
      createdUrlFor: image,
      decodedUrl: 'blob:local-image',
      revokedUrl: 'blob:local-image',
    });
  });

  it('revokes the image URL when decoding fails', async () => {
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:unreadable');
    const revokeUrl = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
    vi.spyOn(BrowserQRCodeReader.prototype, 'decodeFromImageUrl').mockRejectedValue(
      new Error('Unreadable image.'),
    );

    let errorMessage: string | undefined;
    try {
      await createBrowserQrScanner().decodeImage(
        new File(['image'], 'code.png', { type: 'image/png' }),
      );
    } catch (error) {
      errorMessage = error instanceof Error ? error.message : String(error);
    }

    expect({ errorMessage, revokedUrl: revokeUrl.mock.calls[0]?.[0] }).toEqual({
      errorMessage: 'Unreadable image.',
      revokedUrl: 'blob:unreadable',
    });
  });
});
