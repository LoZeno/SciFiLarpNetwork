# Web feature: In-character computer console

**Legacy route:** `Informatica\Informatica.aspx`.

## Observed behavior

Displays the signed-in character and protection level. It has views for encryption, hacking a selected network, and publishing a news story with a future expiry. Legacy skill checks gate publication and hacking UI; encryption/decryption callbacks are incomplete, and successful network intrusion opens an embedded account-browsing screen.

## Acceptance criteria

- The page is available only to an authenticated character and shows that character's current configured protection/status.
- In-character actions (fictional encryption, simulated network access, and publication) are shown only when the character is authorized; every command is independently authorized server-side.
- Publishing validates title, body, network, and expiry, attributes the story to the character, and records the action as an event.
- Encryption is labeled and implemented as either a real security feature or explicitly as fictional display metadata; simulated actions never substitute for actual data security.
- The console may mount the fictional presentation in CROSSCUT-02, but it does not initiate a production challenge until CROSSCUT-03 is implemented in Phase F. No route can issue shell commands, scan networks, or access infrastructure.
- Unknown network selection, failed attempts, and cancellation leave the client in a safe, understandable state; the former iframe flow becomes a normal SPA route/view.
