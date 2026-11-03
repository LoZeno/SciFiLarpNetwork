# SPEC-23: HTTPS certificates for self-hosted and venue deployments

## Requirement

Provide secure HTTPS for the self-hosted application on a venue LAN, including browser camera access, without requiring a dedicated web server or continuous internet access. Operators must be able to choose either publicly trusted certificates from Let's Encrypt or a venue-managed private certificate authority using `step-ca`.

## Certificate options

- **Let's Encrypt:** Use a domain name controlled by the association and automated certificate issuance/renewal. DNS-01 validation is the recommended option when the host is not publicly reachable; it requires internet access to Let's Encrypt and the DNS provider during issuance and renewal, plus narrowly scoped DNS API credentials when automated. HTTP-01 is acceptable only when its public reachability requirements are met. Venue-local DNS must resolve the certificate's domain name to the host's private address during events. Do not use `.local`, an unowned domain, or a private IP address as a Let's Encrypt certificate identity.
- **`step-ca`:** Provide a locally hosted, venue-managed CA for local DNS names or addresses and offline operation. Protect and back up CA state, keep the root signing key offline where the chosen deployment permits, and use an issuing intermediate for routine certificates. Participating devices must trust the CA root before browsers will accept its server certificates.

The options are alternatives, not prerequisites for each other. The operator chooses the model that fits the association's domain ownership, internet availability, and ability to install trust on participant devices.

## Acceptance criteria

- Provide documented, repeatable automation scripts for **both** Let's Encrypt and `step-ca`, so an operator can select either option. Scripts cover initial setup, certificate issuance and renewal, installation/configuration for the self-hosted frontend/backend endpoint, status/expiry checks, and safe service reload or restart after renewal.
- Automation is suitable for the supported host operating systems in SPEC-07, is safe to rerun, reports actionable errors, and never prints or commits private keys, DNS API tokens, passwords, or other secrets. Document secure secret storage and least-privilege DNS credentials.
- The Let's Encrypt workflow supports DNS-01 for a private venue host, explains the requirement for an association-controlled public domain and internet/DNS-provider access while issuing or renewing, and documents HTTP-01's public reachability constraints if offered. It must not depend on internet access during the event itself.
- Document that a certificate renewed while the host is connected to the internet remains usable during a later offline event until its `NotAfter` expiry, provided venue DNS resolves the certificate name to the host and client devices can reach it. Include a renewal schedule, expiry warning/recovery guidance, and a contingency for events beyond certificate validity.
- The `step-ca` workflow supports certificate issuance/renewal on the venue network without public DNS or a public certificate service. Document secure CA initialization, backups/recovery, certificate SANs and local DNS, and a safe way to distribute and install only the public CA root certificate on supported client devices. Never distribute the CA private key to clients.
- Serve the React application and API over HTTPS with certificate names matching the URL used by clients; preserve secure certificate validation and avoid asking users to bypass browser warnings. Cover any required proxy or Kestrel configuration without making a separately installed/configured web server mandatory under SPEC-18.
- Document browser camera secure-context behavior, including that `localhost` only qualifies on the host device, and provide the manual/image-scanning fallback when HTTPS trust cannot be provisioned.
- Include tests or validation procedures for configuration generation, certificate identity/expiry, renewal and service reload, certificate trust, and offline event operation. Record platform-specific smoke-test evidence before claiming support.
- Update the self-hosted deployment and operator README instructions (SPEC-18 and SPEC-17) with both choices, their prerequisites, setup, renewal, troubleshooting, and offline limitations.

## Operational note: renewing before an offline event

Let's Encrypt works for an offline event if the host obtains or renews its certificate while it has internet access and the certificate remains valid for the event. For a host that is not publicly reachable, DNS-01 can prove control of an association-owned domain through its DNS provider; during the event, local DNS can map that same name to the host's private address. Participant devices then use the domain name in the URL and trust the publicly issued certificate without installing a private CA.

Renewal does not work while fully offline: schedule it during the connected period with enough margin for retries, and monitor the expiry date. If the certificate expires before the next connection, browsers will reject it. This approach requires continued control of the domain and DNS account; `step-ca` is the alternative where those internet or public-domain dependencies are undesirable.
