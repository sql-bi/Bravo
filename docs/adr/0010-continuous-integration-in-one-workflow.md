# 0010. Continuous integration is one GitHub Actions workflow, sized to what exists

- Status: Accepted
- Date: 2026-11-11

## Context

`develop-vnext` has no workflow: the local build is the only gate, and nothing has been built on Linux. On
`main` the workflow ran on pull requests only; its job `build-and-test` is the required status check. The
branch holds one portable test project and the user interface.

## Decision

One workflow, `.github/workflows/ci.yml`, runs on every push to `main`, `develop-vnext` and a work
branch, on pull requests to `main` and on request. A new push cancels the run in progress of a work branch or
pull request. Four jobs:

- `build-and-test`, on Windows: restore in locked mode, build with `-warnaserror`, test. Its name is the one
  `main` requires, so the final pull request changes no repository rule.
- `build-and-test-linux`: the same on Linux, on the whole solution until a Windows-only project exists.
- `build-web`: `npm ci` and the release bundle of the user interface, which checks the types.
- `audit`: a restore that turns the NuGet audit warnings into errors, in this job only, and `npm audit` of the
  shipped dependencies, which only reports until they can be updated.

The token is read-only, the checkout keeps no credentials, every action is pinned to a full commit SHA, and
the .NET CLI and the test platform send no telemetry.

References: GitHub Docs
([events](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows),
[concurrency](https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/control-workflow-concurrency),
[secure use](https://docs.github.com/en/actions/reference/security/secure-use)); Microsoft Learn
([NuGet audit in CI](https://learn.microsoft.com/nuget/concepts/auditing-packages#running-nuget-audit-in-ci),
[test platform telemetry](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-telemetry),
[`-warnaserror`](https://learn.microsoft.com/visualstudio/msbuild/msbuild-command-line-reference)); the
workflows of `icsharpcode/ILSpy`, `dotnet/eShop` and `files-community/Files`.

## Rejected alternatives

- One Windows job only: nothing would be built on Linux, and a known vulnerability would stay a warning that
  nobody reads.
- The extended set, with coverage, test-report packages, CodeQL and dependency review: part of it cannot run
  today, because it acts on the default branch or needs pull requests; the rest has nothing to measure before
  product tests exist.

## Consequences

- An advisory published later turns `audit` red at the next push, not the build of an unchanged commit
  ([ADR 0009](0009-rules-enforced-by-the-build.md)).
- Caches are shared only through the default branch: until the final merge each work branch starts without
  one.
- Until the final merge the workflow cannot be started by hand, no scheduled run looks for new advisories, and
  the pinned actions are updated by hand.
- Backlog H-01 adds the solution filter of the Linux job; H-04 makes `npm audit` blocking.
