# Web feature: Browse accounts on a breached network

**Legacy route:** `Hacking\NewHackedNetwork.aspx`.

## Observed behavior

Lists available accounts for the current hacking network, supports paging and account selection, then starts a challenge using the target's protection level. Successful attempts open the compromised-account menu; failures are recorded and return to the list.

## Acceptance criteria

- The paged account list is scoped to the active network and displays only fields allowed by game rules.
- Selecting an account starts a challenge with server-derived protection and a stable target identity; paging/reload preserves network scope.
- Successful and failed attempts are recorded against the correct actor/target and cannot be replayed as a new success.
- A successful breach opens WEBFORM-09 or its successor; failure returns to the list with a clear status and respects cooldowns.
- Network-specific labels/themes are configuration-driven and generic, not hard-coded franchise faction names.
- Challenge presentation follows CROSSCUT-02 and remains strictly an in-game visual simulation.
