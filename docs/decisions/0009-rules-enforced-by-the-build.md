# 0009. The code rules are enforced by the build, with the .NET SDK analyzers and plain tests

- Status: Accepted
- Date: 2026-11-11

## Context

Coding agents write version 1.1.0 under the maintainer's review
([ADR 0007](0007-redesign-on-a-development-branch.md)). A rule that only a document states gets broken; a rule
that the build checks does not.

## Decision

Every rule that a tool can check fails the build or a test; none has an allow-list.

- `Directory.Build.props`: nullable reference types, warnings as errors, code style in the build,
  `AnalysisLevel` `latest-recommended`, no implicit usings, no transitive project references, lock files.
  NuGet audit warnings stay warnings: a later advisory must not break an unchanged commit.
- `Directory.Packages.props`: central package management with transitive pinning.
- `.editorconfig`: the style rules at `warning`, and three rules silent by default: naming (IDE1006),
  `sealed` on internal types (CA1852) and unused usings (IDE0005).
- Banned symbols, read by `Microsoft.CodeAnalysis.BannedApiAnalyzers`: the system clock (use `TimeProvider`)
  and hand-written Win32 declarations (use CsWin32).
- Architecture tests in plain reflection: allowed project references, no catch-all names, no static mutable
  state, each rule tested on a fixture that breaks it.
- `global.json` pins the SDK feature band and selects Microsoft.Testing.Platform for `dotnet test`, where the
  tests run on xUnit v3 4.x; `web/package.json` pins the Node.js line.

References: Microsoft Learn
([code analysis](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview),
[naming rules](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/naming-rules),
[CA1852](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1852),
[auditing packages](https://learn.microsoft.com/nuget/concepts/auditing-packages),
[central package management](https://learn.microsoft.com/nuget/consume-packages/central-package-management),
[testing with `dotnet test`](https://learn.microsoft.com/dotnet/core/testing/unit-testing-with-dotnet-test));
the [xUnit.net v3 4.0.0 release notes](https://xunit.net/releases/v3/4.0.0); the banned-symbol lists of
`dotnet/roslyn` and `microsoft/testfx`; the reflection tests of `dotnet/efcore`.

## Rejected alternatives

- The same rules with their defaults: naming, `sealed`, the clock and interop would stay with review.
- Maximum strictness, with Meziantou.Analyzer, Roslynator, SonarAnalyzer and ArchUnitNET: overlapping and
  conflicting rules, a slower build, and for SonarAnalyzer a licence that is not open source.
  NetArchTest.Rules has had no release since 1.3.2 (2021); ArchUnitNET reads IL, and until
  [issue 498](https://github.com/TNG/ArchUnitNET/issues/498), closed on 2026-10-09, its negative rules passed
  in Release builds.
- For the tests: VSTest with `xunit.v3.mtp-off`, the older runner, which the .NET 10 SDK no longer offers to
  Microsoft.Testing.Platform projects; xUnit v3 3.x, built on the platform version that xUnit 4.0 dropped.

## Consequences

- A new SDK feature band brings new analyzer rules, so it arrives by a commit that edits `global.json`.
- A rule that no analyzer covers needs a test, or stays with review.
- Test filters and coverage use the options of Microsoft.Testing.Platform, not those of VSTest.
