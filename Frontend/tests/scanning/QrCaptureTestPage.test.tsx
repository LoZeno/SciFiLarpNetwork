import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { AppRouter } from '../../src/app/AppRouter';
import { LocaleProvider } from '../../src/i18n/LocaleProvider';
import { QrCaptureTestPage } from '../../src/scanning/QrCaptureTestPage';
import type { QrScanner } from '../../src/scanning/qrScanner';

function createScanner(): QrScanner {
  return {
    getCameraAvailability: () => 'available',
    listCameras: vi.fn(async () => []),
    startCamera: vi.fn(async () => ({ stop: vi.fn() })),
    decodeImage: vi.fn(async () => 'decoded-image-code'),
  };
}

function renderTestPage() {
  return render(
    <LocaleProvider>
      <MemoryRouter initialEntries={['/qr-capture-test']}>
        <AppRouter />
      </MemoryRouter>
    </LocaleProvider>,
  );
}

describe('QR capture test page', () => {
  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it('is available at its direct URL without rendering the backend-connected app', async () => {
    const request = vi.fn<typeof fetch>();
    vi.stubGlobal('fetch', request);
    renderTestPage();

    const heading = await screen.findByRole('heading', { name: 'Scan a QR code' });
    expect({
      heading: heading.textContent,
      decodedValue: (screen.getByRole('textbox', { name: 'Decoded QR code' }) as HTMLInputElement)
        .value,
      requestCount: request.mock.calls.length,
      applicationShell: document.querySelector('.application-shell'),
    }).toEqual({
      heading: 'Scan a QR code',
      decodedValue: '',
      requestCount: 0,
      applicationShell: null,
    });
  });

  it('shows decoded content below the scanner after explicit submission', () => {
    const scanner = createScanner();
    render(
      <LocaleProvider>
        <QrCaptureTestPage scanner={scanner} />
      </LocaleProvider>,
    );
    const manualField = screen.getByLabelText('Enter a code or use a keyboard-wedge scanner');
    fireEvent.change(manualField, { target: { value: 'decoded-test-code' } });
    fireEvent.click(screen.getByRole('button', { name: 'Use code' }));

    const result = screen.getByRole('textbox', { name: 'Decoded QR code' });
    const clearButton = screen.getByRole('button', { name: 'Clear result' });
    const followsResult =
      result.compareDocumentPosition(clearButton) === Node.DOCUMENT_POSITION_FOLLOWING;

    expect({
      decodedValue: (result as HTMLInputElement).value,
      clearButtonFollowsResult: followsResult,
    }).toEqual({
      decodedValue: 'decoded-test-code',
      clearButtonFollowsResult: true,
    });
  });

  it('clears previously decoded content on request', () => {
    const scanner = createScanner();
    render(
      <LocaleProvider>
        <QrCaptureTestPage scanner={scanner} />
      </LocaleProvider>,
    );
    fireEvent.change(screen.getByLabelText('Enter a code or use a keyboard-wedge scanner'), {
      target: { value: 'decoded-test-code' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Use code' }));
    const result = screen.getByRole('textbox', { name: 'Decoded QR code' }) as HTMLInputElement;
    fireEvent.click(screen.getByRole('button', { name: 'Clear result' }));

    expect(result.value).toBe('');
  });
});
