# Cross-cutting specification: QR capture and identity/code scanning

## Legacy behavior

The Web Forms QR control initializes a jQuery `WebcamQRCode` plugin, reads from a webcam, writes decoded content to a hidden field, and submits a synthetic postback. QR scans are used for character login, character/sample analysis, item and substance identification, ingredient/component selection, and datapad lookup. A standalone add-on route is only a decode prototype.

## Recommended implementation

Create a small React/TypeScript scanner adapter with a stable application-facing result type. Evaluate `@zxing/browser` as an initial candidate for camera, image, and video decoding, subject to a maintenance, security, licensing, compatibility, and bundle-size review before adoption. Feature-detect `BarcodeDetector` only as an optional progressive enhancement, not as the only implementation: MDN labels it experimental and of limited availability. Do not select `html5-qrcode` as the sole long-term dependency without a maintenance plan: its upstream README says it is in maintenance mode and seeking new owners. Keep the adapter replaceable, pin and review dependencies, and verify upstream status again at implementation time.

## Alternatives and tradeoffs

- **Browser `BarcodeDetector` API:** no package/runtime bundle for supporting browsers and can use native detection; support/format behavior is not uniform, so it must have a fallback.
- **`@zxing/browser`:** TypeScript/JavaScript browser layer over ZXing with camera/video/image decoding and explicit start/stop controls; adds a dependency and must be checked for compatibility, maintenance, licensing, and bundle size before adoption.
- **`html5-qrcode`:** offers a higher-level UI and camera/file scanning, but its upstream currently states maintenance mode; unsuitable as the only unmaintained dependency unless the project accepts a fork/ownership plan.
- **Device scanner as keyboard input:** many USB/Bluetooth readers type the code into a focused input; this is a low-friction alternative for staffed stations, but depends on hardware and must not auto-submit unvalidated content.
- **Still-image upload/manual code entry:** camera-independent fallback; decode locally in the browser where feasible and do not upload/store images unless a separate explicit requirement exists.

## Acceptance criteria

- Camera capture starts only after explicit user action, over a secure context, and after the browser grants permission. Explain denied permission, missing camera, unsupported browser, and unreadable-code states.
- Show a camera preview and controls to stop scanning, switch available cameras, and retry. Stop media tracks and release resources on successful decode, cancellation, route change, and component disposal.
- Request the least camera capability needed (prefer environment/back camera on mobile when available); do not record, upload, or persist frames.
- Provide manual entry and optional still-image or keyboard-wedge scanner alternatives. All decoded/entered strings are untrusted and validated by the relevant server-side workflow.
- Do not put secret QR values in URLs, analytics, console logs, or error telemetry. Login QR tokens are high-entropy, short-lived, revocable, purpose-limited, and protected against replay; public character/item identifiers are not login credentials.
- Prevent duplicate decode callbacks from creating duplicate commands; display a clear result and require explicit rescan for the next item.
- Adapter contract and domain workflows are tested independently: valid payload, malformed payload, wrong-purpose payload, repeated decode, permission denial, no camera, cancellation, and route disposal.
- Benchmark reliable scan performance on the supported browsers/devices and document required HTTPS/secure-context setup.
- QR decoding libraries and scanner UI are bundled or served by the local host; decoding occurs on the device and never calls a third-party internet service. A scan succeeds on the isolated LAN with outbound internet blocked.
- Document how camera permission's secure-context requirement is met on a venue LAN without public DNS or an internet-based certificate service. Provide manual entry, keyboard-wedge scanner, or locally decoded still-image fallback on devices where browser camera access cannot be enabled.

## Reference material consulted

- MDN: [`BarcodeDetector`](https://developer.mozilla.org/en-US/docs/Web/API/BarcodeDetector) (experimental/limited availability).
- [`@zxing/browser`](https://github.com/zxing-js/browser) upstream browser-layer documentation.
- [`html5-qrcode`](https://github.com/mebjas/html5-qrcode) upstream maintenance-status notice.

The upstream documentation was checked on 2026-10-30; reassess project health before selecting a dependency.
