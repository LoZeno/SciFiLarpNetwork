import english from '../../../Localization/en.json';
import italian from '../../../Localization/it.json';

export const messages = { en: english, it: italian } as const;
export type Locale = keyof typeof messages;
export type MessageKey = keyof typeof english;

export function isMessageKey(value: string): value is MessageKey {
  return Object.hasOwn(english, value);
}

export function isLocale(value: string | null | undefined): value is Locale {
  return value === 'en' || value === 'it';
}

export function translate(locale: Locale, key: string): string {
  const localized = messages[locale] as Readonly<Record<string, string>>;
  const fallback = messages.en as Readonly<Record<string, string>>;
  return localized[key] ?? fallback[key] ?? key;
}
