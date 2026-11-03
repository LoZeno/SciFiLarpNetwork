import { describe, expect, it } from 'vitest';
import { calculateRequiredHits } from '../../src/hacking/challengeRules';

describe('calculateRequiredHits', () => {
  it('uses the difficulty base, positive hacking level, and configured minimum', () => {
    expect(calculateRequiredHits(7, 1, 3)).toBe(4);
    expect(calculateRequiredHits(3, 5, 2)).toBe(5);
  });

  it('does not let negative skill reduce the base and clamps at the minimum', () => {
    expect(calculateRequiredHits(4, 1, -8)).toBe(4);
    expect(calculateRequiredHits(1, 1, 1000)).toBe(1);
    expect(calculateRequiredHits(10, 2, 1000)).toBe(2);
  });

  it.each([
    [0, 1, 0],
    [1, 1, -1.5],
    [11, 1, 0],
    [1, 0, 0],
    [1, 1, 1.5],
  ])(
    'rejects invalid challenge settings: difficulty=%s, minimum=%s, level=%s',
    (difficulty, minimumHits, attackerLevel) => {
      expect(() => calculateRequiredHits(difficulty, minimumHits, attackerLevel)).toThrow(
        RangeError,
      );
    },
  );
});
