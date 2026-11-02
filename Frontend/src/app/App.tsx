import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { ApiError, createApiBaseUrl, createApiClient } from '../api/client';
import {
  navigationRelation,
  type SirenAction,
  type SirenEntity,
  type SirenField,
  type SirenLink,
} from '../api/siren';
import { formatDate, formatDateTime, formatNumber, useLocale } from '../i18n/LocaleProvider';
import { isLocale, isMessageKey, type Locale } from '../i18n/messages';

const apiBasePath = import.meta.env.VITE_API_BASE_PATH || '/api';

function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}

function resourcePath(href: string): string {
  return `/resource?href=${encodeURIComponent(href)}`;
}

function displayedValue(value: unknown, locale: Locale): string {
  if (typeof value === 'string') {
    if (/^\d{4}-\d{2}-\d{2}(?:T.*)?$/.test(value)) {
      const date = new Date(value);
      if (!Number.isNaN(date.valueOf())) {
        return value.includes('T') ? formatDateTime(date, locale) : formatDate(date, locale);
      }
    }

    return value;
  }

  if (typeof value === 'number') {
    return formatNumber(value, locale);
  }

  if (typeof value === 'boolean') {
    return String(value);
  }

  return JSON.stringify(value);
}

function getResourceTitle(entity: SirenEntity, applicationName: string): string {
  const properties = entity.properties;
  const title = properties?.title ?? properties?.name;
  if (typeof title === 'string') {
    return title;
  }

  return entity.class?.join(' · ') || applicationName;
}

const inputTypes: Readonly<Record<string, string>> = {
  text: 'text',
  search: 'search',
  email: 'email',
  password: 'password',
  number: 'number',
  date: 'date',
  'datetime-local': 'datetime-local',
  checkbox: 'checkbox',
  tel: 'tel',
  url: 'url',
};

function isSupportedField(field: SirenField): boolean {
  return Object.hasOwn(inputTypes, field.type);
}

function initialFieldValues(fields: readonly SirenField[]): Record<string, string | boolean> {
  return Object.fromEntries(
    fields.map((field) => [
      field.name,
      field.type === 'checkbox'
        ? field.value === true
        : typeof field.value === 'string' || typeof field.value === 'number'
          ? String(field.value)
          : '',
    ]),
  );
}

function actionValues(
  fields: readonly SirenField[],
  values: Readonly<Record<string, string | boolean>>,
): Record<string, string | number | boolean> {
  const entries: [string, string | number | boolean][] = [];
  for (const field of fields) {
    const value = values[field.name];
    if (value === undefined || (typeof value === 'string' && value === '')) {
      continue;
    }

    if (field.type === 'number' && typeof value === 'string') {
      const number = Number(value);
      if (Number.isFinite(number)) {
        entries.push([field.name, number]);
      }

      continue;
    }

    entries.push([field.name, value]);
  }

  return Object.fromEntries(entries);
}

function LanguageSelector() {
  const { locale, setLocale, t } = useLocale();

  return (
    <label className="language-picker">
      <span>{t('common.language')}</span>
      <select
        aria-label={t('common.language')}
        value={locale}
        onChange={(event) => {
          const nextLocale = event.currentTarget.value;
          if (isLocale(nextLocale)) {
            setLocale(nextLocale);
          }
        }}
      >
        <option value="en">{t('language.english')}</option>
        <option value="it">{t('language.italian')}</option>
      </select>
    </label>
  );
}

function NavigationLink({ link }: { readonly link: SirenLink }) {
  const { t } = useLocale();
  const title = link.title || t('app.untitledNavigation');

  return (
    <Link className="navigation-link" to={resourcePath(link.href)}>
      {title}
    </Link>
  );
}

function ActionForm({
  action,
  onComplete,
}: {
  readonly action: SirenAction;
  readonly onComplete: (entity: SirenEntity | null) => void;
}) {
  const { locale, t } = useLocale();
  const fields = action.fields ?? [];
  const [values, setValues] = useState(() => initialFieldValues(fields));
  const [busy, setBusy] = useState(false);
  const [errorCode, setErrorCode] = useState<string | null>(null);
  const supported =
    action.type === 'application/json' &&
    ['POST', 'PUT', 'PATCH', 'DELETE'].includes(action.method.toUpperCase()) &&
    fields.every(isSupportedField);
  const client = useMemo(
    () =>
      createApiClient(createApiBaseUrl(apiBasePath, window.location.origin), fetch, () => locale),
    [locale],
  );

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setErrorCode(null);

    try {
      const result = await client.submitAction(action, actionValues(fields, values));
      onComplete(result);
    } catch (error: unknown) {
      setErrorCode(isApiError(error) ? error.code : 'errors.requestFailed');
    } finally {
      setBusy(false);
    }
  }

  if (!supported) {
    return <p className="notice">{t('actions.unsupported')}</p>;
  }

  return (
    <form className="action-form" onSubmit={submit}>
      <h3>{action.title}</h3>
      {fields.map((field) => {
        const value = values[field.name] ?? '';
        const inputType = inputTypes[field.type];

        return (
          <label className="field" key={field.name}>
            <span>{field.title || field.name}</span>
            <input
              type={inputType}
              name={field.name}
              {...(field.type === 'checkbox'
                ? { checked: value === true }
                : { value: String(value) })}
              onChange={(event) => {
                const nextValue =
                  field.type === 'checkbox'
                    ? event.currentTarget.checked
                    : event.currentTarget.value;
                setValues((current) => ({ ...current, [field.name]: nextValue }));
              }}
            />
          </label>
        );
      })}
      <button className="button button-primary" type="submit" disabled={busy}>
        {busy ? t('common.loading') : action.title}
      </button>
      {errorCode !== null && (
        <p className="form-message" role="status">
          {t(isMessageKey(errorCode) ? errorCode : 'errors.requestFailed')}
        </p>
      )}
    </form>
  );
}

function ResourceContent({
  entity,
  onActionComplete,
}: {
  readonly entity: SirenEntity;
  readonly onActionComplete: (entity: SirenEntity | null) => void;
}) {
  const { locale, t } = useLocale();
  const { properties = {}, actions = [] } = entity;
  const entries = Object.entries(properties);
  const displayEntries = entries.filter(([key]) => key !== 'name' && key !== 'title');

  return (
    <div className="resource-content">
      {entity.class !== undefined && entity.class.length > 0 && (
        <p className="resource-class">{entity.class.join(' · ')}</p>
      )}
      {displayEntries.length > 0 && (
        <dl className="property-list">
          {displayEntries.map(([key, value]) => (
            <div className="property-row" key={key}>
              <dt>{key}</dt>
              <dd>{displayedValue(value, locale)}</dd>
            </div>
          ))}
        </dl>
      )}
      {actions.length > 0 && (
        <section className="actions-section" aria-labelledby="actions-heading">
          <h2 id="actions-heading">{t('actions.heading')}</h2>
          {actions.map((action) => (
            <ActionForm key={action.name} action={action} onComplete={onActionComplete} />
          ))}
        </section>
      )}
    </div>
  );
}

export function App() {
  const { locale, t } = useLocale();
  const localeRef = useRef(locale);
  localeRef.current = locale;
  const location = useLocation();
  const navigate = useNavigate();
  const resourceHref = new URLSearchParams(location.search).get('href');
  const [resource, setResource] = useState<SirenEntity | null>(null);
  const [loading, setLoading] = useState(true);
  const [errorCode, setErrorCode] = useState<string | null>(null);
  const [actionNotice, setActionNotice] = useState<string | null>(null);
  const [reloadCounter, setReloadCounter] = useState(0);
  const apiBaseUrl = useMemo(() => createApiBaseUrl(apiBasePath, window.location.origin), []);
  const client = useMemo(
    () => createApiClient(apiBaseUrl, fetch, () => localeRef.current),
    [apiBaseUrl],
  );

  useEffect(() => {
    setActionNotice(null);
  }, [resourceHref]);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setErrorCode(null);

    const request = resourceHref === null ? client.discover() : client.getResource(resourceHref);
    void request
      .then((result) => {
        if (active) {
          setResource(result);
          setLoading(false);
        }
      })
      .catch((error: unknown) => {
        if (active) {
          setErrorCode(isApiError(error) ? error.code : 'errors.requestFailed');
          setResource(null);
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [client, reloadCounter, resourceHref]);

  const navigationLinks =
    resource?.links?.filter((link) => link.rel.includes(navigationRelation)) ?? [];
  const title = resource === null ? t('app.name') : getResourceTitle(resource, t('app.name'));

  function completeAction(updatedResource: SirenEntity | null) {
    if (updatedResource !== null) {
      setResource(updatedResource);
      return;
    }

    setActionNotice('actions.completed');
    setReloadCounter((counter) => counter + 1);
  }

  return (
    <div className="application-shell">
      <header className="top-bar">
        <Link className="brand" to="/" aria-label={t('app.name')}>
          <span className="brand-mark" aria-hidden="true">
            S
          </span>
          <span>{t('app.name')}</span>
        </Link>
        <LanguageSelector />
      </header>
      <div className="workspace">
        <aside className="sidebar" aria-label={t('navigation.label')}>
          <p className="sidebar-heading">{t('navigation.heading')}</p>
          {navigationLinks.length > 0 ? (
            <nav className="navigation-list">
              {navigationLinks.map((link) => (
                <NavigationLink key={link.href} link={link} />
              ))}
            </nav>
          ) : (
            <p className="sidebar-empty">{t('navigation.empty')}</p>
          )}
        </aside>
        <main className="main-content">
          <div className="page-heading">
            <div>
              <p className="eyebrow">{t('app.workspace')}</p>
              <h1>{title}</h1>
            </div>
            <button className="button button-secondary" type="button" onClick={() => navigate('/')}>
              {t('navigation.home')}
            </button>
          </div>
          {loading && (
            <div className="loading-panel" role="status" aria-live="polite">
              <span className="loading-indicator" aria-hidden="true" />
              <span>{t('common.loading')}</span>
            </div>
          )}
          {!loading && errorCode !== null && (
            <section className="error-panel" role="alert">
              <h2>{t('app.connectionError')}</h2>
              <p>{t(isMessageKey(errorCode) ? errorCode : 'errors.requestFailed')}</p>
              <button
                className="button button-primary"
                type="button"
                onClick={() => setReloadCounter((counter) => counter + 1)}
              >
                {t('common.retry')}
              </button>
            </section>
          )}
          {actionNotice !== null && (
            <p className="notice" role="status">
              {t(actionNotice)}
            </p>
          )}
          {!loading && errorCode === null && resource !== null && (
            <>
              {resource.class?.includes('api') && (
                <section className="welcome-panel">
                  <span className="status-indicator" aria-hidden="true" />
                  <div>
                    <h2>{t('app.apiConnected')}</h2>
                    <p>{t('app.apiDescription')}</p>
                  </div>
                </section>
              )}
              {navigationLinks.length === 0 && (
                <p className="notice">{t('navigation.emptyPage')}</p>
              )}
              <ResourceContent entity={resource} onActionComplete={completeAction} />
            </>
          )}
        </main>
      </div>
    </div>
  );
}
