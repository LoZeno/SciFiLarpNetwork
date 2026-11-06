# Manager capability: Configurable networks

This capability is a new product prerequisite derived from the network-scoped workflows in WEBFORM-39 and the network-dependent manager and player features. It is not a request to reproduce franchise-specific factions.

## Purpose

Allow authorized administrators to define the generic in-game networks that characters may belong to and that player-facing workflows may select. The network catalog supplies stable references for Players and Characters, news, authentication, and later simulated challenges.

## Acceptance criteria

- Administrators can list, search, create, and edit networks using stable identifiers and generic, setting-configurable display names. Optional visual themes are presentation metadata only and never grant authorization.
- Validation rejects blank or duplicate identifiers/names according to the documented uniqueness rules and preserves existing character, event, news, and other references during edits.
- Administrators can enable or disable a network. Deactivation prevents new selection or assignment where policy requires it but does not delete history, rewrite references, or silently move characters.
- Define the configured default network explicitly; enforce at most one default and require an intentional choice when none or multiple candidates exist.
- Network availability is deployment/workstation policy. Do not infer it from untrusted client-supplied IP headers. If address-based availability is supported, it uses explicitly configured trusted proxy and network rules.
- Network assignment and availability are revalidated server-side for every relevant command and query; client-provided network IDs are not proof of membership or permission.
- Network create/edit/activation/default changes are authorized, validated, auditable event-sourced commands with optimistic concurrency and clear conflict feedback.
- The manager UI, backend API, and English/Italian resources handle empty catalogs, disabled networks, invalid input, stale edits, and persistence failures clearly.
- The network catalog can be configured on an isolated local deployment with no external identity or network service.

## Verification

- xUnit tests cover validation, unique/default-network invariants, deactivation with existing references, authorization, event emission, and concurrency conflicts.
- API and management-client tests verify discovery/action behavior, safe network selection, and that untrusted address headers or client network IDs cannot bypass server policy.
