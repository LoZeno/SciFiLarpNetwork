import { describe, expect, it } from 'vitest';
import { formatDate, formatDateTime, formatNumber } from '../../src/i18n/LocaleProvider';
import { isLocale, isMessageKey, translate } from '../../src/i18n/messages';

describe('localization helpers', () => {
  it('recognizes supported locales and falls back to English messages', () => {
    expect(isLocale('it')).toBe(true);
    expect(isLocale('fr')).toBe(false);
    expect(isMessageKey('common.retry')).toBe(true);
    expect(isMessageKey('missing.key')).toBe(false);
    expect(translate('it', 'common.retry')).toBe('Riprova');
    expect(translate('it', 'missing.key')).toBe('missing.key');
  });

  it('formats numbers, dates, and date-times using the selected locale', () => {
    expect(formatNumber(1234567.5, 'it')).toBe('1.234.567,5');
    expect(formatDate(new Date('2026-03-04T12:00:00Z'), 'it')).not.toBe('');
    expect(formatDateTime(new Date('2026-03-04T12:00:00Z'), 'it')).not.toBe('');
  });
});
