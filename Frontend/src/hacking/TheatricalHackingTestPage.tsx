import { useCallback, useRef, useState, type FormEvent } from 'react';
import { useLocale } from '../i18n/LocaleProvider';
import { calculateRequiredHits } from './challengeRules';
import {
  TheatricalHackingSimulation,
  type HackingAttemptResult,
  type HackingChallengeView,
  type HackingOutcome,
} from './TheatricalHackingSimulation';

const demoDurationMs = 60_000;
const targetCharacters = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
const penaltyCharacterCount = 4;

interface DemoChallenge extends HackingChallengeView {
  readonly targetCharacter: string;
}

function randomIndex(maximum: number): number {
  const range = 0x1_0000_0000;
  const limit = Math.floor(range / maximum) * maximum;
  let value: number;
  do {
    const randomValue = crypto.getRandomValues(new Uint32Array(1))[0];
    if (randomValue === undefined) {
      throw new Error('Unable to generate a local demonstration challenge.');
    }

    value = randomValue;
  } while (value >= limit);

  return value % maximum;
}

function chooseRandomCharacter(excluded: readonly string[] = []): string {
  const candidates = [...targetCharacters].filter((character) => !excluded.includes(character));
  const chosen = candidates[randomIndex(candidates.length)];
  if (chosen === undefined) {
    throw new Error('Unable to generate a local demonstration challenge.');
  }

  return chosen;
}

function choosePenaltyCharacters(
  targetCharacter: string,
  previousCharacters: readonly string[] = [],
): readonly string[] {
  const characters: string[] = [];
  while (characters.length < penaltyCharacterCount) {
    characters.push(chooseRandomCharacter([targetCharacter, ...previousCharacters, ...characters]));
  }

  return characters;
}

function randomDemoChallengeId(): string {
  return Array.from(crypto.getRandomValues(new Uint32Array(4)), (value) => value.toString(16)).join(
    '-',
  );
}

export function TheatricalHackingTestPage() {
  const { t } = useLocale();
  const challengeRef = useRef<DemoChallenge | null>(null);
  const [challenge, setChallenge] = useState<DemoChallenge | null>(null);
  const [difficulty, setDifficulty] = useState(5);
  const [minimumHits, setMinimumHits] = useState(1);
  const [attackerLevel, setAttackerLevel] = useState(0);
  const [configurationError, setConfigurationError] = useState<string | null>(null);

  function startSimulation(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    try {
      const targetCharacter = chooseRandomCharacter();
      const nextChallenge: DemoChallenge = {
        id: randomDemoChallengeId(),
        deadline: Date.now() + demoDurationMs,
        requiredHits: calculateRequiredHits(difficulty, minimumHits, attackerLevel),
        hits: 0,
        outcome: 'active',
        targetCharacter,
        penaltyCharacters: choosePenaltyCharacters(targetCharacter),
      };
      challengeRef.current = nextChallenge;
      setChallenge(nextChallenge);
      setConfigurationError(null);
    } catch (error: unknown) {
      setConfigurationError(
        error instanceof RangeError ? t('hacking.configurationError') : t('hacking.startError'),
      );
    }
  }

  const onAttempt = useCallback(
    async (challengeId: string, character: string): Promise<HackingAttemptResult> => {
      const current = challengeRef.current;
      if (current === null || current.id !== challengeId || current.outcome !== 'active') {
        return {
          hits: current?.hits ?? 0,
          outcome: current?.outcome ?? 'expired',
          penaltyCharacters: current?.penaltyCharacters ?? [],
        };
      }

      if (Date.now() >= current.deadline) {
        const expired = { ...current, outcome: 'expired' as const };
        challengeRef.current = expired;
        setChallenge(expired);
        return {
          hits: expired.hits,
          outcome: expired.outcome,
          penaltyCharacters: expired.penaltyCharacters,
        };
      }

      const isCorrect = character.toUpperCase() === current.targetCharacter;
      const isPenalty = current.penaltyCharacters.includes(character.toUpperCase());
      const nextHits = isCorrect
        ? current.hits + 1
        : isPenalty
          ? Math.max(0, current.hits - 1)
          : current.hits;
      const nextOutcome: HackingOutcome = nextHits >= current.requiredHits ? 'succeeded' : 'active';
      const progressChanged = nextHits !== current.hits;
      const targetCharacter =
        isCorrect && nextOutcome === 'active'
          ? chooseRandomCharacter([current.targetCharacter, ...current.penaltyCharacters])
          : current.targetCharacter;
      const updated = {
        ...current,
        hits: nextHits,
        outcome: nextOutcome,
        targetCharacter,
        penaltyCharacters:
          progressChanged && nextOutcome === 'active'
            ? choosePenaltyCharacters(targetCharacter, current.penaltyCharacters)
            : current.penaltyCharacters,
      };
      challengeRef.current = updated;
      setChallenge(updated);
      return {
        hits: updated.hits,
        outcome: updated.outcome,
        penaltyCharacters: updated.penaltyCharacters,
      };
    },
    [],
  );

  const onRequestHint = useCallback(async (challengeId: string): Promise<string> => {
    const current = challengeRef.current;
    if (current === null || current.id !== challengeId || current.outcome !== 'active') {
      throw new Error('There is no active demonstration challenge.');
    }

    if (Date.now() >= current.deadline) {
      const expired = { ...current, outcome: 'expired' as const };
      challengeRef.current = expired;
      setChallenge(expired);
      throw new Error('The demonstration challenge has expired.');
    }

    return current.targetCharacter;
  }, []);

  const onExpired = useCallback((challengeId: string) => {
    const current = challengeRef.current;
    if (current === null || current.id !== challengeId || current.outcome !== 'active') {
      return;
    }

    const expired = { ...current, outcome: 'expired' as const };
    challengeRef.current = expired;
    setChallenge(expired);
  }, []);

  return (
    <main className="hacking-test-page">
      <p className="hacking-demo-notice">{t('hacking.demoOnly')}</p>
      <form className="hacking-config" onSubmit={startSimulation}>
        <label className="field">
          <span>{t('hacking.difficulty')}</span>
          <input
            type="number"
            min={1}
            max={10}
            step={1}
            required
            value={difficulty}
            onChange={(event) => setDifficulty(Number(event.currentTarget.value))}
          />
        </label>
        <label className="field">
          <span>{t('hacking.minimumHits')}</span>
          <input
            type="number"
            min={1}
            max={10}
            step={1}
            required
            value={minimumHits}
            onChange={(event) => setMinimumHits(Number(event.currentTarget.value))}
          />
        </label>
        <label className="field">
          <span>{t('hacking.attackerLevel')}</span>
          <input
            type="number"
            min={-100}
            max={100}
            step={1}
            required
            value={attackerLevel}
            onChange={(event) => setAttackerLevel(Number(event.currentTarget.value))}
          />
        </label>
        <button className="button button-primary" type="submit">
          {t('hacking.start')}
        </button>
      </form>

      {configurationError !== null && (
        <p className="hacking-feedback" role="alert">
          {configurationError}
        </p>
      )}
      {challenge !== null && (
        <TheatricalHackingSimulation
          key={challenge.id}
          challenge={{
            id: challenge.id,
            deadline: challenge.deadline,
            requiredHits: challenge.requiredHits,
            hits: challenge.hits,
            penaltyCharacters: challenge.penaltyCharacters,
            outcome: challenge.outcome,
          }}
          onAttempt={onAttempt}
          onRequestHint={onRequestHint}
          onExpired={onExpired}
          showTestOnlyFeatures
        />
      )}
    </main>
  );
}
