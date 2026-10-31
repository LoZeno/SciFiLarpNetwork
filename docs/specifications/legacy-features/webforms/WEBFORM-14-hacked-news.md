# Web feature: News on a breached network

**Legacy route:** `Hacking\HackedNews.aspx`.

## Observed behavior

Displays active stories for the breached account's network, allows previous/next navigation, and offers an edit action protected by the shared hacking challenge. Successful challenge opens an editor populated with the selected story; submit updates title/body while preserving author, network, dates, and difficulty. A reload action returns to the first story.

## Acceptance criteria

- News is filtered to the breached network and configured active-time rules; navigation handles no stories and first/last boundaries.
- A story can be edited only after a successful, authorized category challenge; the target network/story is revalidated at submit time.
- Save validates title/body, preserves immutable metadata unless separately authorized, and appends an auditable edit event with before/after values.
- Challenge failure returns to the story view without applying changes; reload returns to the defined first result.
- Story content is rendered as safe text/rich content and fake code/animation never executes user input.
