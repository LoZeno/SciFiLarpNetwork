# SPEC-25: Local administrator authentication and authorization

## Purpose and scope

Provide the identity and authorization foundation that protects administrative operations in the Avalonia management client and any administrator-only backend workflow. It follows [SPEC-24](SPEC-24-event-store-runtime.md) so authorized changes can be attributed to an authenticated administrator and durably recorded.

This scope provisions and authenticates administrative application accounts for a self-hosted, offline-capable installation. It is not player registration, a Character identity, or an in-game network account. Later player membership sign-in may reuse the account/session infrastructure, but it must have explicit provisioning and authorization rules of its own.

## Acceptance criteria

- Administrators authenticate using credentials validated locally by the backend; routine operation does not require an external identity provider, SaaS, public DNS, or internet access.
- Define a secure first-administrator bootstrap and recovery process for an operator-controlled installation. Do not ship default credentials, source-controlled secrets, or an unauthenticated public setup endpoint. Bootstrap credentials are one-time, short-lived, and never written to logs.
- Store only an established, appropriately salted password verifier using a maintained .NET identity/cryptography implementation. Passwords, session tokens, bootstrap secrets, and recovery secrets are never persisted in event payloads, emitted to logs, or returned in API representations.
- Issue revocable server-validated sessions with documented expiry, renewal, logout, and credential-reset behavior. Keep credentials and session tokens out of URLs and avoid browser storage accessible to arbitrary scripts. Protect cookie-authenticated state-changing requests against CSRF; the native management client uses a supported secure session flow and does not log or persist bearer/session tokens in plaintext.
- Enforce administrator authorization server-side on every management query and command. The UI may hide unavailable actions but is not an authorization boundary. Unknown, unauthenticated, expired, or insufficiently authorized sessions fail with the API's stable Siren error contract.
- Provide least-privilege administrator roles/permissions sufficient to distinguish read and write administrative access where needed. Role changes are administrator-authorized and auditable; clients cannot self-assign roles or submit actor identities.
- Associate accepted administrative commands and resulting events with the authenticated administrator identity. Authentication and authorization decisions do not create misleading success responses or sensitive event data.
- Keep Player, Character, and Account identities distinct. Linking an administrator account to a player is explicit and is not inferred from matching names, email addresses, or character data.
- The Avalonia application can establish, use, expire, and clear the authenticated session against a configured local backend. The backend remains authoritative for authorization and every operation can be performed without a third-party identity service.

## Verification

- xUnit tests cover successful and failed authentication, bootstrap single-use/expiry, session expiry/revocation/logout, role authorization, actor attribution, and refusal to accept client-supplied identity/roles.
- API integration tests prove all administrative write routes reject anonymous and insufficiently authorized callers, including direct requests that bypass client navigation.
- Tests verify credential verifiers are not recoverable plaintext and secrets are absent from event payloads, logs, URLs, and API responses.
- Management-client tests cover authentication, protected requests, expired-session handling, logout, and safe settings/token handling.
- Maintain localization, offline behavior, and the repository's coverage requirements.
