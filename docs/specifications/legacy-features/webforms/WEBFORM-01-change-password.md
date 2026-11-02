# Web feature: Change password

**Legacy route:** `Account\ChangePassword.aspx`.

## Observed behavior

The page hosts the ASP.NET membership change-password control; its code-behind adds no custom behavior.

## Acceptance criteria

- An authenticated user can change their own credential by providing the current credential and a new credential with confirmation.
- The new credential meets documented strength and length rules; errors identify the failed requirement without revealing whether unrelated accounts exist.
- Success is persisted securely and confirmed; failure leaves the existing credential unchanged.
- The page is accessible only to the intended signed-in user and does not expose a path to change another character's credential.
