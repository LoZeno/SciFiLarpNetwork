# Web feature: Network selection

**Legacy route:** `SelezioneRete.aspx`.

## Observed behavior

On first load, asks the network manager for networks available to the workstation's IPv4 address and displays their faction names.

## Acceptance criteria

- Display only networks configured as available to the current deployment/workstation policy.
- Network availability is not inferred from an untrusted client-supplied IP header; proxy/trusted-network policy is explicitly configured.
- The user can select a network and proceed to its generic login flow; the selected network is checked again server-side at authentication and authorization.
- Empty results, unavailable configuration, and multiple networks have clear UI states.
- Network labels and themes are configurable and franchise-neutral; no user is silently assigned a network from an untrusted address.
