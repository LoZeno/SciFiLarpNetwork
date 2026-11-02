# TypeScript conventions

The frontend toolchain is configured in `Frontend/package.json`. Run its checks from that directory:

```powershell
npm run check
```

The check runs Prettier verification, ESLint, strict TypeScript compilation, and the Vitest test suite. Individual scripts are available for formatting (`npm run format`), linting, type checking, and tests. The project keeps generated dependencies and output out of source control while committing its dependency lockfile.

TypeScript compiler settings in `tsconfig.json` enable strict checking, including unchecked-index and exact-optional-property checks. ESLint rejects explicit `any`; narrowly justified boundary assertions must remain localized and documented. Use explicit API/domain types and keep loading/error/client state clear, without duplicating authoritative server-side rules.

Use focused React components and hooks with one clear responsibility. Keep UI presentation and client-side validation distinct from server authorization and domain decisions. Follow the product glossary in [domain language and scope](domain-language-and-scope.md): Player, Character, and Account remain distinct concepts, while Network, Group, and Skill are generic configurable concepts rather than franchise assumptions.

The frontend test suite uses Vitest with a jsdom environment and React Testing Library for UI contracts. `npm run check` enforces the SPEC-10 80% statement and line coverage thresholds across executable TypeScript, excluding only declaration files. Feature-level UI tests are added with their corresponding roadmap tasks.
