import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { isLocale, translate, type Locale } from './messages';

const preferenceKey = 'scifi-larp-network.locale';

interface LocaleContextValue {
  readonly locale: Locale;
  readonly setLocale: (locale: Locale) => void;
  readonly t: (key: string) => string;
}

const LocaleContext = createContext<LocaleContextValue | null>(null);

function initialLocale(): Locale {
  try {
    const stored = window.localStorage.getItem(preferenceKey);
    if (isLocale(stored)) {
      return stored;
    }
  } catch {
    // Browser storage is optional; the application falls back to English.
  }

  return 'en';
}

export function LocaleProvider({ children }: { readonly children: ReactNode }) {
  const [locale, setLocale] = useState<Locale>(initialLocale);
  const value = useMemo<LocaleContextValue>(
    () => ({ locale, setLocale, t: (key) => translate(locale, key) }),
    [locale],
  );

  useEffect(() => {
    document.documentElement.lang = locale;
    try {
      window.localStorage.setItem(preferenceKey, locale);
    } catch {
      // The selected locale is still active for the current page.
    }
  }, [locale]);

  return <LocaleContext.Provider value={value}>{children}</LocaleContext.Provider>;
}

export function useLocale(): LocaleContextValue {
  const context = useContext(LocaleContext);
  if (context === null) {
    throw new Error('useLocale must be used inside LocaleProvider.');
  }

  return context;
}

export function formatDate(value: Date, locale: Locale): string {
  return new Intl.DateTimeFormat(locale).format(value);
}

export function formatDateTime(value: Date, locale: Locale): string {
  return new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(value);
}

export function formatNumber(value: number, locale: Locale): string {
  return new Intl.NumberFormat(locale).format(value);
}
