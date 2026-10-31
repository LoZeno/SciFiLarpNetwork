# Web feature: Biochemistry workbench

**Legacy route:** `LaboratorioBiochimico\NewLaboratorioBiochimico.aspx`.

## Observed behavior

Requires legacy skill ID 2. Scans substance labels as ingredients, appends them to a selected-ingredient list, waits 45 seconds after “create,” and resolves the ingredient set to configured substance data. A result is accepted only for types 0, 2, or 3; reset clears the list and result.

## Acceptance criteria

- Only authorized characters can use the workbench, enforced server-side.
- Users can scan multiple valid ingredient labels, see the ordered ingredient set, remove/reset entries, and submit an explicit analysis attempt.
- The server resolves the submitted ingredient identities against configured recipes; client-supplied names or hidden IDs cannot fabricate a recipe.
- A valid configured result displays its approved name, category, effects, usage, efficacy, expiry, cost, and optional image; invalid combinations give a clear no-result outcome.
- Simulated processing duration is configurable and cancellation/reset prevents stale attempts from overwriting a newer one.
- QR capture and non-camera fallback follow CROSSCUT-01; all content remains fictional game data.
