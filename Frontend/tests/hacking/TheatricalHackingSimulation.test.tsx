import { act, cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { LocaleProvider } from '../../src/i18n/LocaleProvider';
import {
  TheatricalHackingSimulation,
  type HackingChallengeView,
} from '../../src/hacking/TheatricalHackingSimulation';

function createChallenge(overrides: Partial<HackingChallengeView> = {}): HackingChallengeView {
  return {
    id: 'demo-challenge',
    deadline: Date.now() + 60_000,
    requiredHits: 2,
    hits: 0,
    penaltyCharacters: ['A', 'B', 'C', 'D'],
    outcome: 'active',
    ...overrides,
  };
}

function renderSimulation({
  challenge = createChallenge(),
  onAttempt = vi.fn(async () => ({
    hits: 0,
    outcome: 'active' as const,
    penaltyCharacters: ['A', 'B', 'C', 'D'],
  })),
  onRequestHint = vi.fn(async () => 'Q'),
  onExpired = vi.fn(),
  showTestOnlyFeatures = false,
}: {
  readonly challenge?: HackingChallengeView;
  readonly onAttempt?: (
    id: string,
    key: string,
  ) => Promise<{
    readonly hits: number;
    readonly outcome: 'active' | 'succeeded' | 'expired';
    readonly penaltyCharacters: readonly string[];
  }>;
  readonly onRequestHint?: (id: string) => Promise<string>;
  readonly onExpired?: (id: string) => void;
  readonly showTestOnlyFeatures?: boolean;
} = {}) {
  return render(
    <LocaleProvider>
      <TheatricalHackingSimulation
        challenge={challenge}
        onAttempt={onAttempt}
        onRequestHint={onRequestHint}
        onExpired={onExpired}
        showTestOnlyFeatures={showTestOnlyFeatures}
      />
    </LocaleProvider>,
  );
}

describe('theatrical hacking simulation control', () => {
  afterEach(() => {
    cleanup();
    vi.useRealTimers();
  });

  it('surfaces an attempt failure without reporting success', async () => {
    renderSimulation({
      onAttempt: vi.fn(async () => {
        throw new Error('Local simulation failure.');
      }),
    });

    fireEvent.keyDown(screen.getByRole('group', { name: 'Fictional terminal display' }), {
      key: 'Q',
    });

    expect(
      await screen.findByText('The challenge could not process that input. Try again.'),
    ).toBeTruthy();
  });

  it('surfaces a hint failure without reporting success', async () => {
    renderSimulation({
      onRequestHint: vi.fn(async () => {
        throw new Error('Local hint failure.');
      }),
      showTestOnlyFeatures: true,
    });

    fireEvent.click(screen.getByRole('button', { name: 'Request a hint' }));

    expect(await screen.findByText('A hint is not available for this simulation.')).toBeTruthy();
  });

  it('notifies the challenge owner once when its deadline expires', async () => {
    vi.useFakeTimers();
    const onExpired = vi.fn();
    renderSimulation({
      challenge: createChallenge({ deadline: Date.now() + 500 }),
      onExpired,
    });

    await act(async () => {
      await vi.advanceTimersByTimeAsync(1_000);
    });

    expect(onExpired.mock.calls).toEqual([['demo-challenge']]);
  });

  it('does not accept input after a challenge has completed', () => {
    const onAttempt = vi.fn(async () => ({
      hits: 2,
      outcome: 'succeeded' as const,
      penaltyCharacters: ['A', 'B', 'C', 'D'],
    }));
    renderSimulation({
      challenge: createChallenge({ hits: 2, outcome: 'succeeded' }),
      onAttempt,
    });

    expect({
      accessGranted: screen.getByText('ACCESS GRANTED').textContent,
      terminalTabIndex: screen
        .getByRole('group', { name: 'Fictional terminal display' })
        .getAttribute('tabindex'),
      hintButton: screen.queryByRole('button', { name: 'Request a hint' }),
      penaltyGroup: screen.queryByRole('group', { name: 'Penalty characters' }),
      attempts: onAttempt.mock.calls,
    }).toEqual({
      accessGranted: 'ACCESS GRANTED',
      terminalTabIndex: '-1',
      hintButton: null,
      penaltyGroup: null,
      attempts: [],
    });
  });

  it('hides test-only penalties and hints in the reusable control by default', () => {
    renderSimulation();

    expect({
      penalties: screen.queryByRole('group', { name: 'Penalty characters' }),
      hintButton: screen.queryByRole('button', { name: 'Request a hint' }),
      progressBar: screen.getByRole('progressbar', { name: 'Access progress' }) !== null,
      testCounter: screen.queryByText(/0\s*\/\s*2/),
      title: screen.getByRole('heading', { name: 'Restricted Network Access' }).textContent,
    }).toEqual({
      penalties: null,
      hintButton: null,
      progressBar: true,
      testCounter: null,
      title: 'Restricted Network Access',
    });
  });

  it('prevents Firefox quick-find from opening for apostrophe input', async () => {
    const onAttempt = vi.fn(async () => ({
      hits: 0,
      outcome: 'active' as const,
      penaltyCharacters: ['A', 'B', 'C', 'D'],
    }));
    renderSimulation({ onAttempt });
    const terminal = screen.getByRole('group', { name: 'Fictional terminal display' });

    const defaultPrevented = fireEvent.keyDown(terminal, { key: "'" });

    expect({
      defaultPrevented,
      terminalRetainsFocus: document.activeElement === terminal,
      attemptCalls: onAttempt.mock.calls,
    }).toEqual({
      defaultPrevented: false,
      terminalRetainsFocus: true,
      attemptCalls: [['demo-challenge', "'"]],
    });
  });

  it('scrolls the code viewport as characters are revealed', () => {
    renderSimulation();
    const viewport = document.querySelector('.hacking-code-viewport');
    if (!(viewport instanceof HTMLElement)) {
      throw new Error('The hacking code viewport is missing.');
    }

    Object.defineProperty(viewport, 'scrollHeight', { configurable: true, value: 640 });
    fireEvent.keyDown(screen.getByRole('group', { name: 'Fictional terminal display' }), {
      key: 'Q',
    });

    expect(viewport.scrollTop).toBe(640);
  });
});
