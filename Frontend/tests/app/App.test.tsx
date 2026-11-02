import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
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

    expect(await screen.findByText('API connection established')).toBeTruthy();
    fireEvent.click(screen.getByRole('link', { name: 'News' }));

    expect(await screen.findByRole('heading', { name: 'Network news' })).toBeTruthy();
    expect(screen.getByText('A new transmission')).toBeTruthy();
    expect(request).toHaveBeenCalledTimes(3);
    expect(request.mock.calls[2]?.[0]).toEqual(new URL('http://localhost:3000/api/v1/news'));
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
    fireEvent.change(screen.getByRole('combobox', { name: 'Language' }), {
      target: { value: 'it' },
    });
    expect(screen.getByRole('textbox', { name: 'Message' })).toHaveProperty(
      'value',
      'Hello network',
    );
    fireEvent.click(screen.getByRole('button', { name: 'Send message' }));

    expect(await screen.findByRole('heading', { name: 'Sent' })).toBeTruthy();
    await waitFor(() => expect(request).toHaveBeenCalledTimes(4));
    expect(request.mock.calls[3]?.[1]).toMatchObject({
      method: 'POST',
      body: JSON.stringify({ body: 'Hello network' }),
      headers: { 'Accept-Language': 'it' },
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

    expect(await screen.findByText('Action completed.')).toBeTruthy();
    await waitFor(() => expect(request).toHaveBeenCalledTimes(5));
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
    expect(await screen.findByText('API connection established')).toBeTruthy();
    fireEvent.change(screen.getByRole('combobox', { name: 'Language' }), {
      target: { value: 'it' },
    });
    fireEvent.click(await screen.findByRole('link', { name: 'News' }));

    expect(
      await screen.findByText('Non è stato possibile trovare le informazioni richieste.'),
    ).toBeTruthy();
    expect(document.documentElement.lang).toBe('it');
    expect(request.mock.calls[2]?.[1]).toMatchObject({
      headers: { Accept: sirenMediaType, 'Accept-Language': 'it' },
    });
  });

  it('shows a translated connection error and retries the API root', async () => {
    const request = vi.mocked(fetch);
    request
      .mockRejectedValueOnce(new TypeError('offline'))
      .mockResolvedValueOnce(sirenResponse(apiRoot()))
      .mockResolvedValueOnce(sirenResponse(versionRoot()));

    renderApp();
    expect(await screen.findByRole('alert')).toBeTruthy();
    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));

    expect(await screen.findByText('API connection established')).toBeTruthy();
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

    expect(await screen.findByText(/uses fields or a request format/i)).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Unsupported action' })).toBeNull();
  });
});
