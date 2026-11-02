# Web feature: Compromised incoming messages

**Legacy route:** `Hacking\HackedIncoming.aspx`.

## Observed behavior

Lists target inbox entries, including recently deleted entries from the prior six months; deleted entries receive increased hacking difficulty. Selecting a message shows sender/title/body, supports simulated decryption, and allows copying after a successful hacking challenge. Legacy outgoing-display and crypto callbacks include unimplemented methods.

## Acceptance criteria

- A breached account's incoming message list is target-scoped and filtered according to configurable retention/deletion rules.
- Deleted-message status and any additional protection are applied by the domain service; client parameters cannot lower difficulty.
- Message details are not revealed until required decryption/gameplay rules pass; successful decryption has a complete, tested outcome.
- A successful copy records the message in the hacker's personal files and appends an auditable event; failure does not expose content.
- Paging, empty inbox, missing/expired breach context, deleted messages, and long content are handled accessibly.
- Any unsupported legacy response/decryption behavior is resolved explicitly instead of retaining a `NotImplemented` path.
