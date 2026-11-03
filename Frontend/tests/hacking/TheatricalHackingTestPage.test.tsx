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

describe('theatrical hacking test page', () => {
  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
  });

  it('is directly addressable, local-only, and starts with a one-hit minimum', async () => {
    const request = vi.fn<typeof fetch>();
    vi.stubGlobal('fetch', request);
    renderTestPage();

    expect(await screen.findByText(/Local test page only/)).toBeTruthy();
    expect(screen.getByRole('spinbutton', { name: 'Minimum required hits' })).toHaveProperty(
      'value',
      '1',
    );
    expect(request).not.toHaveBeenCalled();
    expect(document.querySelector('.application-shell')).toBeNull();
  });

  it('only advances for the secret character and completes after the formula count', async () => {
    renderTestPage();
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Challenge difficulty (1–10)' }), {
      target: { value: '5' },
    });
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Attacker hacking level' }), {
      target: { value: '1' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Start hacking simulation' }));

    const terminal = screen.getByRole('group', { name: 'Fictional terminal display' });
    expect(document.activeElement).toBe(terminal);
    expect(document.querySelector('.hacking-code')?.textContent).toBe('');

    fireEvent.click(screen.getByRole('button', { name: 'Request a hint' }));
    const hint = await screen.findByText(/The challenge character is/);
    const secretCharacter = hint.textContent?.trim().at(-1);
    expect(secretCharacter).toMatch(/^[A-Z0-9]$/);

    const wrongCharacter = secretCharacter === 'A' ? 'B' : 'A';
    terminal.focus();
    fireEvent.keyDown(terminal, { key: wrongCharacter });
    expect(document.querySelector('.hacking-code')?.textContent).toHaveLength(1);
    fireEvent.keyDown(terminal, { key: secretCharacter, repeat: true });
    fireEvent.keyUp(terminal, { key: secretCharacter });
    fireEvent.keyDown(screen.getByRole('button', { name: 'Request a hint' }), {
      key: secretCharacter,
    });
    expect(document.querySelector('.hacking-code')?.textContent).toHaveLength(1);
    expect(screen.getByRole('progressbar', { name: 'Access progress' })).toHaveProperty('value', 0);

    let previousCharacter = secretCharacter;
    for (let hit = 0; hit < 4; hit += 1) {
      if (hit > 0) {
        await waitFor(() => {
          expect(screen.getByRole('button', { name: 'Request a hint' })).toHaveProperty(
            'disabled',
            false,
          );
        });
        fireEvent.click(screen.getByRole('button', { name: 'Request a hint' }));
        let nextCharacter: string | undefined;
        await waitFor(() => {
          const nextHint = screen.getByText(/The challenge character is/);
          nextCharacter = nextHint.textContent?.trim().at(-1);
          expect(nextCharacter).not.toBe(previousCharacter);
        });
        expect(nextCharacter).toMatch(/^[A-Z0-9]$/);
        previousCharacter = nextCharacter;
      }

      fireEvent.keyDown(terminal, { key: previousCharacter });
      await waitFor(() => {
        expect(screen.getByRole('progressbar', { name: 'Access progress' })).toHaveProperty(
          'value',
          hit + 1,
        );
      });
    }

    await waitFor(() => {
      expect(screen.getByRole('progressbar', { name: 'Access progress' })).toHaveProperty(
        'value',
        4,
      );
    });
    expect(document.querySelector('.hacking-code')?.textContent).toHaveLength(5);
    expect(screen.getByText('ACCESS GRANTED')).toBeTruthy();
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
      expect(screen.getByRole('progressbar', { name: 'Access progress' })).toHaveProperty(
        'value',
        1,
      );
    });
  });

  it('rotates four penalty characters whenever progress advances or reverses', async () => {
    renderTestPage();
    fireEvent.click(screen.getByRole('button', { name: 'Start hacking simulation' }));
    const terminal = screen.getByRole('group', { name: 'Fictional terminal display' });
    const penaltyKeys = () =>
      within(screen.getByRole('group', { name: 'Penalty characters' }))
        .getAllByText(/^[A-Z0-9]$/)
        .map((element) => element.textContent ?? '');

    const initialPenaltyKeys = penaltyKeys();
    expect(initialPenaltyKeys).toHaveLength(4);
    expect(new Set(initialPenaltyKeys).size).toBe(4);

    fireEvent.click(screen.getByRole('button', { name: 'Request a hint' }));
    const initialTarget = (await screen.findByText(/The challenge character is/)).textContent
      ?.trim()
      .at(-1);
    terminal.focus();
    fireEvent.keyDown(terminal, { key: initialTarget });
    await waitFor(() => {
      expect(screen.getByRole('progressbar', { name: 'Access progress' })).toHaveProperty(
        'value',
        1,
      );
    });

    const forwardPenaltyKeys = penaltyKeys();
    expect(forwardPenaltyKeys).toHaveLength(4);
    expect(new Set(forwardPenaltyKeys).size).toBe(4);
    expect(forwardPenaltyKeys.some((key) => initialPenaltyKeys.includes(key))).toBe(false);

    fireEvent.keyDown(terminal, { key: forwardPenaltyKeys[0] });
    await waitFor(() => {
      expect(screen.getByRole('progressbar', { name: 'Access progress' })).toHaveProperty(
        'value',
        0,
      );
    });

    const reversePenaltyKeys = penaltyKeys();
    expect(reversePenaltyKeys).toHaveLength(4);
    expect(new Set(reversePenaltyKeys).size).toBe(4);
    expect(reversePenaltyKeys.some((key) => forwardPenaltyKeys.includes(key))).toBe(false);
  });
});
