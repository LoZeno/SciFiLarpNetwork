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

    expect(await screen.findByRole('heading', { name: 'Scan a QR code' })).toBeTruthy();
    expect(screen.getByRole('textbox', { name: 'Decoded QR code' })).toHaveProperty('value', '');
    expect(request).not.toHaveBeenCalled();
    expect(document.querySelector('.application-shell')).toBeNull();
  });

  it('shows decoded content below the scanner and clears it on request', () => {
    const scanner = createScanner();
    const { unmount } = render(
      <LocaleProvider>
        <QrCaptureTestPage scanner={scanner} />
      </LocaleProvider>,
    );
    const manualField = screen.getByLabelText('Enter a code or use a keyboard-wedge scanner');
    fireEvent.change(manualField, { target: { value: 'decoded-test-code' } });
    fireEvent.click(screen.getByRole('button', { name: 'Use code' }));

    const result = screen.getByRole('textbox', { name: 'Decoded QR code' });
    expect(result).toHaveProperty('value', 'decoded-test-code');
    expect(
      result.compareDocumentPosition(screen.getByRole('button', { name: 'Clear result' })),
    ).toBe(Node.DOCUMENT_POSITION_FOLLOWING);

    fireEvent.click(screen.getByRole('button', { name: 'Clear result' }));
    expect(result).toHaveProperty('value', '');
    unmount();
  });
});
