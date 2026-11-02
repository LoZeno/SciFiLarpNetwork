# Web feature: Compromised outgoing messages

**Legacy route:** `Hacking\HackedOutgoing.aspx`.

## Observed behavior

Lists the target's outgoing messages (including recently deleted entries from the prior six months), raises difficulty for deleted entries, and can display a selected message with simulated decryption and a post-challenge save/copy action. Legacy display/decryption callbacks are partly unimplemented.

## Acceptance criteria

- The list is limited to the breached target's outgoing messages and applies configured retention/deletion rules.
- The user can inspect recipients and message content only after configured decryption/access checks.
- Any copy operation is explicit, creates an attributed copy in the hacker's files, and records a durable event.
- Failed access leaves message state unchanged, records the configured result, and presents no message contents.
- Replace every unsupported legacy callback with defined behavior or remove the unsupported action.
- All message access and challenge state are verified server-side and use the fictional-only simulation in CROSSCUT-02.
