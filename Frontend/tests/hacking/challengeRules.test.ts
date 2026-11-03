import { describe, expect, it } from 'vitest';
import { calculateRequiredHits } from '../../src/hacking/challengeRules';

describe('calculateRequiredHits', () => {
  it('adds difficulty and minimum hits, then subtracts the hacking level', () => {
    expect(calculateRequiredHits(3, 1, 0)).toBe(4);
    expect(calculateRequiredHits(7, 1, 3)).toBe(5);
    expect(calculateRequiredHits(3, 5, 2)).toBe(6);
  });

  it('does not let negative skill reduce the base and clamps at one hit', () => {
    expect(calculateRequiredHits(4, 1, -8)).toBe(5);
    expect(calculateRequiredHits(1, 1, 1000)).toBe(1);
    expect(calculateRequiredHits(10, 2, 1000)).toBe(1);
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
