export function calculateRequiredHits(
  difficulty: number,
  minimumHits: number,
  attackerHackingLevel: number,
): number {
  if (
    !Number.isInteger(difficulty) ||
    difficulty < 1 ||
    difficulty > 10 ||
    !Number.isInteger(minimumHits) ||
    !Number.isInteger(attackerHackingLevel) ||
    minimumHits < 1
  ) {
    throw new RangeError('Challenge difficulty, minimum hits, or hacking level is invalid.');
  }

  return Math.max(1, minimumHits + difficulty - Math.max(0, attackerHackingLevel));
}
