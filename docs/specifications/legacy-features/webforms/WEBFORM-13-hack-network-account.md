# Web feature: Select a network account to breach

**Legacy route:** `Hacking\HackedNetwork.aspx`.

## Observed behavior

Lists living characters in the selected network/faction and accepts a target character number. It configures target protection, starts the shared hacking control, records success/failure, and stores the breached account for subsequent pages.

## Acceptance criteria

- The user sees only eligible targets on the active network and cannot breach a character outside that network by changing a submitted identifier.
- Target selection supports search/list selection and validates that the target still exists and remains eligible when the challenge starts.
- Protection and character hacking level come from authoritative server-side data; failed attempts show a safe result and apply configured cooldowns.
- A successful attempt creates a scoped breach session and records actor, target, network, time, and outcome; expiration/logout invalidates the session.
- No password or secret override value is exposed in the URL, client logs, or displayed fake terminal text.
- The challenge uses the fictional-only experience in CROSSCUT-02, not actual hacking functions.
