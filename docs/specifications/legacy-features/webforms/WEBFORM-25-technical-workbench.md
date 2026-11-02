# Web feature: Technical assembly workbench

**Legacy route:** `LaboratorioTecnico\NewCreazioneOggetti.aspx`.

## Observed behavior

Requires one of legacy skills 13, 66, or 132. Scans object/component QR labels, displays the selected components, waits 45 seconds, and resolves a configured item recipe. A match displays item details; incompatible components return an error and allow reset.

## Acceptance criteria

- Only characters with a configured qualifying technical skill can use the workbench; authorization is checked server-side.
- Users can add valid component identifiers, review/remove them, reset the selection, and start an assembly attempt.
- Server-side recipe rules determine the assembled item; clients cannot change component identity, quantity, or outcome by editing hidden fields.
- A valid result shows approved item name, description/effect, type, charges, cost, and optional image; invalid recipes produce a clear retryable outcome.
- Processing time is configurable, cancellation is supported, and repeated submit/network retry is idempotent.
- QR scanning uses CROSSCUT-01 and the feature is strictly an in-game simulation.
