# Web feature: Personal files

**Legacy route:** `CartellaPersonale\FilePersonali.aspx`.

This feature precedes WEBFORM-33 because messaging can copy messages into the authenticated character's personal files.

## Observed behavior

For the signed-in character, lists saved files, allows creating a title/body file, stores line breaks and an optional encryption level, and displays a selected file. The legacy decryption-success callback is unimplemented.

## Acceptance criteria

- A character can list, create, and read only their own personal files; file ownership is enforced server-side for every operation.
- Creation validates title/content, preserves formatted text safely, and records optional fictional encryption metadata without claiming cryptographic protection unless real encryption is implemented.
- File display handles empty/long content and renders user-authored text safely, without interpreting it as HTML or script.
- If encryption/decryption gameplay is retained, unauthorized users see a clear locked state; the successful-decryption behavior is fully specified and tested rather than a stub.
- Save, retry, and error states do not lose the user's draft or create duplicate files after repeated submission.
