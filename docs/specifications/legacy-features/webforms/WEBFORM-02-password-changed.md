# Web feature: Password-change confirmation

**Legacy route:** `Account\ChangePasswordSuccess.aspx`.

## Observed behavior

Static success route for the membership change-password flow; its code-behind has no custom logic.

## Acceptance criteria

- Show a clear confirmation only after a successful password-change operation.
- Provide an accessible route back to sign-in or the authenticated application.
- Direct navigation to this route must not be presented as proof that a credential change occurred; confirmation is tied to the completed operation.
