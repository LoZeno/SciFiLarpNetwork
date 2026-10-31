# Web feature: Chemistry workbench

**Legacy route:** `LaboratorioChimico\NewLaboratorioChimico.aspx`.

## Observed behavior

Requires legacy skill ID 1. Scans substance ingredient labels, lists chosen components, waits 45 seconds after submission, and resolves a configured recipe. The legacy UI accepts types 0 or 1 and resets after invalid combinations.

## Acceptance criteria

- Access requires the configured chemistry skill/permission, checked on every server request.
- Users can scan or otherwise select valid component identifiers, inspect/remove selected components, and reset the attempt.
- Only a server-defined valid recipe can produce a result; the client cannot declare a product or alter component identities.
- A valid result displays approved substance metadata; an unknown/incompatible composition returns a clear recoverable result and retains or clears components according to a documented UX rule.
- Processing delay is configurable and server-controlled, and duplicate submissions do not create duplicate outcomes.
- Camera denial/unavailability is supported through CROSSCUT-01; this is fictional gameplay, not real chemistry advice.
