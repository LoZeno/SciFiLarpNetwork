import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { App } from '../../src/app/App';
import { sirenMediaType } from '../../src/api/siren';
import { LocaleProvider } from '../../src/i18n/LocaleProvider';

function sirenResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': sirenMediaType },
  });
}

function apiRoot() {
  return {
    class: ['api'],
    properties: { name: 'Sci-Fi LARP Network' },
    links: [
      { rel: ['self'], href: '/api' },
      { rel: ['urn:scifilarnetwork:rels:latest-version'], href: '/api/v1' },
    ],
  };
}

function versionRoot() {
  return {
    class: ['api', 'version'],
    properties: { version: 'v1', name: 'Sci-Fi LARP Network' },
    links: [
      { rel: ['self'], href: '/api/v1' },
      { rel: ['urn:scifilarnetwork:rels:navigation'], href: '/api/v1/news', title: 'News' },
    ],
  };
}

function renderApp(initialEntry = '/') {
  return render(
    <LocaleProvider>
      <MemoryRouter initialEntries={[initialEntry]}>
        <App />
      </MemoryRouter>
    </LocaleProvider>,
  );
}

describe('React application shell', () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.stubGlobal('fetch', vi.fn<typeof fetch>());
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it('discovers the API version and renders Siren navigation and resource data', async () => {
    const request = vi.mocked(fetch);
    request
      .mockResolvedValueOnce(sirenResponse(apiRoot()))
      .mockResolvedValueOnce(sirenResponse(versionRoot()))
      .mockResolvedValueOnce(
        sirenResponse({
          class: ['news-feed'],
          properties: { title: 'Network news', headline: 'A new transmission' },
        }),
      );

    renderApp();

    const connectionStatus = await screen.findByText('API connection established');
    const connectionStatusText = connectionStatus.textContent;
    fireEvent.click(screen.getByRole('link', { name: 'News' }));

    const heading = await screen.findByRole('heading', { name: 'Network news' });
    expect({
      connectionStatus: connectionStatusText,
      heading: heading.textContent,
      headline: screen.getByText('A new transmission').textContent,
      requestCount: request.mock.calls.length,
      resourceUrl: String(request.mock.calls[2]?.[0]),
    }).toEqual({
      connectionStatus: 'API connection established',
      heading: 'Network news',
      headline: 'A new transmission',
      requestCount: 3,
      resourceUrl: 'http://localhost:3000/api/v1/news',
    });
  });

  it('submits an advertised action using only its typed fields', async () => {
    const request = vi.mocked(fetch);
    request
      .mockResolvedValueOnce(sirenResponse(apiRoot()))
      .mockResolvedValueOnce(sirenResponse(versionRoot()))
      .mockResolvedValueOnce(
        sirenResponse({
          class: ['message-form'],
          properties: { title: 'Compose message' },
          actions: [
            {
              name: 'send-message',
              title: 'Send message',
              href: '/api/v1/messages',
              method: 'POST',
              type: 'application/json',
              fields: [{ name: 'body', title: 'Message', type: 'text' }],
            },
          ],
        }),
      )
      .mockResolvedValueOnce(sirenResponse({ class: ['message'], properties: { title: 'Sent' } }));

    renderApp();
    fireEvent.click(await screen.findByRole('link', { name: 'News' }));
    const message = await screen.findByRole('textbox', { name: 'Message' });
    fireEvent.change(message, { target: { value: 'Hello network' } });
    const submittedMessage = (message as HTMLInputElement).value;
    fireEvent.change(screen.getByRole('combobox', { name: 'Language' }), {
      target: { value: 'it' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Send message' }));

    const heading = await screen.findByRole('heading', { name: 'Sent' });
    expect({
      message: submittedMessage,
      sentHeading: heading.textContent,
      requestCount: request.mock.calls.length,
      request: request.mock.calls[3]?.[1],
    }).toEqual({
      message: 'Hello network',
      sentHeading: 'Sent',
      requestCount: 4,
      request: expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ body: 'Hello network' }),
        headers: {
          Accept: sirenMediaType,
          'Accept-Language': 'it',
          'Content-Type': 'application/json',
        },
      }),
    });
  });

  it('shows feedback when an action succeeds without a response representation', async () => {
    const request = vi.mocked(fetch);
    const resource = {
      class: ['message-form'],
      properties: { title: 'Compose message' },
      actions: [
        {
          name: 'send-message',
          title: 'Send message',
          href: '/api/v1/messages',
          method: 'POST',
          type: 'application/json',
          fields: [{ name: 'body', title: 'Message', type: 'text' }],
        },
      ],
    };
    request
      .mockResolvedValueOnce(sirenResponse(apiRoot()))
      .mockResolvedValueOnce(sirenResponse(versionRoot()))
      .mockResolvedValueOnce(sirenResponse(resource))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(sirenResponse(resource));

    renderApp();
    fireEvent.click(await screen.findByRole('link', { name: 'News' }));
    fireEvent.change(await screen.findByRole('textbox', { name: 'Message' }), {
      target: { value: 'Hello network' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Send message' }));

    const feedback = await screen.findByText('Action completed.');
    expect({ feedback: feedback.textContent, requestCount: request.mock.calls.length }).toEqual({
      feedback: 'Action completed.',
      requestCount: 5,
    });
  });

  it('localizes a stable API error code after language selection', async () => {
    const request = vi.mocked(fetch);
    request
      .mockResolvedValueOnce(sirenResponse(apiRoot()))
      .mockResolvedValueOnce(sirenResponse(versionRoot()))
      .mockResolvedValueOnce(
        sirenResponse({ class: ['error'], properties: { code: 'errors.notFound' } }, 404),
      );

    renderApp();
    await screen.findByText('API connection established');
    fireEvent.change(screen.getByRole('combobox', { name: 'Language' }), {
      target: { value: 'it' },
    });
    fireEvent.click(await screen.findByRole('link', { name: 'News' }));

    const message = await screen.findByText(
      'Non è stato possibile trovare le informazioni richieste.',
    );
    expect({
      message: message.textContent,
      documentLanguage: document.documentElement.lang,
      requestHeaders: request.mock.calls[2]?.[1]?.headers,
    }).toEqual({
      message: 'Non è stato possibile trovare le informazioni richieste.',
      documentLanguage: 'it',
      requestHeaders: { Accept: sirenMediaType, 'Accept-Language': 'it' },
    });
  });

  it('shows a translated connection error when the API is offline', async () => {
    const request = vi.mocked(fetch);
    request.mockRejectedValueOnce(new TypeError('offline'));

    renderApp();
    const alert = await screen.findByRole('alert');

    expect(alert.textContent).toContain('The network service is unavailable');
  });

  it('retries API discovery after the retry action', async () => {
    const request = vi.mocked(fetch);
    request
      .mockRejectedValueOnce(new TypeError('offline'))
      .mockResolvedValueOnce(sirenResponse(apiRoot()))
      .mockResolvedValueOnce(sirenResponse(versionRoot()));

    renderApp();
    await screen.findByRole('alert');
    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));

    const status = await screen.findByText('API connection established');
    expect({ status: status.textContent, requestCount: request.mock.calls.length }).toEqual({
      status: 'API connection established',
      requestCount: 3,
    });
  });

  it('does not render an unsupported action as an executable form', async () => {
    const request = vi.mocked(fetch);
    request
      .mockResolvedValueOnce(sirenResponse(apiRoot()))
      .mockResolvedValueOnce(sirenResponse(versionRoot()))
      .mockResolvedValueOnce(
        sirenResponse({
          class: ['resource'],
          actions: [
            {
              name: 'unsupported',
              title: 'Unsupported action',
              href: '/api/v1/action',
              method: 'POST',
              type: 'application/json',
              fields: [{ name: 'payload', title: 'Payload', type: 'custom-editor' }],
            },
          ],
        }),
      );

    renderApp();
    fireEvent.click(await screen.findByRole('link', { name: 'News' }));

    const warning = await screen.findByText(/uses fields or a request format/i);
    expect({
      warning: warning.textContent,
      executableButton: screen.queryByRole('button', { name: 'Unsupported action' }),
    }).toEqual({
      warning: expect.stringMatching(/uses fields or a request format/i),
      executableButton: null,
    });
  });
});
