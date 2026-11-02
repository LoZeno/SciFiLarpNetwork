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
  it('distinguishes secure camera support from insecure and unsupported contexts', () => {
    const scanner = createBrowserQrScanner();
    Object.defineProperty(window, 'isSecureContext', { configurable: true, value: false });
    expect(scanner.getCameraAvailability()).toBe('insecure-context');

    Object.defineProperty(window, 'isSecureContext', { configurable: true, value: true });
    vi.stubGlobal('navigator', {});
    expect(scanner.getCameraAvailability()).toBe('unsupported');

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

  it('passes camera selection through and forwards decoded strings without retry noise', async () => {
    const controls: IScannerControls = { stop: vi.fn() };
    let decode: DecodeCallback | undefined;
    const start = vi
      .spyOn(BrowserQRCodeReader.prototype, 'decodeFromVideoDevice')
      .mockImplementation(async (_deviceId, _video, callback) => {
        decode = callback;
        return controls;
      });
    const video = document.createElement('video');
    const onDecoded = vi.fn();
    const onError = vi.fn();
    const session = await createBrowserQrScanner().startCamera(
      video,
      'rear-camera',
      onDecoded,
      onError,
    );

    expect(start).toHaveBeenCalledWith('rear-camera', video, expect.any(Function));
    const result = new Result('untrusted-code', new Uint8Array(), 0, [], BarcodeFormat.QR_CODE);
    decode?.(result, undefined, controls);
    decode?.(undefined, NotFoundException.getNotFoundInstance(), controls);

    expect(onDecoded).toHaveBeenCalledOnce();
    expect(onDecoded).toHaveBeenCalledWith('untrusted-code');
    expect(onError).not.toHaveBeenCalled();
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

    await expect(createBrowserQrScanner().decodeImage(image)).resolves.toBe('untrusted-image-code');
    expect(createUrl).toHaveBeenCalledWith(image);
    expect(decodeImage).toHaveBeenCalledWith('blob:local-image');
    expect(revokeUrl).toHaveBeenCalledWith('blob:local-image');
  });

  it('revokes the image URL when decoding fails', async () => {
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:unreadable');
    const revokeUrl = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
    vi.spyOn(BrowserQRCodeReader.prototype, 'decodeFromImageUrl').mockRejectedValue(
      new Error('Unreadable image.'),
    );

    await expect(
      createBrowserQrScanner().decodeImage(new File(['image'], 'code.png', { type: 'image/png' })),
    ).rejects.toThrow('Unreadable image.');
    expect(revokeUrl).toHaveBeenCalledWith('blob:unreadable');
  });
});
