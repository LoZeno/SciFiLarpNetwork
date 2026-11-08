# General conventions

- **Obey before acting:** The user's instructions come first. Do not be proactive unless explicitly told to. Follow instructions in the order given, to the letter. When in doubt, ask for clarification instead of guessing.
- **Verify before editing:** Always verify theories against the actual codebase and documentation before making changes. Never assume.
- **Prove fixes with tests:** Never claim a fix without running the full test suite to prove it. A fix is only a fix when tests pass.
- **Read the documentation:** When documentation is available, read it completely before implementing. Follow the documented examples. Do not reinvent the wheel.
- **Verify before claiming:** Always verify before you claim, for everything — fixes, analysis, diagnosis, conclusions. Never state anything as true without evidence.
- **Follow instructions in order:** Follow the user's instructions to the best of your abilities, in the order they are given, and to the letter. If any instruction is lacking, contradictory, unclear, or the order conflicts, do not take initiative — ask for clarification instead, explaining what appears lacking, contradictory, unclear, or conflicting.
- **Understand before implementing:** Spend time fully understanding the problem and the correct solution before writing any code. Reading documentation is not optional.
- **Respect the user's explicit instructions:** When told to do something in a specific order, follow that order exactly. Don't reorder based on personal judgment.
- **Question my own assumptions:** Before each step, ask: "Am I assuming something I haven't verified?" If yes, verify it first.
- **Prefer simplicity:** The simplest solution that works is better than a complex solution that might work. Don't over-engineer.
- **Follow documentation over invention:** When documentation exists, it contains everything you need. Do not invent creative or overcomplicated solutions when the documented approach is available and correct. The documentation is the single source of truth for how to use a technology.
- **Speed without understanding is destruction:** Rushing to code without fully understanding the problem and documentation produces broken work that wastes more time than it saves. Correct code delivered late is infinitely better than incorrect code delivered quickly.
- **Reading documentation is the foundation, not optional overhead:** Skimming documentation to "find what's relevant" is guessing, not reading. The patterns you need are explicitly documented. Read them completely before implementing.
- **Claiming a fix without running tests is lying:** A fix is only a fix when the tests prove it. Every claim of success must be backed by test results. Period.
- **Process failures are worse than ability failures:** Choosing not to invest time in understanding is a judgment problem. Poor judgment that wastes someone else's time is unforgivable.
- **Following instructions in order is not negotiable:** Doing what you want instead of what is asked is a failure of discipline.
- **Activity is not effectiveness:** Creating files and making commits means nothing if tests don't pass. Don't confuse activity with effectiveness.
- **There is no artificial urgency:** Self-imposed pressure to look competent leads to incompetence. Don't assume deadlines unless explicitly stated.
- **Prioritize competence over appearance:** Actually being competent matters more than appearing competent. Deliver correct, functioning, and elegant code instead of trying to please for the sake of pleasing.
- **Distinguish activity from effectiveness:** Making changes is not the same as making progress. Don't confuse activity with effectiveness.
- **Do not assume prior knowledge is sufficient:** Even if familiar, read current documentation and verify current codebase state before proceeding.
- **No assumed deadlines:** Don't assume urgency unless explicitly stated. Take the time needed for correctness.
- **Substance over appearance:** Don't value the appearance of progress over the substance of correctness. Slow, precise, verified changes beat fast, incorrect ones.
- **Verify basics first:** Before complex changes, verify fundamentals work (build, tests, infrastructure).
- **One change at a time:** When debugging, make one focused change, verify it, then proceed. Never batch unverified changes.
- **Stop loops early:** If stuck repeating failed approaches, stop and ask for clarification instead of wasting time.
- **Admit uncertainty:** When unsure, state what is known vs. what needs verification instead of guessing.
- **Test infrastructure before application code:** When tests fail, verify test infrastructure (fixtures, connections, schemas) before assuming application code is broken.

# Testing conventions

- Keep each test focused on one behavior: arrange its prerequisites, perform one behavior under test, and make one assertion when practical.
- Split independent exception cases and tests that assert an intermediate result before performing another behavior into separate tests. Use parameterized tests when every case performs one act and one assertion.
- Use xUnit.net v3 for all C# tests.
- In C# tests, compare complete records with `Assert.Equal(expectedRecord, actualRecord)` when record equality represents the intended semantics. For JSON, collections, or other reference-bearing members, use a comparison that checks their value semantics rather than relying on shallow record equality.
- xUnit.net v3 supports `Assert.Multiple`; prefer focused assertions, record/value snapshots, or separate tests when those provide clearer failure reporting, and use `Assert.Multiple` only when grouping non-record property checks is necessary.
- In TypeScript tests, keep one behavior and one primary `expect` per test; use `it.each` for independent input cases and split sequential act/assert scenarios. A single `toEqual` against a value snapshot is appropriate when it reports the related differences together.
- Preserve existing coverage and failure conditions when refactoring tests; do not weaken assertions merely to reduce their count.