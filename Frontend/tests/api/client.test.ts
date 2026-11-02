import { describe, expect, it, vi } from 'vitest';
import {
  ApiError,
  createApiBaseUrl,
  createApiClient,
  resolveApiTarget,
} from '../../src/api/client';
import { parseSirenEntity, sirenMediaType } from '../../src/api/siren';
import apiLinkPolicyCases from '../../../Shared/SciFiLarpNetwork.ApiLinkPolicy/test-cases.json';

function sirenResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': sirenMediaType },
  });
}

describe('Siren API client', () => {
  it.each(apiLinkPolicyCases)('matches shared API link policy: $name', (testCase) => {
    const apiBase = new URL(testCase.apiBaseUrl);
    const basePath = apiBase.pathname;
    const clientBase = createApiBaseUrl(basePath, apiBase.origin);
    const resolved = resolveApiTarget(testCase.href, clientBase);

    expect(resolved?.href ?? null).toBe(testCase.expectedHref);
  });

  it('resolves only same-origin targets under the configured API path', () => {
    const apiBase = createApiBaseUrl('/local/api', 'https://network.test');

    expect(resolveApiTarget('/local/api/v1', apiBase)?.href).toBe(
      'https://network.test/local/api/v1',
    );
    expect(resolveApiTarget('v1/news', apiBase)?.href).toBe(
      'https://network.test/local/api/v1/news',
    );
    expect(resolveApiTarget('https://attacker.test/local/api', apiBase)).toBeNull();
    expect(resolveApiTarget('/openapi/v1.json', apiBase)).toBeNull();
    expect(resolveApiTarget('//attacker.test/local/api', apiBase)).toBeNull();
    expect(resolveApiTarget('/local/api/v1#fragment', apiBase)).toBeNull();
  });

  it('rejects an API base that is not a same-origin HTTP path', () => {
    expect(() => createApiBaseUrl('https://attacker.test/api', 'https://network.test')).toThrow();
    expect(() => createApiBaseUrl('/api?query=1', 'https://network.test')).toThrow();
    expect(() => createApiBaseUrl('/', 'https://network.test')).toThrow();
  });

  it('discovers and follows the advertised latest-version link', async () => {
    const request = vi
      .fn<typeof fetch>()
      .mockResolvedValueOnce(
        sirenResponse({
          class: ['api'],
          links: [
            { rel: ['self'], href: '/api' },
            {
              rel: ['urn:scifilarnetwork:rels:latest-version'],
              href: '/api/v1',
            },
          ],
        }),
      )
      .mockResolvedValueOnce(sirenResponse({ class: ['api', 'version'] }));
    const client = createApiClient(
      createApiBaseUrl('/api', 'https://network.test'),
      request,
      () => 'it',
    );

    const result = await client.discover();

    expect(result.class).toEqual(['api', 'version']);
    expect(request).toHaveBeenCalledTimes(2);
    expect(request).toHaveBeenNthCalledWith(
      1,
      new URL('https://network.test/api'),
      expect.objectContaining({
        method: 'GET',
        headers: { Accept: sirenMediaType, 'Accept-Language': 'it' },
      }),
    );
  });

  it('submits only a supported JSON action and returns its Siren result', async () => {
    const request = vi.fn<typeof fetch>().mockResolvedValue(sirenResponse({ class: ['message'] }));
    const client = createApiClient(createApiBaseUrl('/api', 'https://network.test'), request);

    const result = await client.submitAction(
      {
        name: 'send-message',
        title: 'Send',
        href: '/api/v1/messages',
        method: 'POST',
        type: 'application/json',
      },
      { body: 'Hello', urgent: false },
    );

    expect(result?.class).toEqual(['message']);
    expect(request).toHaveBeenCalledWith(
      new URL('https://network.test/api/v1/messages'),
      expect.objectContaining({
        method: 'POST',
        headers: {
          Accept: sirenMediaType,
          'Accept-Language': 'en',
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ body: 'Hello', urgent: false }),
      }),
    );
  });

  it('accepts a successful action with no representation', async () => {
    const request = vi.fn<typeof fetch>().mockResolvedValue(new Response(null, { status: 204 }));
    const client = createApiClient(createApiBaseUrl('/api', 'https://network.test'), request);

    await expect(
      client.submitAction(
        {
          name: 'delete-resource',
          title: 'Delete',
          href: '/api/v1/resource',
          method: 'DELETE',
          type: 'application/json',
        },
        {},
      ),
    ).resolves.toBeNull();
  });

  it('does not execute unsupported action formats or cross-boundary targets', async () => {
    const request = vi.fn<typeof fetch>();
    const client = createApiClient(createApiBaseUrl('/api', 'https://network.test'), request);

    await expect(
      client.submitAction(
        {
          name: 'open-external',
          title: 'Open',
          href: 'https://attacker.test/api',
          method: 'POST',
          type: 'application/json',
        },
        {},
      ),
    ).rejects.toMatchObject({ status: 400 });
    await expect(
      client.submitAction(
        {
          name: 'unsupported',
          title: 'Unsupported',
          href: '/api/v1/action',
          method: 'GET',
          type: 'text/plain',
        },
        {},
      ),
    ).rejects.toBeInstanceOf(ApiError);
    expect(request).not.toHaveBeenCalled();
  });

  it('maps server error codes and rejects non-Siren response bodies', async () => {
    const request = vi
      .fn<typeof fetch>()
      .mockResolvedValueOnce(
        sirenResponse({ class: ['error'], properties: { code: 'errors.conflict' } }, 409),
      )
      .mockResolvedValueOnce(
        new Response('{}', { headers: { 'Content-Type': 'application/json' } }),
      );
    const client = createApiClient(createApiBaseUrl('/api', 'https://network.test'), request);

    await expect(client.getResource('/api/v1')).rejects.toMatchObject({
      status: 409,
      code: 'errors.conflict',
    });
    await expect(client.getResource('/api/v1')).rejects.toMatchObject({
      status: 200,
      code: 'errors.requestFailed',
    });
  });

  it('validates incoming Siren envelopes without trusting malformed links', () => {
    expect(
      parseSirenEntity({ class: ['resource'], links: [{ rel: ['self'], href: '/api' }] }),
    ).not.toBeNull();
    expect(parseSirenEntity({ links: [{ rel: 'self', href: '/api' }] })).toBeNull();
    expect(parseSirenEntity(null)).toBeNull();
  });
});
