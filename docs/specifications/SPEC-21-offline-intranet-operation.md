# SPEC-21: Offline-capable closed-intranet operation

## Requirement

All components—React frontend, .NET backend, Avalonia management application, database/event store, and their required runtime services—must operate on a closed local network with no guaranteed access to the public internet. A LARP deployment must be able to run the system locally even when the venue has no internet connection.

The internet may be available during development or deployment preparation, but it must not be a runtime dependency for core application features. All required application assets, libraries, configuration, and database services must be locally available after installation.

## Acceptance criteria

- After installation, the host and client applications can start, authenticate, load the UI, access the database, and use core workflows with outbound internet connectivity physically unavailable.
- No runtime workflow requires third-party SaaS, external identity providers, public DNS, CDN-hosted JavaScript/CSS/fonts, external API calls, remote telemetry, or online license activation.
- Frontend assets, localization resources, fonts/icons, QR-decoding code, and other required dependencies are bundled or served locally. No remote resource is silently required for the application to render or function.
- Backend, management client, and datastore use configurable local hostnames/IPs and ports; the local setup does not require public DNS, cloud services, or a separately managed internet service.
- Build/release artifacts and container images include or clearly package all runtime dependencies required for offline installation. Installation, initialization, upgrade, and recovery procedures can be completed using locally available artifacts.
- Any optional internet-connected feature is disabled by default, clearly identified as optional, and fails without affecting core functionality when offline.
- Startup and operational diagnostics clearly distinguish local configuration/service failures from lack of public internet and do not attempt indefinite external retries.
- A documented offline smoke test blocks outbound network access and demonstrates host startup, browser access, desktop-client access, database persistence, and QR scanning.
- Database licensing and operation are verified to work without online activation or periodic internet access. If the selected datastore or any mandatory dependency cannot meet this requirement, stop and request approval for an alternative before implementation proceeds.

## Baseline implementation

- [Closed-intranet and offline operation](../architecture/offline-operation.md) defines runtime/build dependency boundaries, local-service configuration, optional connectivity policy, licensing review, and the final outbound-blocked smoke test.
- `Frontend/scripts/check-offline-assets.mjs` detects remote resource references in frontend HTML/CSS under `Frontend/src` and `Frontend/public`. The frontend `npm run check` includes this guard.
- Backend, Avalonia, datastore, release packaging, and integrated outbound-blocked runtime verification remain acceptance work for their scheduled implementation/deployment tasks; the current empty application scaffold cannot yet demonstrate those behaviors.
