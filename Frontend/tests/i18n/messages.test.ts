import { describe, expect, it } from 'vitest';
import { formatDate, formatDateTime, formatNumber } from '../../src/i18n/LocaleProvider';
import { isLocale, isMessageKey, translate } from '../../src/i18n/messages';

describe('localization helpers', () => {
  it.each([
    ['locale', 'it', true],
    ['locale', 'fr', false],
    ['message key', 'common.retry', true],
    ['message key', 'missing.key', false],
  ])('recognizes supported %s values', (kind, value, expected) => {
    const result = kind === 'locale' ? isLocale(value) : isMessageKey(value);

    expect(result).toBe(expected);
  });

  it.each([
    ['common.retry', 'Riprova'],
    ['missing.key', 'missing.key'],
  ])('translates %s in Italian', (key, expected) => {
    expect(translate('it', key)).toBe(expected);
  });

  it('formats numbers using the selected locale', () => {
    expect(formatNumber(1234567.5, 'it')).toBe('1.234.567,5');
  });

  it('formats dates using the selected locale', () => {
    expect(formatDate(new Date('2026-03-04T12:00:00Z'), 'it')).not.toBe('');
  });

  it('formats date-times using the selected locale', () => {
    expect(formatDateTime(new Date('2026-03-04T12:00:00Z'), 'it')).not.toBe('');
  });
});
