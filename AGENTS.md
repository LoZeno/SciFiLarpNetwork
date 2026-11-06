# Testing conventions

- Keep each test focused on one behavior: arrange its prerequisites, perform one behavior under test, and make one assertion when practical.
- Split independent exception cases and tests that assert an intermediate result before performing another behavior into separate tests. Use parameterized tests when every case performs one act and one assertion.
- Use xUnit.net v3 for all C# tests.
- In C# tests, compare complete records with `Assert.Equal(expectedRecord, actualRecord)` when record equality represents the intended semantics. For JSON, collections, or other reference-bearing members, use a comparison that checks their value semantics rather than relying on shallow record equality.
- xUnit.net v3 supports `Assert.Multiple`; prefer focused assertions, record/value snapshots, or separate tests when those provide clearer failure reporting, and use `Assert.Multiple` only when grouping non-record property checks is necessary.
- In TypeScript tests, keep one behavior and one primary `expect` per test; use `it.each` for independent input cases and split sequential act/assert scenarios. A single `toEqual` against a value snapshot is appropriate when it reports the related differences together.
- Preserve existing coverage and failure conditions when refactoring tests; do not weaken assertions merely to reduce their count.