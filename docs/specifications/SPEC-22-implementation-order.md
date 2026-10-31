# SPEC-22: Implementation order and agent handoff

## Purpose

This specification defines the canonical implementation sequence for the product specifications, QR/hacking cross-cutting specifications, every legacy manager tab, and every active legacy Web Forms page. It is the shared handoff point for work performed by different agents at different times.

The sequence is deliberately one task at a time. Do not start a later row while an earlier row is unchecked. A task may be completed by verifying that its acceptance criteria are already fully satisfied by previously delivered work; do not duplicate functionality merely to produce a change for that row.

## Handoff protocol

1. Read this file and inspect the repository/worktree before changing code. Do not assume prior chat history.
2. Find the **first unchecked row** in the sequence. That row is the only implementation task to start. The next agent uses the same rule.
3. Read that specification and its cited prerequisites in full. Follow SPEC-15, the specification-first workflow, and keep the change limited to the selected task and directly necessary dependencies.
4. If the task is blocked by an ambiguity, conflict, unavailable dependency, or unmet acceptance criterion, do not skip it. Record the blocker in the handoff/PR and ask for a decision where needed.
5. Mark the row `[x]` only after all acceptance criteria for that specification are implemented or demonstrably satisfied, relevant tests pass, and the changes are persisted in the branch/commit/PR being handed off. Otherwise leave it `[ ]`.
6. In the handoff, state the completed row number and spec, verification performed, any remaining limitation, and the next unchecked row. Do not mark later rows complete just because some of their code was incidentally introduced.
7. Work serially against this checklist: do not have multiple agents implement different rows concurrently. Shared domain/API changes can create hidden dependencies and invalidate the handoff state.

All rows below begin unchecked. The previous `00-legacy-analysis.md` records completed repository analysis; it is discovery evidence, not an implementation task. `WEBFORM-INDEX.md` is an index, not a feature specification.

## Canonical sequence

### Phase A — repository, product constraints, architecture, and test foundation

| # | Status | Specification | Implementation gate |
|---:|:---:|---|---|
| 01 | [x] | [SPEC-01 Repository and layout](SPEC-01-repository-location.md) | Establish the root solution and reserved source/test/deployment layout without changing the legacy repository; actual project files are added by later tasks. |
| 02 | [ ] | [SPEC-12 Greenfield replacement](SPEC-12-greenfield-replacement.md) | Deliberately retain or remove old destination contents; preserve Git history and metadata. |
| 03 | [ ] | [SPEC-15 Specification-first workflow](SPEC-15-specification-first-workflow.md) | Use the specification selection and acceptance process throughout implementation. |
| 04 | [ ] | [SPEC-02 Generic Sci-Fi LARP product](SPEC-02-generic-sci-fi-larp-product.md) | Establish generic terminology, product identity, and in-scope domain capability vocabulary. |
| 05 | [ ] | [SPEC-03 .NET 10](SPEC-03-dotnet-10.md) | Establish the SDK-style .NET 10 project baseline. |
| 06 | [ ] | [SPEC-13 Idiomatic modern C#](SPEC-13-idiomatic-csharp.md) | Establish C# language, nullable, analyzer, and formatting conventions. |
| 07 | [ ] | [SPEC-14 Idiomatic TypeScript](SPEC-14-idiomatic-typescript.md) | Establish strict TypeScript, React, lint, and formatting conventions. |
| 08 | [ ] | [SPEC-20 Application language and localization](SPEC-20-language-and-localization.md) | Establish English-default, English/Italian resources and extensible localization conventions before UI strings proliferate. |
| 09 | [ ] | [SPEC-21 Offline-capable closed-intranet operation](SPEC-21-offline-intranet-operation.md) | Establish offline-first dependency, asset, licensing, and outbound-network constraints before selecting runtime dependencies. |
| 10 | [ ] | [SPEC-07 Cross-platform and architecture support](SPEC-07-cross-platform-support.md) | Verify the proposed platform/runtime/dependency matrix and resolve unsupported combinations before relying on them. |
| 11 | [ ] | [SPEC-11 xUnit testing](SPEC-11-xunit-testing.md) | Establish .NET test conventions, integration-test isolation, and frontend testing alongside its own test runner. |
| 12 | [ ] | [SPEC-10 Minimum code coverage](SPEC-10-code-coverage.md) | Establish separate coverage reporting and the 80% CI threshold before feature implementation. |
| 13 | [ ] | [SPEC-08 Event-sourced domain](SPEC-08-event-sourced-domain.md) | Define command/event/stream/projection boundaries and domain invariants. |
| 14 | [ ] | [SPEC-09 Event store and projections](SPEC-09-event-store-and-projections.md) | Verify the approved RavenDB direction against offline, license, platform, event, and projection requirements. |
| 15 | [ ] | [SPEC-05 ASP.NET Core MVC backend](SPEC-05-aspnetcore-mvc-backend.md) | Establish the .NET 10 API, contracts, validation, error codes, configuration, and documentation baseline. |
| 16 | [ ] | [SPEC-04 React and TypeScript web client](SPEC-04-react-typescript-spa.md) | Establish the locally served SPA, API client, routing, localization, and UI test baseline. |
| 17 | [ ] | [SPEC-06 Avalonia management application](SPEC-06-avalonia-management-app.md) | Establish the cross-platform administrative client, API integration, localization, and test baseline. |

### Phase B — shared interaction foundations

| # | Status | Specification | Implementation gate |
|---:|:---:|---|---|
| 18 | [ ] | [CROSSCUT-01 QR capture and identity/code scanning](CROSSCUT-01-qr-capture.md) | Deliver the offline-capable scanner adapter and fallbacks before any QR-dependent workflow. |
| 19 | [ ] | [CROSSCUT-02 Theatrical hacking simulation](CROSSCUT-02-cinematic-hacking.md) | Deliver the fictional-only, server-validated challenge before any hacking workflow. |

### Phase C — administrative catalog and event capabilities

Implement the manager features first so that the game catalog and event data required by player-facing workflows can be configured.

| # | Status | Specification | Implementation gate |
|---:|:---:|---|---|
| 20 | [ ] | [MANAGER-01 Players and characters](legacy-features/manager-tabs/MANAGER-01-players-characters.md) | Establish player/character administration and identity/QR management. |
| 21 | [ ] | [MANAGER-02 Skill groups and skills](legacy-features/manager-tabs/MANAGER-02-skills.md) | Establish configurable skills used by permissions and later gameplay features. |
| 22 | [ ] | [MANAGER-03 Events and participation](legacy-features/manager-tabs/MANAGER-03-events.md) | Establish event/day/registration and participant administration. |
| 23 | [ ] | [MANAGER-04 Messages](legacy-features/manager-tabs/MANAGER-04-messages.md) | Establish administrative message inspection, creation, and replies. |
| 24 | [ ] | [MANAGER-05 News](legacy-features/manager-tabs/MANAGER-05-news.md) | Establish network news creation, scheduling, and editing. |
| 25 | [ ] | [MANAGER-06 Items and components](legacy-features/manager-tabs/MANAGER-06-items-components.md) | Establish item catalog, composition, QR identity, and print data. |
| 26 | [ ] | [MANAGER-07 Substances and ingredients](legacy-features/manager-tabs/MANAGER-07-substances-ingredients.md) | Establish substance/ingredient catalog, recipes/components, QR identity, and print data. |
| 27 | [ ] | [MANAGER-08 Datapads and files](legacy-features/manager-tabs/MANAGER-08-datapads-files.md) | Establish datapad and associated file administration. |
| 28 | [ ] | [MANAGER-09 Character conditions](legacy-features/manager-tabs/MANAGER-09-infections.md) | Establish condition catalog and character assignment management. |
| 29 | [ ] | [MANAGER-10 Event print management](legacy-features/manager-tabs/MANAGER-10-event-printing.md) | Integrate event assignments with item, substance, and datapad print workflows. |

### Phase D — Web Forms-derived authentication, network selection, and application shell

These are capabilities to migrate into the React SPA/backend, not a requirement to recreate ASP.NET Web Forms routes or their security behavior.

| # | Status | Specification | Implementation gate |
|---:|:---:|---|---|
| 30 | [ ] | [WEBFORM-39 Network selection](legacy-features/webforms/WEBFORM-39-network-selection.md) | Network selection and trusted local availability rules precede network-scoped sign-in. |
| 31 | [ ] | [WEBFORM-03 Membership login](legacy-features/webforms/WEBFORM-03-membership-login.md) | Establish supported authentication and safe return navigation. |
| 32 | [ ] | [WEBFORM-28 Default network sign-in](legacy-features/webforms/WEBFORM-28-default-network-login.md) | Implement the configurable default-network route. |
| 33 | [ ] | [WEBFORM-27 Configured network sign-in (legacy FPI)](legacy-features/webforms/WEBFORM-27-faction-login-independent-planets.md) | Implement or explicitly alias the first legacy network-specific entry route. |
| 34 | [ ] | [WEBFORM-29 Configured network sign-in (legacy Empire)](legacy-features/webforms/WEBFORM-29-faction-login-empire.md) | Implement or explicitly alias the second legacy network-specific entry route. |
| 35 | [ ] | [WEBFORM-30 Configured network sign-in (legacy Republic)](legacy-features/webforms/WEBFORM-30-faction-login-republic.md) | Implement or explicitly alias the third legacy network-specific entry route. |
| 36 | [ ] | [WEBFORM-31 QR login prototype](legacy-features/webforms/WEBFORM-31-qr-login-prototype.md) | Complete or retire the prototype using the secure identity design and CROSSCUT-01. |
| 37 | [ ] | [WEBFORM-04 Registration](legacy-features/webforms/WEBFORM-04-registration.md) | Implement the approved account provisioning/verification flow. |
| 38 | [ ] | [WEBFORM-01 Change password](legacy-features/webforms/WEBFORM-01-change-password.md) | Implement authenticated self-service credential changes. |
| 39 | [ ] | [WEBFORM-02 Password-change confirmation](legacy-features/webforms/WEBFORM-02-password-changed.md) | Tie confirmation to a completed password-change result. |
| 40 | [ ] | [WEBFORM-26 End session](legacy-features/webforms/WEBFORM-26-end-session.md) | Invalidate authentication, character, and breach context. |
| 41 | [ ] | [WEBFORM-38 Character home](legacy-features/webforms/WEBFORM-38-character-home.md) | Deliver the authenticated character landing view and permission-aware navigation. |
| 42 | [ ] | [WEBFORM-37 About and version](legacy-features/webforms/WEBFORM-37-about.md) | Display product/version metadata from the built application. |
| 43 | [ ] | [WEBFORM-05 Standalone QR reader add-on](legacy-features/webforms/WEBFORM-05-qr-reader-addon.md) | Complete as a useful scanner utility or retire with an explicit replacement/route decision. |

### Phase E — Web Forms-derived player-facing domain features

| # | Status | Specification | Implementation gate |
|---:|:---:|---|---|
| 44 | [ ] | [WEBFORM-34 Contacts](legacy-features/webforms/WEBFORM-34-contacts.md) | Establish character-owned contacts before message recipient selection. |
| 45 | [ ] | [WEBFORM-35 Legacy contacts list](legacy-features/webforms/WEBFORM-35-contacts-legacy.md) | Redirect/alias to WEBFORM-34 or explicitly retire the duplicate route. |
| 46 | [ ] | [WEBFORM-33 Mailbox and messaging](legacy-features/webforms/WEBFORM-33-messages.md) | Implement inbox/outbox, compose, reply, save, delete, and read state. |
| 47 | [ ] | [WEBFORM-32 Legacy mailbox](legacy-features/webforms/WEBFORM-32-messages-legacy.md) | Redirect/alias to WEBFORM-33 or explicitly retire the duplicate mailbox. |
| 48 | [ ] | [WEBFORM-08 Personal files](legacy-features/webforms/WEBFORM-08-personal-files.md) | Implement character-owned files and the agreed fictional encryption behavior. |
| 49 | [ ] | [WEBFORM-36 Network news feed](legacy-features/webforms/WEBFORM-36-news-feed.md) | Display active, correctly scoped network news. |
| 50 | [ ] | [WEBFORM-17 Manual datapad reader](legacy-features/webforms/WEBFORM-17-manual-datapad-reader.md) | Implement the scanner-independent access-code path. |
| 51 | [ ] | [WEBFORM-18 QR datapad reader](legacy-features/webforms/WEBFORM-18-qr-datapad-reader.md) | Add datapad scanning, file selection, and list/detail navigation using CROSSCUT-01. |
| 52 | [ ] | [WEBFORM-19 Identify an item](legacy-features/webforms/WEBFORM-19-identify-item.md) | Resolve and display allowed item data from a validated code. |
| 53 | [ ] | [WEBFORM-20 Identify a substance or ingredient](legacy-features/webforms/WEBFORM-20-identify-substance.md) | Implement skill-gated, category-scoped identification. |
| 54 | [ ] | [WEBFORM-21 Identify a toxin or drug](legacy-features/webforms/WEBFORM-21-identify-toxin.md) | Implement skill-gated toxin/drug identification and safe fictional framing. |
| 55 | [ ] | [WEBFORM-06 Manual medical analysis](legacy-features/webforms/WEBFORM-06-manual-medical-analysis.md) | Implement the manual sample-code path and configured simulated result. |
| 56 | [ ] | [WEBFORM-07 QR sample medical analysis](legacy-features/webforms/WEBFORM-07-qr-medical-analysis.md) | Add QR sample analysis using CROSSCUT-01 and server-side authorization. |
| 57 | [ ] | [WEBFORM-23 Biochemistry workbench](legacy-features/webforms/WEBFORM-23-biochemistry-lab.md) | Implement skill-gated ingredient recipe analysis. |
| 58 | [ ] | [WEBFORM-24 Chemistry workbench](legacy-features/webforms/WEBFORM-24-chemistry-lab.md) | Implement skill-gated chemical recipe analysis. |
| 59 | [ ] | [WEBFORM-25 Technical assembly workbench](legacy-features/webforms/WEBFORM-25-technical-workbench.md) | Implement skill-gated item assembly from catalogued components. |
| 60 | [ ] | [WEBFORM-22 In-character computer console](legacy-features/webforms/WEBFORM-22-computer-console.md) | Integrate fictional encryption/publication and the approved hacking challenge into the SPA. |

### Phase F — Web Forms-derived simulated account intrusion

All routes in this phase depend on CROSSCUT-02, server-side authorization, account/network data, and the relevant underlying message/news/file capabilities. Each category must remain a game-only simulation.

| # | Status | Specification | Implementation gate |
|---:|:---:|---|---|
| 61 | [ ] | [WEBFORM-16 Browse accounts on a breached network](legacy-features/webforms/WEBFORM-16-network-account-list.md) | List eligible targets in the selected configured network. |
| 62 | [ ] | [WEBFORM-13 Select a network account to breach](legacy-features/webforms/WEBFORM-13-hack-network-account.md) | Challenge a selected target and establish an expiring, audited breach context. |
| 63 | [ ] | [WEBFORM-09 Compromised account menu](legacy-features/webforms/WEBFORM-09-hacked-account-menu.md) | Present only supported, authorized target data categories after a breach. |
| 64 | [ ] | [WEBFORM-10 Compromised contacts](legacy-features/webforms/WEBFORM-10-hacked-contacts.md) | Implement a clearly scoped contact view or explicitly omit it from the menu. |
| 65 | [ ] | [WEBFORM-11 Compromised personal files](legacy-features/webforms/WEBFORM-11-hacked-files.md) | Implement the authorized file challenge, read/copy rules, and audit events. |
| 66 | [ ] | [WEBFORM-12 Compromised incoming messages](legacy-features/webforms/WEBFORM-12-hacked-inbox.md) | Implement the target inbox challenge, decryption rules, and copy semantics. |
| 67 | [ ] | [WEBFORM-14 News on a breached network](legacy-features/webforms/WEBFORM-14-hacked-news.md) | Implement protected story viewing/editing and auditable updates. |
| 68 | [ ] | [WEBFORM-15 Compromised outgoing messages](legacy-features/webforms/WEBFORM-15-hacked-outbox.md) | Implement the target outbox challenge and authorized copy semantics. |

### Phase G — deployment, release packaging, and operator documentation

| # | Status | Specification | Implementation gate |
|---:|:---:|---|---|
| 69 | [ ] | [SPEC-18 Self-hosted deployment](SPEC-18-self-hosted-deployment.md) | Deliver a complete locally hosted stack for a normal computer and isolated venue LAN. |
| 70 | [ ] | [SPEC-19 Container-based hosting](SPEC-19-container-deployment.md) | Add optional redistributable containers/deployment artifacts without replacing self-hosting. |
| 71 | [ ] | [SPEC-16 Cross-platform CI builds and packaging](SPEC-16-ci-build-and-packaging.md) | Build, test, smoke-test, and package the final components/database setup across the verified matrix, including offline validation. |
| 72 | [ ] | [SPEC-17 Configuration documentation in README](SPEC-17-configuration-readme.md) | Finish and verify operator instructions for every component, platform, language, and hosting mode. |

## Continuous requirements (not separate queue entries)

- Run the existing checks and preserve the coverage thresholds from SPEC-10 for each completed implementation task; do not postpone testing until Phase G.
- Maintain English-default and English/Italian localization (SPEC-20), closed-intranet operation (SPEC-21), supported platform claims (SPEC-07), and event-sourcing rules (SPEC-08/SPEC-09) throughout every phase.
- Update CI incrementally as projects/features are introduced; row 71 is the final cross-component packaging, release-matrix, and offline-validation completion gate, not permission to defer basic builds or tests.
- Keep the manager and Web Forms features as generic product workflows. Legacy file names and route names in this roadmap identify source behavior only; they are not required product branding or architecture.
