export const sirenMediaType = 'application/vnd.siren+json';
export const navigationRelation = 'urn:scifilarnetwork:rels:navigation';
export const latestVersionRelation = 'urn:scifilarnetwork:rels:latest-version';

export interface SirenLink {
  readonly rel: readonly string[];
  readonly href: string;
  readonly title?: string;
  readonly type?: string;
}

export interface SirenField {
  readonly name: string;
  readonly type: string;
  readonly title?: string;
  readonly value?: unknown;
}

export interface SirenAction {
  readonly name: string;
  readonly title: string;
  readonly href: string;
  readonly method: string;
  readonly type: string;
  readonly fields?: readonly SirenField[];
}

export interface SirenEntity {
  readonly class?: readonly string[];
  readonly properties?: Readonly<Record<string, unknown>>;
  readonly entities?: readonly SirenEntity[];
  readonly links?: readonly SirenLink[];
  readonly actions?: readonly SirenAction[];
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isStringArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.every((item) => typeof item === 'string');
}

function parseLink(value: unknown): SirenLink | null {
  if (!isRecord(value) || !isStringArray(value.rel) || typeof value.href !== 'string') {
    return null;
  }

  return {
    rel: value.rel,
    href: value.href,
    ...(typeof value.title === 'string' ? { title: value.title } : {}),
    ...(typeof value.type === 'string' ? { type: value.type } : {}),
  };
}

function parseField(value: unknown): SirenField | null {
  if (
    !isRecord(value) ||
    typeof value.name !== 'string' ||
    typeof value.type !== 'string' ||
    (value.title !== undefined && typeof value.title !== 'string')
  ) {
    return null;
  }

  return {
    name: value.name,
    type: value.type,
    ...(typeof value.title === 'string' ? { title: value.title } : {}),
    ...(Object.hasOwn(value, 'value') ? { value: value.value } : {}),
  };
}

function parseAction(value: unknown): SirenAction | null {
  if (
    !isRecord(value) ||
    typeof value.name !== 'string' ||
    typeof value.title !== 'string' ||
    typeof value.href !== 'string' ||
    typeof value.method !== 'string' ||
    typeof value.type !== 'string'
  ) {
    return null;
  }

  const fields = value.fields;
  const parsedFields = Array.isArray(fields) ? fields.map(parseField) : undefined;
  if (
    fields !== undefined &&
    (parsedFields === undefined || parsedFields.some((field) => field === null))
  ) {
    return null;
  }

  return {
    name: value.name,
    title: value.title,
    href: value.href,
    method: value.method,
    type: value.type,
    ...(parsedFields === undefined
      ? {}
      : { fields: parsedFields.filter((field): field is SirenField => field !== null) }),
  };
}

export function parseSirenEntity(value: unknown): SirenEntity | null {
  if (!isRecord(value)) {
    return null;
  }

  if (value.class !== undefined && !isStringArray(value.class)) {
    return null;
  }

  if (value.properties !== undefined && !isRecord(value.properties)) {
    return null;
  }

  const links = value.links;
  const parsedLinks = Array.isArray(links) ? links.map(parseLink) : undefined;
  if (
    links !== undefined &&
    (parsedLinks === undefined || parsedLinks.some((link) => link === null))
  ) {
    return null;
  }

  const actions = value.actions;
  const parsedActions = Array.isArray(actions) ? actions.map(parseAction) : undefined;
  if (
    actions !== undefined &&
    (parsedActions === undefined || parsedActions.some((action) => action === null))
  ) {
    return null;
  }

  const entities = Array.isArray(value.entities)
    ? value.entities
        .map(parseSirenEntity)
        .filter((entity): entity is SirenEntity => entity !== null)
    : undefined;

  return {
    ...(isStringArray(value.class) ? { class: value.class } : {}),
    ...(isRecord(value.properties) ? { properties: value.properties } : {}),
    ...(entities === undefined ? {} : { entities }),
    ...(parsedLinks === undefined
      ? {}
      : { links: parsedLinks.filter((link): link is SirenLink => link !== null) }),
    ...(parsedActions === undefined
      ? {}
      : { actions: parsedActions.filter((action): action is SirenAction => action !== null) }),
  };
}
