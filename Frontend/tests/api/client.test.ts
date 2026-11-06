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

  it.each([
    ['/local/api/v1', 'https://network.test/local/api/v1'],
    ['v1/news', 'https://network.test/local/api/v1/news'],
    ['https://attacker.test/local/api', null],
    ['/openapi/v1.json', null],
    ['//attacker.test/local/api', null],
    ['/local/api/v1#fragment', null],
  ])('resolves only same-origin targets under the configured API path', (href, expectedHref) => {
    const apiBase = createApiBaseUrl('/local/api', 'https://network.test');

    expect(resolveApiTarget(href, apiBase)?.href ?? null).toBe(expectedHref);
  });

  it.each(['https://attacker.test/api', '/api?query=1', '/'])(
    'rejects an API base that is not a same-origin HTTP path: %s',
    (apiBasePath) => {
      expect(() => createApiBaseUrl(apiBasePath, 'https://network.test')).toThrow();
    },
  );

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

    expect({
      resourceClass: result.class,
      requests: request.mock.calls.map(([url, init]) => ({
        url: String(url),
        method: init?.method,
        accept: new Headers(init?.headers).get('Accept'),
        acceptLanguage: new Headers(init?.headers).get('Accept-Language'),
      })),
    }).toEqual({
      resourceClass: ['api', 'version'],
      requests: [
        {
          url: 'https://network.test/api',
          method: 'GET',
          accept: sirenMediaType,
          acceptLanguage: 'it',
        },
        {
          url: 'https://network.test/api/v1',
          method: 'GET',
          accept: sirenMediaType,
          acceptLanguage: 'it',
        },
      ],
    });
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

    const call = request.mock.calls[0];
    const headers = new Headers(call?.[1]?.headers);
    expect({
      resultClass: result?.class,
      request: {
        url: String(call?.[0]),
        method: call?.[1]?.method,
        accept: headers.get('Accept'),
        acceptLanguage: headers.get('Accept-Language'),
        contentType: headers.get('Content-Type'),
        body: call?.[1]?.body,
      },
    }).toEqual({
      resultClass: ['message'],
      request: {
        url: 'https://network.test/api/v1/messages',
        method: 'POST',
        accept: sirenMediaType,
        acceptLanguage: 'en',
        contentType: 'application/json',
        body: JSON.stringify({ body: 'Hello', urgent: false }),
      },
    });
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

  it('does not execute a cross-origin action target', async () => {
    const request = vi.fn<typeof fetch>();
    const client = createApiClient(createApiBaseUrl('/api', 'https://network.test'), request);

    let status: number | undefined;
    try {
      await client.submitAction(
        {
          name: 'open-external',
          title: 'Open',
          href: 'https://attacker.test/api',
          method: 'POST',
          type: 'application/json',
        },
        {},
      );
    } catch (error) {
      status = error instanceof ApiError ? error.status : undefined;
    }

    expect({ status, requestCount: request.mock.calls.length }).toEqual({
      status: 400,
      requestCount: 0,
    });
  });

  it('does not execute an unsupported action format', async () => {
    const request = vi.fn<typeof fetch>();
    const client = createApiClient(createApiBaseUrl('/api', 'https://network.test'), request);
    let errorName: string | undefined;

    try {
      await client.submitAction(
        {
          name: 'unsupported',
          title: 'Unsupported',
          href: '/api/v1/action',
          method: 'GET',
          type: 'text/plain',
        },
        {},
      );
    } catch (error) {
      errorName = error instanceof Error ? error.name : undefined;
    }

    expect({ errorName, requestCount: request.mock.calls.length }).toEqual({
      errorName: 'ApiError',
      requestCount: 0,
    });
  });

  it('maps a Siren server error code into ApiError', async () => {
    const request = vi
      .fn<typeof fetch>()
      .mockResolvedValue(
        sirenResponse({ class: ['error'], properties: { code: 'errors.conflict' } }, 409),
      );
    const client = createApiClient(createApiBaseUrl('/api', 'https://network.test'), request);

    let error: ApiError | undefined;
    try {
      await client.getResource('/api/v1');
    } catch (caught) {
      if (caught instanceof ApiError) error = caught;
    }

    expect({ status: error?.status, code: error?.code }).toEqual({
      status: 409,
      code: 'errors.conflict',
    });
  });

  it('rejects a successful response with a non-Siren body', async () => {
    const request = vi
      .fn<typeof fetch>()
      .mockResolvedValue(new Response('{}', { headers: { 'Content-Type': 'application/json' } }));
    const client = createApiClient(createApiBaseUrl('/api', 'https://network.test'), request);

    await expect(client.getResource('/api/v1')).rejects.toMatchObject({
      status: 200,
      code: 'errors.requestFailed',
    });
  });

  it.each([
    [{ class: ['resource'], links: [{ rel: ['self'], href: '/api' }] }, true],
    [{ links: [{ rel: 'self', href: '/api' }] }, false],
    [null, false],
  ])('validates Siren envelope structure', (entity, isValid) => {
    expect(parseSirenEntity(entity) !== null).toBe(isValid);
  });
});
