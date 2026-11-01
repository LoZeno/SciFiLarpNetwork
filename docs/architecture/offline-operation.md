# Closed-intranet and offline operation

## Runtime boundary

Internet access is not a runtime dependency. A venue deployment must be able to start and serve the platform entirely on its local host and LAN with outbound internet access blocked. Runtime APIs, assets, fonts, icons, QR decoding, localization resources, database services, configuration, and required runtime libraries must be installed locally or shipped in the release artifacts.

Keep dependencies that are needed only to develop, test, or build separate from runtime dependencies. The frontend's npm packages are development/build tooling; production browser assets must be generated and served locally by the application deployment. Do not reference CDN scripts, stylesheets, fonts, images, QR services, public APIs, or remote telemetry from core UI assets or workflows. Never fetch application data from an external identity provider or SaaS service.

The frontend's `npm run offline:check` scans HTML, CSS, JavaScript, and TypeScript under `Frontend/src` and `Frontend/public` for statically referenced external assets. The full `npm run check` includes this guard. This static scan is a focused asset check, not proof that arbitrary computed URLs, backend code, a desktop client, or a datastore makes no outbound connections; those require component-specific review and the final isolated-network smoke test.

## Local configuration and optional connectivity

Configure backend, desktop client, and datastore endpoints with local hostnames/IP addresses and configurable ports. Do not require public DNS, cloud infrastructure, a separately managed public web server, or internet connectivity to discover local services. Core application startup and local persistence must not perform internet health checks, telemetry uploads, online license activation, or indefinite external retries.

Any future optional internet-connected feature must be off by default, explicitly identified to the operator, time-bounded, and isolated so failure cannot block local startup or core gameplay. Report local configuration and service errors distinctly; absence of public internet is not a local service error.

## Packaging, licensing, and verification

Release artifacts must include or explicitly bundle every runtime dependency required for installation, initialization, upgrade, and recovery without downloading packages at the venue. Build-time package restore may use the internet before deployment, but prepared artifacts and their documented installation path must be usable offline.

Before approving any datastore or mandatory runtime dependency, verify supported operating systems and architectures, redistribution terms, and operation without online activation or periodic internet access. Record the evidence in the datastore specification. If those conditions cannot be established, stop and request a product decision rather than choosing an online-dependent alternative.

The final offline smoke test must block outbound network traffic and demonstrate, on a supported host, application startup, local browser access to the SPA and backend, desktop management-client access, database persistence across restart, and local QR decoding. Test with public DNS and outbound routes unavailable, not merely with the test machine disconnected from Wi-Fi while another network path remains active. Run that integrated release test when the components and deployment artifacts exist; the current scaffold cannot yet satisfy it.

See [localization conventions](localization.md) for the corresponding requirement that locale resources and required UI assets remain local.
