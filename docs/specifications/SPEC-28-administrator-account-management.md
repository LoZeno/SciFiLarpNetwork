# SPEC-28: Administrator account management

## Purpose and scope

Give administrators a management-client screen for the administrator accounts that [SPEC-25](SPEC-25-administrator-authentication-authorization.md) already provides in the backend: list accounts, create an account, and change an account's role. Today these work only through the API (`api/v1/admin/accounts`); the [management app guide](../architecture/avalonia-management-app.md) lists account creation and role editing as not yet provided.

This scope covers administrator accounts only. It is not player registration, Character identity, or in-game network accounts. Bootstrap and recovery stay operator procedures as defined in SPEC-25 and the [administrator guide](../architecture/administrator-authentication.md).

## Acceptance criteria

- Readers and administrators can open an **Administrator accounts** entry from the advertised navigation and see each account's user name, role, and creation time.
- Only administrators can create accounts and change roles. The screen follows the advertised actions, so readers see no change commands; the server still authorizes every request.
- Creating an account asks for a user name, a password, and a role, and applies the SPEC-25 credential rules. Passwords are never shown again, logged, or kept by the client after the request.
- Changing a role asks for confirmation. The last administrator cannot be demoted, and the screen explains why.
- Errors are shown clearly in English and Italian: invalid input, a name already used, the last administrator, an actor who is no longer an administrator, a lost race, and persistence failures.
- **Lost-race contract change (deferred from MANAGER-11, PR #8).** Account creation and role changes currently return 409 `errors.staleVersion` when another administrator command committed first (`AdministratorOperationResult.ConcurrentChange`). Change this to 409 `errors.concurrentChange`, the code the skill catalog uses for the same situation, so one code always means "another command saved shared data at the same time; nothing was saved; save again". The screen then keeps the user's input and offers to save again, as the skill dialog does. Update `docs/architecture/siren-api-profile.md`, the API tests, and `AdministratorConcurrencyTests` to match.

## Verification

- Management-client tests cover listing, the reader view without actions, account creation, role change with confirmation, the last-administrator refusal, and every error message above, including the lost-race message that keeps the user's input.
- API tests verify that a lost race on account creation and on role change returns 409 `errors.concurrentChange`, and that a real loss of the administrator role still returns 403 `errors.forbidden`.
- Maintain localization, offline behavior, and the repository's coverage requirements.
