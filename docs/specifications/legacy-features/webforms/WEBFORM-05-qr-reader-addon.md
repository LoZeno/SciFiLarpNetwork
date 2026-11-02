# Web feature: Standalone QR reader add-on

**Legacy route:** `AddOn\QRCodeReader\Default.aspx`.

## Observed behavior

An experimental standalone camera/QR page wires a hidden field and synthetic postback to receive decoded text. Its server handler reads the value but performs no further action.

## Acceptance criteria

- Treat this route as a prototype, not a complete business workflow; implement only if a supported standalone scanner/test utility is still needed.
- Camera permission is requested only after an explicit user action, with visible preview, decode status, stop-camera control, and clear permission/device error handling.
- A decoded value is displayed as inert text and is never executed or treated as trusted identity without a purpose-specific validation flow.
- Manual entry and an accessible scanner-free fallback are available.
- The implementation uses the progressive enhancement and privacy requirements in [QR capture](../../CROSSCUT-01-qr-capture.md).
