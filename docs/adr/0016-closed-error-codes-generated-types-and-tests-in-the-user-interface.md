# 0016. Closed error codes, generated types and tests in the user interface

- Status: Accepted
- Date: 2026-11-13

## Context

[ADR 0014](0014-api-conventions-and-a-contract-kept-as-a-file.md) gave every error a code and generated the
client. Open: which codes exist, how the user interface uses the generated types and is tested, when a known
vulnerability fails a build.

## Decision

- **Error codes.** A closed list of the contract: the OpenAPI document lists them, the generated client
  carries them, and the type check fails for a code without a text in the user interface. A host use case
  reports only a listed code; a status with no code of its own gets one common code. Codes are part of an API
  contract ([Azure guidelines](https://github.com/microsoft/api-guidelines/blob/vNext/azure/Guidelines.md)),
  and a new one visible to existing clients is a breaking change
  ([Graph guidelines](https://github.com/microsoft/api-guidelines/blob/vNext/graph/articles/errorResponses.md)):
  host and user interface ship together, so the list can be closed. At run time the user interface keeps a
  default for a value outside the list ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457.html#section-3.2)).
- **Generated types.** The user interface uses the generated type of every value that travels, with the
  host's names, and converts nothing between names and numbers: first the three option enums, the others
  with their features. Orval writes an enum as a
  [const object with a type](https://github.com/orval-labs/orval/blob/v8.47.0/docs/content/docs/reference/configuration/output.mdx),
  as the [TypeScript handbook](https://www.typescriptlang.org/docs/handbook/enums.html#objects-vs-enums)
  suggests. An enum that does not travel stays, and so does a number in `customOptions`, which 1.0.x saved.
- **Tests.** Unit tests of the modules that read the contract, with [Vitest](https://vitest.dev), run by
  continuous integration.
- **Vulnerabilities.** Continuous integration fails on an advisory in a package that ships
  ([`npm audit`](https://docs.npmjs.com/cli/v11/commands/npm-audit) without development dependencies) and
  reports the others.

## Rejected alternatives

- Free-text codes and a hand-kept table in the user interface: a user finds the code without a text.
- Codes declared for each route: heavy for what it gives.
- A conversion at the boundary, for every enum and direction.
- No tests, as today; browser tests now: backlog H-07.
- Tests run by Node.js alone: its
  [type stripping](https://nodejs.org/api/typescript.html#typescript-features) refuses `enum` and
  namespaces, which this code uses.
- Failing on every advisory, also of a build tool. The npm documentation does not say whether a build
  should fail.

## Consequences

- A new code changes host, document and user interface texts in one commit.
- Until a use case reports an error, the list holds only the codes of the API itself.
