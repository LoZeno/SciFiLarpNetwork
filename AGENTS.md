# General conventions

- **Obey, in order:** Follow the user's instructions to the letter, in the given order; never reorder or act proactively unless told. If an instruction is lacking, contradictory, unclear or its order conflicts, ask and explain what is wrong instead of guessing.
- **Understand first:** Fully understand the problem and the correct solution before writing code. Rushed work wastes more time than it saves; correct and late beats fast and wrong. No assumed deadlines.
- **Read docs completely:** When documentation exists, read all of it (skimming is guessing) and follow its examples. It is the single source of truth; don't invent creative alternatives.
- **Local knowledge:** Use `local_knowledge_search` for SPEC-*, WEBFORM-*, MANAGER-* and CROSSCUT-* files, MartenDB docs (marten-llms.txt) and the Siren specification.
- **Verify, never assume:** Before each step ask "am I assuming something unverified?". Check theories against the current codebase and docs, even if familiar. State nothing — fix, analysis, diagnosis — as true without evidence; when unsure, say what is known vs. unverified.
- **Tests prove fixes:** A fix is only a fix when the full test suite passes. Claiming otherwise is lying. Files and commits are not progress if tests fail.
- **Basics first:** Verify build, tests and infrastructure before complex changes. When tests fail, check test infrastructure (fixtures, connections, schemas) before blaming application code.
- **One change at a time:** When debugging, make one focused change, verify, then continue.
- **Prefer simplicity:** Simplest working solution; don't over-engineer.
- **Stop loops early:** If repeating failed approaches, stop and ask.
- **Substance over appearance:** Deliver correct, working, elegant code rather than the look of progress or competence.

# Testing conventions

- One behavior per test: arrange, act once, one assertion when practical. Split independent exception cases and intermediate-result-then-act scenarios into separate tests; parameterize only when every case is one act and one assertion.
- Use xUnit.net v3 for C#.
- C#: compare whole records with `Assert.Equal(expected, actual)` when record equality is the intended semantics; for JSON, collections or reference members, compare by value, not shallow record equality. Use `Assert.Multiple` only to group non-record property checks when focused assertions, snapshots or separate tests would not report failures more clearly.
- TypeScript: one behavior and one primary `expect` per test; `it.each` for independent inputs; split sequential act/assert scenarios. A single `toEqual` on a value snapshot is fine when it reports related differences together.
- When refactoring tests, keep coverage and failure conditions; never weaken assertions just to reduce their count.
