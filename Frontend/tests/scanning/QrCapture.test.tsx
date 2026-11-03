import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { LocaleProvider } from '../../src/i18n/LocaleProvider';
import { QrCapture } from '../../src/scanning/QrCapture';
import type { CameraAvailability, QrScanner, QrScanSession } from '../../src/scanning/qrScanner';

function createScanner(
  options: {
    readonly availability?: CameraAvailability;
    readonly cameras?: Awaited<ReturnType<QrScanner['listCameras']>>;
    readonly startError?: Error;
    readonly imageError?: Error;
  } = {},
) {
  let decodeCameraResult: ((value: string) => void) | undefined;
  const session: QrScanSession = { stop: vi.fn() };
  const scanner: QrScanner = {
    getCameraAvailability: () => options.availability ?? 'available',
    listCameras: vi.fn(async () => options.cameras ?? []),
    startCamera: vi.fn(async (_video, _deviceId, onDecoded) => {
      if (options.startError !== undefined) {
        throw options.startError;
      }

      decodeCameraResult = onDecoded;
      return session;
    }),
    decodeImage: vi.fn(async () => {
      if (options.imageError !== undefined) {
        throw options.imageError;
      }

      return 'image-code';
    }),
  };

  return { scanner, session, decodeCameraResult: () => decodeCameraResult };
}

function renderCapture(scanner: QrScanner, onScan: (value: string) => void = vi.fn()) {
  return render(
    <LocaleProvider>
      <QrCapture onScan={onScan} scanner={scanner} />
    </LocaleProvider>,
  );
}

describe('QR capture', () => {
  afterEach(() => {
    cleanup();
  });

  it('starts on explicit request and immediately emits only the first camera decode', async () => {
    const onScan = vi.fn();
    const { scanner, session, decodeCameraResult } = createScanner();
    renderCapture(scanner, onScan);

    expect(scanner.startCamera).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Start camera' }));
    await waitFor(() => expect(scanner.startCamera).toHaveBeenCalledOnce());
    await waitFor(() => expect(decodeCameraResult()).toBeTypeOf('function'));

    act(() => {
      decodeCameraResult()?.('first-code');
      decodeCameraResult()?.('duplicate-code');
    });

    expect(onScan).toHaveBeenCalledOnce();
    expect(onScan).toHaveBeenCalledWith('first-code');
    expect(session.stop).toHaveBeenCalledOnce();
    expect(screen.getByRole('status').textContent).toContain('Code captured');
    expect(screen.getByRole('button', { name: 'Scan another code' })).toBeTruthy();
  });

  it.each([
    ['NotAllowedError', 'Camera permission was denied'],
    ['NotFoundError', 'No camera was found'],
  ])('explains camera startup failure: %s', async (errorName, message) => {
    const { scanner } = createScanner({ startError: new DOMException('', errorName) });
    renderCapture(scanner);

    fireEvent.click(screen.getByRole('button', { name: 'Start camera' }));

    expect(await screen.findByText(new RegExp(message))).toBeTruthy();
  });

  it('explains insecure-context and unsupported-browser limitations', () => {
    const insecure = createScanner({ availability: 'insecure-context' });
    const { unmount } = renderCapture(insecure.scanner);

    expect(screen.getByText(/Camera access requires HTTPS or localhost/)).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Start camera' })).toHaveProperty('disabled', true);
    unmount();

    const unsupported = createScanner({ availability: 'unsupported' });
    renderCapture(unsupported.scanner);
    expect(screen.getByText(/Camera access is not supported/)).toBeTruthy();
  });

  it('stops the camera on cancellation and component disposal', async () => {
    const { scanner, session } = createScanner({
      cameras: [
        { deviceId: 'front', label: 'Front camera' },
        { deviceId: 'rear', label: 'Rear camera' },
      ],
    });
    const { unmount } = renderCapture(scanner);

    fireEvent.click(screen.getByRole('button', { name: 'Start camera' }));
    await screen.findByRole('button', { name: 'Stop camera' });
    expect(screen.getByRole('combobox', { name: 'Camera' })).toBeTruthy();
    fireEvent.click(screen.getByRole('button', { name: 'Stop camera' }));
    expect(session.stop).toHaveBeenCalledOnce();

    fireEvent.click(screen.getByRole('button', { name: 'Start camera' }));
    await screen.findByRole('button', { name: 'Stop camera' });
    unmount();
    expect(session.stop).toHaveBeenCalledTimes(2);
  });

  it('ignores a camera result delivered after cancellation', async () => {
    const onScan = vi.fn();
    const { scanner, decodeCameraResult } = createScanner();
    renderCapture(scanner, onScan);

    fireEvent.click(screen.getByRole('button', { name: 'Start camera' }));
    await waitFor(() => expect(scanner.startCamera).toHaveBeenCalledOnce());
    await waitFor(() => expect(screen.getByRole('button', { name: 'Stop camera' })).toBeTruthy());
    fireEvent.click(screen.getByRole('button', { name: 'Stop camera' }));
    act(() => decodeCameraResult()?.('late-code'));

    expect(onScan).not.toHaveBeenCalled();
  });

  it('switches to the selected available camera', async () => {
    const { scanner } = createScanner({
      cameras: [
        { deviceId: 'front', label: 'Front camera' },
        { deviceId: 'rear', label: 'Rear camera' },
      ],
    });
    renderCapture(scanner);

    fireEvent.click(screen.getByRole('button', { name: 'Start camera' }));
    const cameraSelector = await screen.findByRole('combobox', { name: 'Camera' });
    fireEvent.change(cameraSelector, { target: { value: 'rear' } });

    await waitFor(() => expect(scanner.startCamera).toHaveBeenCalledTimes(2));
    expect(scanner.startCamera).toHaveBeenLastCalledWith(
      expect.any(HTMLVideoElement),
      'rear',
      expect.any(Function),
      expect.any(Function),
    );
  });

  it('automatically emits locally decoded images and reports unreadable images', async () => {
    const onScan = vi.fn();
    const { scanner } = createScanner();
    renderCapture(scanner, onScan);
    const image = new File(['image'], 'code.png', { type: 'image/png' });

    fireEvent.change(screen.getByLabelText('Decode a QR code from an image on this device'), {
      target: { files: [image] },
    });

    await waitFor(() => expect(onScan).toHaveBeenCalledWith('image-code'));
    expect(scanner.decodeImage).toHaveBeenCalledWith(image);

    const unreadable = createScanner({ imageError: new Error('No QR code') });
    const unreadableView = renderCapture(unreadable.scanner);
    fireEvent.change(
      within(unreadableView.container).getByLabelText(
        'Decode a QR code from an image on this device',
      ),
      {
        target: { files: [image] },
      },
    );
    expect(await screen.findByText(/No readable QR code was found/)).toBeTruthy();
  });

  it('does not submit an image decode that completes after the scanner unmounts', async () => {
    const onScan = vi.fn();
    const { scanner } = createScanner();
    let resolveDecode: ((value: string) => void) | undefined;
    scanner.decodeImage = vi.fn(
      () =>
        new Promise<string>((resolve) => {
          resolveDecode = resolve;
        }),
    );
    const { unmount } = renderCapture(scanner, onScan);
    const image = new File(['image'], 'code.png', { type: 'image/png' });

    fireEvent.change(screen.getByLabelText('Decode a QR code from an image on this device'), {
      target: { files: [image] },
    });
    await waitFor(() => expect(scanner.decodeImage).toHaveBeenCalledOnce());
    unmount();
    resolveDecode?.('late-image-code');

    await waitFor(() => expect(onScan).not.toHaveBeenCalled());
  });

  it('requires explicit use of manual or keyboard-wedge entry', () => {
    const onScan = vi.fn();
    const { scanner } = createScanner();
    renderCapture(scanner, onScan);
    const field = screen.getByLabelText('Enter a code or use a keyboard-wedge scanner');

    fireEvent.change(field, { target: { value: 'manual-code' } });
    fireEvent.keyDown(field, { key: 'Enter' });
    expect(onScan).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole('button', { name: 'Use code' }));
    expect(onScan).toHaveBeenCalledWith('manual-code');
  });

  it('rejects non-image files without passing them to the decoder', async () => {
    const { scanner } = createScanner();
    renderCapture(scanner);
    const file = new File(['text'], 'payload.txt', { type: 'text/plain' });

    fireEvent.change(screen.getByLabelText('Decode a QR code from an image on this device'), {
      target: { files: [file] },
    });

    expect(await screen.findByText('Choose an image file.')).toBeTruthy();
    expect(scanner.decodeImage).not.toHaveBeenCalled();
  });
});
