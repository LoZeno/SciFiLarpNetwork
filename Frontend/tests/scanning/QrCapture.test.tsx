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

  it('does not start the camera before an explicit request', () => {
    const { scanner } = createScanner();

    renderCapture(scanner);

    expect(vi.mocked(scanner.startCamera).mock.calls).toEqual([]);
  });

  it('starts on explicit request and immediately emits only the first camera decode', async () => {
    const onScan = vi.fn();
    const { scanner, session, decodeCameraResult } = createScanner();
    renderCapture(scanner, onScan);

    fireEvent.click(screen.getByRole('button', { name: 'Start camera' }));
    await screen.findByRole('button', { name: 'Stop camera' });

    act(() => {
      decodeCameraResult()?.('first-code');
      decodeCameraResult()?.('duplicate-code');
    });

    expect({
      scans: onScan.mock.calls,
      startCalls: vi.mocked(scanner.startCamera).mock.calls.length,
      stopCalls: vi.mocked(session.stop).mock.calls.length,
      status: screen.getByRole('status').textContent,
      scanAnotherButton: screen.getByRole('button', { name: 'Scan another code' }).textContent,
    }).toEqual({
      scans: [['first-code']],
      startCalls: 1,
      stopCalls: 1,
      status: expect.stringContaining('Code captured'),
      scanAnotherButton: expect.any(String),
    });
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

  it('explains the insecure-context camera limitation', () => {
    const insecure = createScanner({ availability: 'insecure-context' });
    renderCapture(insecure.scanner);

    expect({
      message: screen.getByText(/Camera access requires HTTPS or localhost/).textContent,
      startDisabled: (screen.getByRole('button', { name: 'Start camera' }) as HTMLButtonElement)
        .disabled,
    }).toEqual({
      message: expect.stringMatching(/Camera access requires HTTPS or localhost/),
      startDisabled: true,
    });
  });

  it('explains unsupported browser camera access', () => {
    const unsupported = createScanner({ availability: 'unsupported' });
    renderCapture(unsupported.scanner);

    expect(screen.getByText(/Camera access is not supported/).textContent).toMatch(
      /Camera access is not supported/,
    );
  });

  it('stops the camera on cancellation', async () => {
    const { scanner, session } = createScanner({
      cameras: [
        { deviceId: 'front', label: 'Front camera' },
        { deviceId: 'rear', label: 'Rear camera' },
      ],
    });
    renderCapture(scanner);

    fireEvent.click(screen.getByRole('button', { name: 'Start camera' }));
    await screen.findByRole('button', { name: 'Stop camera' });
    const cameraSelectorVisible = screen.queryByRole('combobox', { name: 'Camera' }) !== null;
    fireEvent.click(screen.getByRole('button', { name: 'Stop camera' }));

    expect({
      cameraSelectorVisible,
      stopCalls: vi.mocked(session.stop).mock.calls.length,
    }).toEqual({ cameraSelectorVisible: true, stopCalls: 1 });
  });

  it('stops the camera when the component is disposed', async () => {
    const { scanner, session } = createScanner({
      cameras: [{ deviceId: 'front', label: 'Front camera' }],
    });
    const { unmount } = renderCapture(scanner);

    fireEvent.click(screen.getByRole('button', { name: 'Start camera' }));
    await screen.findByRole('button', { name: 'Stop camera' });
    unmount();

    expect(session.stop).toHaveBeenCalledOnce();
  });

  it('ignores a camera result delivered after cancellation', async () => {
    const onScan = vi.fn();
    const { scanner, decodeCameraResult } = createScanner();
    renderCapture(scanner, onScan);

    fireEvent.click(screen.getByRole('button', { name: 'Start camera' }));
    await screen.findByRole('button', { name: 'Stop camera' });
    fireEvent.click(screen.getByRole('button', { name: 'Stop camera' }));
    act(() => decodeCameraResult()?.('late-code'));

    expect(onScan.mock.calls).toEqual([]);
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

    await waitFor(() => {
      if (vi.mocked(scanner.startCamera).mock.calls.length !== 2) {
        throw new Error('The selected camera has not started yet.');
      }
    });
    const lastCall = vi.mocked(scanner.startCamera).mock.calls.at(-1);

    expect({
      callCount: vi.mocked(scanner.startCamera).mock.calls.length,
      videoElement: lastCall?.[0] instanceof HTMLVideoElement,
      deviceId: lastCall?.[1],
      hasDecodeCallback: typeof lastCall?.[2] === 'function',
      hasErrorCallback: typeof lastCall?.[3] === 'function',
    }).toEqual({
      callCount: 2,
      videoElement: true,
      deviceId: 'rear',
      hasDecodeCallback: true,
      hasErrorCallback: true,
    });
  });

  it('automatically emits a locally decoded image', async () => {
    const onScan = vi.fn();
    const { scanner } = createScanner();
    renderCapture(scanner, onScan);
    const image = new File(['image'], 'code.png', { type: 'image/png' });

    fireEvent.change(screen.getByLabelText('Decode a QR code from an image on this device'), {
      target: { files: [image] },
    });

    await waitFor(() => {
      if (onScan.mock.calls.length !== 1) {
        throw new Error('The image scan has not completed yet.');
      }
    });

    expect({
      scans: onScan.mock.calls,
      decodedImages: vi.mocked(scanner.decodeImage).mock.calls,
    }).toEqual({ scans: [['image-code']], decodedImages: [[image]] });
  });

  it('reports an unreadable image', async () => {
    const unreadable = createScanner({ imageError: new Error('No QR code') });
    const view = renderCapture(unreadable.scanner);
    const image = new File(['image'], 'code.png', { type: 'image/png' });
    fireEvent.change(
      within(view.container).getByLabelText('Decode a QR code from an image on this device'),
      { target: { files: [image] } },
    );

    const error = await screen.findByText(/No readable QR code was found/);

    expect({
      message: error.textContent,
      decoderCalls: vi.mocked(unreadable.scanner.decodeImage).mock.calls.length,
    }).toEqual({
      message: expect.stringMatching(/No readable QR code was found/),
      decoderCalls: 1,
    });
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
    await waitFor(() => {
      if (vi.mocked(scanner.decodeImage).mock.calls.length !== 1) {
        throw new Error('The image decoder has not started yet.');
      }
    });
    unmount();
    resolveDecode?.('late-image-code');

    expect(onScan.mock.calls).toEqual([]);
  });

  it('does not submit a manual code on Enter alone', () => {
    const onScan = vi.fn();
    const { scanner } = createScanner();
    renderCapture(scanner, onScan);
    const field = screen.getByLabelText('Enter a code or use a keyboard-wedge scanner');

    fireEvent.change(field, { target: { value: 'manual-code' } });
    fireEvent.keyDown(field, { key: 'Enter' });

    expect(onScan.mock.calls).toEqual([]);
  });

  it('submits a manual code after explicit confirmation', () => {
    const onScan = vi.fn();
    const { scanner } = createScanner();
    renderCapture(scanner, onScan);
    const field = screen.getByLabelText('Enter a code or use a keyboard-wedge scanner');
    fireEvent.change(field, { target: { value: 'manual-code' } });

    fireEvent.click(screen.getByRole('button', { name: 'Use code' }));

    expect(onScan.mock.calls).toEqual([['manual-code']]);
  });

  it('rejects non-image files without passing them to the decoder', async () => {
    const { scanner } = createScanner();
    renderCapture(scanner);
    const file = new File(['text'], 'payload.txt', { type: 'text/plain' });

    fireEvent.change(screen.getByLabelText('Decode a QR code from an image on this device'), {
      target: { files: [file] },
    });

    const message = await screen.findByText('Choose an image file.');

    expect({
      message: message.textContent,
      decodeCalls: vi.mocked(scanner.decodeImage).mock.calls,
    }).toEqual({
      message: 'Choose an image file.',
      decodeCalls: [],
    });
  });
});
