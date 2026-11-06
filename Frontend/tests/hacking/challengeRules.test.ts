import { describe, expect, it } from 'vitest';
import { calculateRequiredHits } from '../../src/hacking/challengeRules';

describe('calculateRequiredHits', () => {
  it.each([
    [3, 1, 0, 4],
    [7, 1, 3, 5],
    [3, 5, 2, 6],
  ])(
    'adds difficulty and minimum hits, then subtracts the hacking level',
    (difficulty, minimumHits, attackerLevel, expected) => {
      expect(calculateRequiredHits(difficulty, minimumHits, attackerLevel)).toBe(expected);
    },
  );

  it.each([
    [4, 1, -8, 5],
    [1, 1, 1000, 1],
    [10, 2, 1000, 1],
  ])(
    'does not let negative skill reduce the base and clamps at one hit',
    (difficulty, minimumHits, attackerLevel, expected) => {
      expect(calculateRequiredHits(difficulty, minimumHits, attackerLevel)).toBe(expected);
    },
  );

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
