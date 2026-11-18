# 0007. The application is redesigned on a development branch, one component at a time

- Status: Accepted
- Date: 2026-11-11

## Context

The host grew by hand over several years. Its features have no tests, user interface and logic are fused, and
an incremental refactoring stalled on pull requests of 57 to 80 files that mixed old and new code. There is no
release deadline, and the maintainer wants a project that a coding agent can maintain under his supervision.

## Decision

- Version 1.1.0 redesigns the repository layout, the host, the API, the installer, CI/CD and the release
  process, with the features of 1.0 and no new one. The TypeScript user interface changes only where the new
  API requires it.
- The work happens on `develop-vnext`, created from `main`. The current code leaves the tree of that branch;
  `main` stays untouched as the executable reference of today's behavior.
- Commits are pushed to `develop-vnext` without pull requests. One final pull request brings the branch into
  `main` with a merge commit; after it, changes reach `main` through pull requests again.
- Each component follows one cycle: requirements from the current behavior; how well-engineered projects and
  the official documentation solve the problem; options with a recommendation; the maintainer's decision as an
  ADR; the implementation with its tests; a report of what was proven.
- Four guarantees hold for users of 1.0.x ([specification](../spec.md#guarantees-for-users-of-10x)). Everything
  else may change.

## Rejected alternatives

- Refactoring in place: without tests on the features, every step mixes old and new code.
- New code beside the old one on `main`: the code that must not be imitated stays next to the new one.
- Emptying `main` at once: the default branch would hold no working application for months.
- Pull requests on the development branch: nobody else works on it, and the review of each component's design
  and result gives control where it matters.

## Consequences

- ADRs 0001 to 0006 and `docs/design/` describe the code on `main`: they are requirements and prior art, not
  constraints. The ADR of a component supersedes the one it replaces.
- Dependency updates and the closing of issues act on the default branch, so they wait for the final merge.
- The scope is bounded by rule: features do not change, an exploration is time-boxed, one component is in
  progress at a time, and with equal requirements the simplest design wins.
- The Azure DevOps release pipeline stays until 1.1.0 ships: a 1.0.x fix is still built with it.
