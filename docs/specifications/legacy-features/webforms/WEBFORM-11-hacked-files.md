# Web feature: Compromised personal files

**Legacy route:** `Hacking\HackedFiles.aspx`.

## Observed behavior

Loads the target account's saved files, shows encrypted content as generated filler, offers a decryption control, and lets the hacker attempt to copy a selected file into their own saved files. Success/failure is intended to be recorded, although the legacy file-attempt registration method is empty.

## Acceptance criteria

- Only a successfully breached target's files are listed; every read/copy action validates target context and permissions server-side.
- Encrypted files remain locked until the configured game challenge succeeds; ciphertext/display masking is clearly distinguished from real data encryption.
- A successful, authorized theft/copy creates an attributable copy in the hacker's personal files and records the source, actor, target, time, and outcome in the event stream.
- Failed attempts record failure and follow configured cooldown/alert rules; they do not disclose the file contents.
- The user can distinguish empty, encrypted, copied, and unavailable files, with no silent no-op action.
- Challenge presentation follows CROSSCUT-02 and cannot cause real system commands or execute displayed code.
