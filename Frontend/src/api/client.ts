import {
  latestVersionRelation,
  parseSirenEntity,
  sirenMediaType,
  type SirenAction,
  type SirenEntity,
} from './siren';

export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly code: string,
  ) {
    super(code);
    this.name = 'ApiError';
  }
}

export function createApiBaseUrl(apiBasePath: string, origin: string): URL {
  const base = new URL(apiBasePath, origin);
  if (
    !['http:', 'https:'].includes(base.protocol) ||
    base.origin !== new URL(origin).origin ||
    base.pathname === '/' ||
    base.search !== '' ||
    base.hash !== ''
  ) {
    throw new Error('The API base must be an HTTP(S) path on the current origin.');
  }

  base.pathname = base.pathname.replace(/\/+$/, '') || '/';
  return base;
}

export function resolveApiTarget(href: string, apiBaseUrl: URL): URL | null {
  try {
    const resolutionBase = apiBaseUrl.pathname.endsWith('/')
      ? apiBaseUrl
      : new URL(`${apiBaseUrl.pathname}/`, apiBaseUrl.origin);
    const target = new URL(href, resolutionBase);
    const basePath = apiBaseUrl.pathname.replace(/\/+$/, '');
    const isWithinApiPath =
      basePath === '' ||
      basePath === '/' ||
      target.pathname === basePath ||
      target.pathname.startsWith(`${basePath}/`);

    if (
      target.origin !== apiBaseUrl.origin ||
      target.protocol !== apiBaseUrl.protocol ||
      target.username !== '' ||
      target.password !== '' ||
      target.hash !== '' ||
      !isWithinApiPath
    ) {
      return null;
    }

    return target;
  } catch {
    return null;
  }
}

function errorCode(value: unknown): string {
  if (typeof value !== 'object' || value === null || !('properties' in value)) {
    return 'errors.requestFailed';
  }

  const properties = value.properties;
  if (
    typeof properties === 'object' &&
    properties !== null &&
    'code' in properties &&
    typeof properties.code === 'string'
  ) {
    return properties.code;
  }

  return 'errors.requestFailed';
}

export interface ApiClient {
  getResource(href: string): Promise<SirenEntity>;
  discover(): Promise<SirenEntity>;
  submitAction(
    action: SirenAction,
    values: Readonly<Record<string, string | number | boolean>>,
  ): Promise<SirenEntity | null>;
}

export function createApiClient(
  apiBaseUrl: URL,
  request: typeof fetch = fetch,
  getLanguage: () => string = () => 'en',
): ApiClient {
  async function send(
    href: string,
    init: RequestInit,
  ): Promise<{ entity: SirenEntity | null; status: number }> {
    const target = resolveApiTarget(href, apiBaseUrl);
    if (target === null) {
      throw new ApiError(400, 'errors.requestFailed');
    }

    const response = await request(target, {
      ...init,
      headers: {
        Accept: sirenMediaType,
        'Accept-Language': getLanguage(),
        ...init.headers,
      },
    });

    if (!response.ok) {
      let payload: unknown;
      try {
        payload = await response.json();
      } catch {
        payload = null;
      }

      throw new ApiError(response.status, errorCode(payload));
    }

    if (response.status === 204 || response.headers.get('content-length') === '0') {
      return { entity: null, status: response.status };
    }

    const contentType = response.headers.get('content-type')?.split(';', 1)[0]?.trim();
    if (contentType !== sirenMediaType) {
      throw new ApiError(response.status, 'errors.requestFailed');
    }

    const entity = parseSirenEntity(await response.json());
    if (entity === null) {
      throw new ApiError(response.status, 'errors.requestFailed');
    }

    return { entity, status: response.status };
  }

  async function getResource(href: string): Promise<SirenEntity> {
    const result = await send(href, { method: 'GET' });
    if (result.entity === null) {
      throw new ApiError(result.status, 'errors.requestFailed');
    }

    return result.entity;
  }

  return {
    getResource,
    async discover() {
      const root = await getResource(apiBaseUrl.href);
      const versionLink = root.links?.find((link) => link.rel.includes(latestVersionRelation));
      return versionLink === undefined ? root : getResource(versionLink.href);
    },
    async submitAction(action, values) {
      const method = action.method.toUpperCase();
      if (
        !['POST', 'PUT', 'PATCH', 'DELETE'].includes(method) ||
        action.type !== 'application/json'
      ) {
        throw new ApiError(400, 'errors.requestFailed');
      }

      const result = await send(action.href, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(values),
      });

      return result.entity;
    },
  };
}
