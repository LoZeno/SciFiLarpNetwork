# Web feature: Registration

**Legacy route:** `Account\Register.aspx`.

## Observed behavior

Uses ASP.NET membership registration; on success it creates an authentication cookie and redirects to a supplied return URL or the home page.

## Acceptance criteria

- Registration follows the configured account-provisioning policy; character/player identity is not assumed to be created merely by creating a user login.
- Inputs are validated, duplicate identities are handled safely, and credential material is stored only through the approved identity provider.
- Successful registration establishes a session only according to the product's verification policy and redirects only to a validated local destination.
- The flow is accessible, reports actionable validation errors, and does not reveal sensitive account-existence information.
