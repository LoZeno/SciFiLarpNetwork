import { useEffect, useLayoutEffect, useRef, useState, type KeyboardEvent } from 'react';
import { useLocale } from '../i18n/LocaleProvider';

export type HackingOutcome = 'active' | 'succeeded' | 'expired';

export interface HackingChallengeView {
  readonly id: string;
  readonly deadline: number;
  readonly requiredHits: number;
  readonly hits: number;
  readonly penaltyCharacters: readonly string[];
  readonly outcome: HackingOutcome;
}

export interface HackingAttemptResult {
  readonly hits: number;
  readonly penaltyCharacters: readonly string[];
  readonly outcome: HackingOutcome;
}

const keypadCharacters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789'.split('');
const fictionalCode = `use std::collections::{HashMap, VecDeque};
use std::time::{Duration, Instant};

#[derive(Clone, Debug)]
struct RelaySession {
    station: String,
    nonce: [u8; 32],
    issued_at: Instant,
    expires_after: Duration,
    attempts: u16,
}

impl RelaySession {
    fn is_current(&self, now: Instant) -> bool {
        now.saturating_duration_since(self.issued_at) < self.expires_after
    }

    fn verify_route(&self, route: &RouteFrame, registry: &Registry) -> bool {
        let Some(node) = registry.nodes.get(&route.origin) else {
            return false;
        };
        node.enabled
            && node.channel == route.channel
            && route.sequence == self.attempts as u64
            && route.payload.len() <= registry.max_frame_size
    }
}

#[derive(Default)]
struct Registry {
    nodes: HashMap<String, RelayNode>,
    max_frame_size: usize,
}

struct RelayNode {
    enabled: bool,
    channel: u16,
}

struct RouteFrame {
    origin: String,
    channel: u16,
    sequence: u64,
    payload: Vec<u8>,
}

enum SignalState {
    Standby,
    Calibrating { progress: u8 },
    Connected(RelaySession),
    Rejected { reason: &'static str },
}

fn evaluate_signal(
    session: &RelaySession,
    frame: &RouteFrame,
    registry: &Registry,
    now: Instant,
) -> SignalState {
    if !session.is_current(now) {
        return SignalState::Rejected { reason: "expired" };
    }

    if !session.verify_route(frame, registry) {
        return SignalState::Rejected { reason: "route mismatch" };
    }

    let remaining = session.expires_after
        .saturating_sub(now.saturating_duration_since(session.issued_at));
    if remaining < Duration::from_secs(2) {
        return SignalState::Calibrating { progress: 96 };
    }

    SignalState::Connected(session.clone())
}

fn consume_queue(
    queue: &mut VecDeque<RouteFrame>,
    registry: &Registry,
    session: &RelaySession,
    now: Instant,
) -> Vec<SignalState> {
    let mut states = Vec::with_capacity(queue.len());
    while let Some(frame) = queue.pop_front() {
        states.push(evaluate_signal(session, &frame, registry, now));
    }
    states
}`;

function isPrintableCodeKey(key: string): boolean {
  return /^[a-z0-9]$/i.test(key);
}

function isTerminalInputKey(key: string): boolean {
  return isPrintableCodeKey(key) || key === "'";
}

export function TheatricalHackingSimulation({
  challenge,
  onAttempt,
  onRequestHint,
  onExpired,
  showTestOnlyFeatures = false,
}: {
  readonly challenge: HackingChallengeView;
  readonly onAttempt: (challengeId: string, character: string) => Promise<HackingAttemptResult>;
  readonly onRequestHint: (challengeId: string) => Promise<string>;
  readonly onExpired: (challengeId: string) => void;
  readonly showTestOnlyFeatures?: boolean;
}) {
  const { t } = useLocale();
  const terminalRef = useRef<HTMLDivElement>(null);
  const codeViewportRef = useRef<HTMLDivElement>(null);
  const [remainingSeconds, setRemainingSeconds] = useState(() =>
    Math.max(0, Math.ceil((challenge.deadline - Date.now()) / 1000)),
  );
  const [displayedCode, setDisplayedCode] = useState('');
  const [penaltyCharacters, setPenaltyCharacters] = useState(challenge.penaltyCharacters);
  const [showKeypad, setShowKeypad] = useState(false);
  const [hint, setHint] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [feedback, setFeedback] = useState<string | null>(null);
  const queuedCharactersRef = useRef<string[]>([]);
  const drainingQueueRef = useRef(false);
  const expiredRef = useRef<string | null>(null);

  useEffect(() => {
    if (challenge.outcome === 'active') {
      terminalRef.current?.focus();
    }
  }, [challenge.id, challenge.outcome]);

  useEffect(() => {
    setPenaltyCharacters(challenge.penaltyCharacters);
  }, [challenge.id, challenge.penaltyCharacters]);

  useLayoutEffect(() => {
    const viewport = codeViewportRef.current;
    if (viewport !== null) {
      viewport.scrollTop = viewport.scrollHeight;
    }
  }, [displayedCode]);

  useEffect(() => {
    if (challenge.outcome !== 'active') {
      return;
    }

    const timer = window.setInterval(() => {
      const remaining = Math.max(0, Math.ceil((challenge.deadline - Date.now()) / 1000));
      setRemainingSeconds(remaining);
      if (remaining === 0 && expiredRef.current !== challenge.id) {
        expiredRef.current = challenge.id;
        onExpired(challenge.id);
      }
    }, 500);

    return () => window.clearInterval(timer);
  }, [challenge.deadline, challenge.id, challenge.outcome, onExpired]);

  async function submitCharacter(character: string) {
    if (
      challenge.outcome !== 'active' ||
      remainingSeconds === 0 ||
      !isTerminalInputKey(character)
    ) {
      return;
    }

    queuedCharactersRef.current.push(character.toUpperCase());
    setDisplayedCode((current) => {
      const nextCharacter = fictionalCode[current.length % fictionalCode.length] ?? '';
      return current + nextCharacter;
    });
    if (drainingQueueRef.current) {
      return;
    }

    drainingQueueRef.current = true;
    setBusy(true);
    setFeedback(null);
    try {
      let previousHits = challenge.hits;
      while (queuedCharactersRef.current.length > 0) {
        const nextCharacter = queuedCharactersRef.current.shift();
        if (nextCharacter === undefined) {
          continue;
        }

        const result = await onAttempt(challenge.id, nextCharacter);
        if (
          !Number.isInteger(result.hits) ||
          result.hits < 0 ||
          result.hits > challenge.requiredHits ||
          !Array.isArray(result.penaltyCharacters) ||
          result.penaltyCharacters.length !== 4 ||
          new Set(result.penaltyCharacters).size !== 4 ||
          result.penaltyCharacters.some((key) => !isPrintableCodeKey(key)) ||
          !['active', 'succeeded', 'expired'].includes(result.outcome)
        ) {
          throw new Error('The challenge returned an invalid progress update.');
        }

        setPenaltyCharacters(result.penaltyCharacters);
        if (result.hits !== previousHits) {
          setHint(null);
        }
        previousHits = result.hits;
        setFeedback(result.outcome === 'succeeded' ? t('hacking.success') : t('hacking.progress'));
        if (result.outcome !== 'active') {
          queuedCharactersRef.current = [];
        }
      }
    } catch {
      queuedCharactersRef.current = [];
      setFeedback(t('hacking.attemptError'));
    } finally {
      drainingQueueRef.current = false;
      setBusy(false);
    }
  }

  async function requestHint() {
    if (challenge.outcome !== 'active' || remainingSeconds === 0 || busy) {
      return;
    }

    setBusy(true);
    setFeedback(null);
    try {
      const requestedHint = await onRequestHint(challenge.id);
      if (!isPrintableCodeKey(requestedHint)) {
        throw new Error('The challenge returned an invalid hint.');
      }

      setHint(requestedHint.toUpperCase());
    } catch {
      setFeedback(t('hacking.hintError'));
    } finally {
      setBusy(false);
    }
  }

  function handleStageKeyDown(event: KeyboardEvent<HTMLElement>) {
    if (
      event.target !== event.currentTarget ||
      event.repeat ||
      event.altKey ||
      event.ctrlKey ||
      event.metaKey ||
      !isTerminalInputKey(event.key)
    ) {
      return;
    }

    event.preventDefault();
    void submitCharacter(event.key);
  }

  return (
    <section className="hacking-simulation" aria-labelledby="hacking-heading">
      <div className="hacking-heading-row">
        <h2 id="hacking-heading">{t('hacking.heading')}</h2>
        <p className="hacking-timer" aria-label={t('hacking.timeRemaining')}>
          {remainingSeconds}s
        </p>
      </div>

      <div
        ref={terminalRef}
        className="hacking-terminal"
        role="group"
        aria-label={t('hacking.terminal')}
        tabIndex={challenge.outcome === 'active' ? 0 : -1}
        onKeyDown={handleStageKeyDown}
      >
        <div ref={codeViewportRef} className="hacking-code-viewport" aria-hidden="true">
          <pre className="hacking-code">{displayedCode}</pre>
        </div>
        <p className="hacking-terminal-prompt">{t('hacking.focusPrompt')}</p>
      </div>

      <div className="hacking-progress">
        <progress
          aria-label={t('hacking.progressLabel')}
          max={challenge.requiredHits}
          value={challenge.hits}
        />
      </div>

      {showTestOnlyFeatures && (
        <div className="hacking-penalties" role="group" aria-label={t('hacking.penaltyCharacters')}>
          <p>{t('hacking.penaltyDescription')}</p>
          <div className="hacking-penalty-keys">
            {penaltyCharacters.map((character) => (
              <span
                className="hacking-penalty-key"
                key={character}
                aria-label={t('hacking.penaltyKey').replace('{0}', character)}
              >
                {character}
              </span>
            ))}
          </div>
        </div>
      )}

      {feedback !== null && (
        <p className="hacking-feedback" role="status" aria-live="polite">
          {feedback}
        </p>
      )}
      {challenge.outcome === 'succeeded' && (
        <p className="hacking-outcome" role="status">
          {t('hacking.complete')}
        </p>
      )}
      {challenge.outcome === 'expired' && (
        <p className="hacking-outcome" role="status">
          {t('hacking.expired')}
        </p>
      )}
      {showTestOnlyFeatures && hint !== null && (
        <p className="hacking-hint" role="status">
          {t('hacking.hintIs')} <strong>{hint}</strong>
        </p>
      )}

      <div className="hacking-actions">
        {showTestOnlyFeatures && (
          <button
            className="button button-secondary"
            type="button"
            disabled={challenge.outcome !== 'active' || remainingSeconds === 0 || busy}
            onClick={() => void requestHint()}
          >
            {t('hacking.requestHint')}
          </button>
        )}
        <button
          className="button button-secondary"
          type="button"
          aria-expanded={showKeypad}
          disabled={challenge.outcome !== 'active' || remainingSeconds === 0 || busy}
          onClick={() => setShowKeypad((current) => !current)}
        >
          {showKeypad ? t('hacking.hideKeypad') : t('hacking.showKeypad')}
        </button>
      </div>

      {showKeypad && challenge.outcome === 'active' && (
        <div className="hacking-keypad" role="group" aria-label={t('hacking.keypad')}>
          {keypadCharacters.map((character) => (
            <button
              className="hacking-key"
              type="button"
              key={character}
              disabled={busy}
              aria-label={t('hacking.pressKey').replace('{0}', character)}
              onClick={() => void submitCharacter(character)}
            >
              {character}
            </button>
          ))}
        </div>
      )}
    </section>
  );
}
