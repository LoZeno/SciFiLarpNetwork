import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { AppRouter } from '../../src/app/AppRouter';
import { LocaleProvider } from '../../src/i18n/LocaleProvider';

function renderTestPage() {
  return render(
    <LocaleProvider>
      <MemoryRouter initialEntries={['/theatrical-hacking-test']}>
        <AppRouter />
      </MemoryRouter>
    </LocaleProvider>,
  );
}

function startSimulation(difficulty?: string, attackerLevel?: string) {
  renderTestPage();
  if (difficulty !== undefined) {
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Challenge difficulty (1–10)' }), {
      target: { value: difficulty },
    });
  }
  if (attackerLevel !== undefined) {
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Attacker hacking level' }), {
      target: { value: attackerLevel },
    });
  }
  fireEvent.click(screen.getByRole('button', { name: 'Start hacking simulation' }));
  return screen.getByRole('group', { name: 'Fictional terminal display' });
}

async function requestHint(previousCharacter?: string) {
  await waitFor(() => {
    if ((screen.getByRole('button', { name: 'Request a hint' }) as HTMLButtonElement).disabled) {
      throw new Error('The hint button is still disabled.');
    }
  });
  fireEvent.click(screen.getByRole('button', { name: 'Request a hint' }));
  return waitFor(() => {
    const character =
      screen
        .getByText(/The challenge character is/)
        .textContent?.trim()
        .at(-1) ?? '';
    if (!/^[A-Z0-9]$/.test(character) || character === previousCharacter) {
      throw new Error('The next valid hint is not available yet.');
    }
    return character;
  });
}

function progressValue() {
  return (screen.getByRole('progressbar', { name: 'Access progress' }) as HTMLProgressElement)
    .value;
}

async function waitForProgress(value: number) {
  await waitFor(() => {
    if (progressValue() !== value) {
      throw new Error('The access progress has not reached the expected value yet.');
    }
  });
}

function penaltyKeys() {
  return within(screen.getByRole('group', { name: 'Penalty characters' }))
    .getAllByText(/^[A-Z0-9]$/)
    .map((element) => element.textContent ?? '');
}

describe('theatrical hacking test page', () => {
  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it('is directly addressable, local-only, and starts with a one-hit minimum', async () => {
    const request = vi.fn<typeof fetch>();
    vi.stubGlobal('fetch', request);
    renderTestPage();

    const notice = await screen.findByText(/Local test page only/);
    expect({
      notice: notice.textContent,
      minimumHits: (
        screen.getByRole('spinbutton', { name: 'Minimum required hits' }) as HTMLInputElement
      ).value,
      requestCount: request.mock.calls.length,
      applicationShell: document.querySelector('.application-shell'),
    }).toEqual({
      notice: expect.stringMatching(/Local test page only/),
      minimumHits: '1',
      requestCount: 0,
      applicationShell: null,
    });
  });

  it('focuses an empty terminal when a simulation starts', () => {
    const terminal = startSimulation('5', '1');

    expect({
      focused: document.activeElement === terminal,
      codeLength: document.querySelector('.hacking-code')?.textContent?.length,
    }).toEqual({ focused: true, codeLength: 0 });
  });

  it('reveals code without advancing for an incorrect character', async () => {
    const terminal = startSimulation('5', '1');
    const secretCharacter = await requestHint();
    terminal.focus();

    fireEvent.keyDown(terminal, { key: secretCharacter === 'A' ? 'B' : 'A' });

    expect({
      codeLength: document.querySelector('.hacking-code')?.textContent?.length,
      progress: progressValue(),
    }).toEqual({ codeLength: 1, progress: 0 });
  });

  it.each(['repeated keydown', 'keyup', 'outside terminal'] as const)(
    'ignores %s input',
    async (input) => {
      const terminal = startSimulation('5', '1');
      const secretCharacter = await requestHint();
      terminal.focus();
      fireEvent.keyDown(terminal, { key: secretCharacter === 'A' ? 'B' : 'A' });

      if (input === 'repeated keydown') {
        fireEvent.keyDown(terminal, { key: secretCharacter, repeat: true });
      } else if (input === 'keyup') {
        fireEvent.keyUp(terminal, { key: secretCharacter });
      } else {
        fireEvent.keyDown(screen.getByRole('button', { name: 'Request a hint' }), {
          key: secretCharacter,
        });
      }

      expect({
        codeLength: document.querySelector('.hacking-code')?.textContent?.length,
        progress: progressValue(),
      }).toEqual({ codeLength: 1, progress: 0 });
    },
  );

  it('advances for the disclosed challenge character', async () => {
    const terminal = startSimulation('5', '1');
    const secretCharacter = await requestHint();
    terminal.focus();

    fireEvent.keyDown(terminal, { key: secretCharacter });
    await waitForProgress(1);

    expect(progressValue()).toBe(1);
  });

  it('completes after the required number of correct characters', async () => {
    const terminal = startSimulation('5', '1');
    let character = await requestHint();
    terminal.focus();

    for (let hit = 0; hit < 5; hit += 1) {
      if (hit > 0) character = await requestHint(character);
      fireEvent.keyDown(terminal, { key: character });
      await waitForProgress(hit + 1);
    }

    expect({
      progress: progressValue(),
      codeLength: document.querySelector('.hacking-code')?.textContent?.length,
      outcome: screen.queryByText('ACCESS GRANTED')?.textContent,
    }).toEqual({ progress: 5, codeLength: 5, outcome: 'ACCESS GRANTED' });
  });

  it('provides a non-keyboard keypad for the disclosed challenge character', async () => {
    renderTestPage();
    fireEvent.click(screen.getByRole('button', { name: 'Start hacking simulation' }));
    fireEvent.click(screen.getByRole('button', { name: 'Request a hint' }));
    const hint = await screen.findByText(/The challenge character is/);
    const secretCharacter = hint.textContent?.at(-1);
    fireEvent.click(screen.getByRole('button', { name: 'Show accessible keypad' }));
    fireEvent.click(screen.getByRole('button', { name: `Enter character ${secretCharacter}` }));

    await waitFor(() => {
      if (
        (screen.getByRole('progressbar', { name: 'Access progress' }) as HTMLProgressElement)
          .value !== 1
      ) {
        throw new Error('The keypad input has not advanced access progress yet.');
      }
    });

    expect(
      (screen.getByRole('progressbar', { name: 'Access progress' }) as HTMLProgressElement).value,
    ).toBe(1);
  });

  it('rotates four penalty characters when progress advances', async () => {
    const terminal = startSimulation();
    const initialPenaltyKeys = penaltyKeys();
    const initialTarget = await requestHint();
    terminal.focus();

    fireEvent.keyDown(terminal, { key: initialTarget });
    await waitForProgress(1);

    const forwardPenaltyKeys = penaltyKeys();
    expect({
      initialCount: initialPenaltyKeys.length,
      initialUniqueCount: new Set(initialPenaltyKeys).size,
      forwardCount: forwardPenaltyKeys.length,
      forwardUniqueCount: new Set(forwardPenaltyKeys).size,
      forwardHasNoPreviousKey: forwardPenaltyKeys.every((key) => !initialPenaltyKeys.includes(key)),
      progress: progressValue(),
    }).toEqual({
      initialCount: 4,
      initialUniqueCount: 4,
      forwardCount: 4,
      forwardUniqueCount: 4,
      forwardHasNoPreviousKey: true,
      progress: 1,
    });
  });

  it('rotates four penalty characters when progress reverses', async () => {
    const terminal = startSimulation();
    const initialTarget = await requestHint();
    terminal.focus();
    fireEvent.keyDown(terminal, { key: initialTarget });
    await waitForProgress(1);
    const forwardPenaltyKeys = penaltyKeys();

    fireEvent.keyDown(terminal, { key: forwardPenaltyKeys[0] });
    await waitForProgress(0);

    const reversePenaltyKeys = penaltyKeys();
    expect({
      forwardCount: forwardPenaltyKeys.length,
      forwardUniqueCount: new Set(forwardPenaltyKeys).size,
      reverseCount: reversePenaltyKeys.length,
      reverseUniqueCount: new Set(reversePenaltyKeys).size,
      reverseHasNoPreviousKey: reversePenaltyKeys.every((key) => !forwardPenaltyKeys.includes(key)),
      progress: progressValue(),
    }).toEqual({
      forwardCount: 4,
      forwardUniqueCount: 4,
      reverseCount: 4,
      reverseUniqueCount: 4,
      reverseHasNoPreviousKey: true,
      progress: 0,
    });
  });
});
