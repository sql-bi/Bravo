# Versioning

Bravo has a single Semantic Version, declared in [version.json](../../version.json) and computed by Nerdbank.GitVersioning (NBGV). The application exposes generated values through [AppVersion](../../src/Infrastructure/AppVersion.cs).

## Source of truth

`version.json` at the repository root holds the only hand-edited value:

- `1.1.0` — a released version.
- `1.1.0-beta.1` — a preview of the upcoming `1.1.0`.

One release is one bump: minor for a feature, patch for a fix. The prerelease tag is chosen by hand and
incremented per preview, so that a preview never consumes a patch number of the stable line.

Do not set `Version`, `FileVersion` or `InformationalVersion` in [Bravo.csproj](../../src/Bravo.csproj): NBGV stamps them from
`version.json` at build time.

## Derived values

| Value | Shape and condition | Authoritative for |
| --- | --- | --- |
| `AssemblyFileVersion` | `X.Y.Z.{height}` | ordering two builds; MSI `ProductVersion` |
| `AssemblyInformationalVersion` | `X.Y.Z.{height}[-tag]+{commit}` | diagnostics |
| `AssemblyVersion` | `X.Y.0.0` | assembly identity (`assemblyVersion.precision: minor`) |
| `NBGV_SemVer2`, public build | `X.Y.Z[-tag]`, when the ref matches `publicReleaseRefSpec` | artifact names, `AppVersion.SemanticVersion`, WiX `-dVersion` |
| `NBGV_SemVer2`, internal build | `X.Y.Z-tag.g{commit}` with a prerelease tag; `X.Y.Z-g{commit}` without one | same consumers, with commit identity |

`{height}` is derived from Git history since the numeric `X.Y.Z` last changed. It orders builds on a release line; it is not a unique identity across branches or repeated builds of one commit. Use the commit identifier in `AssemblyInformationalVersion` to distinguish source revisions.

## Rules

- **The prerelease tag never reaches a numeric field.** `AssemblyFileVersion` stays numeric in every state, so
Windows Installer and `System.Version` keep working unchanged. WiX `-dVersion` carries the tag but feeds the
installer telemetry only: the MSI `ProductVersion` is bound to the file version of `Bravo.exe` in [Bravo.wxs](../../installer/wix/src/Bravo/Bravo.wxs) and [Bravo-perUser.wxs](../../installer/wix/src/Bravo/Bravo-perUser.wxs).

- **`SemVer2` is the version that the application, the installer and the artifacts report.** The
application reads it as `ThisAssembly.NuGetPackageVersion`, which equals `SemVer2` only while
`nuGetPackageVersion.semVer` is `2` in `version.json` and `NBGV_ThisAssemblyIncludesPackageVersion` is set in
`Bravo.csproj`. Without the first, the application reports the SemVer1 form `X.Y.Z-tag-0001-g{commit}`; without
the second, it does not compile. [AppVersionTests](../../test/Bravo.Tests/Infrastructure/AppVersionTests.cs) checks the generated shapes, including public and internal build differences.

- **The height resets only when the numeric `X.Y.Z` changes.** Adding, changing or removing the prerelease tag
does not reset it. `AssemblyFileVersion` is therefore monotonic across `1.1.0-beta.1 → 1.1.0-beta.2 → 1.1.0`,
which is what makes a preview and its final release — identical on `X.Y.Z` — orderable.

- **`pathFilters` must stay `:/`, the repository root.** Every commit must advance the height. A narrower filter
leaves the height unchanged for commits that touch only excluded paths, and the commit that promotes a preview
to its final release changes `version.json` alone: such a filter would give the two builds the same
`AssemblyFileVersion`.

- **`publicReleaseRefSpec` lists the branches that produce clean versions.** Outside them, `SemVer2` and
`NuGetPackageVersion` carry the commit id: `X.Y.Z-tag.g{commit}`, or `X.Y.Z-g{commit}` without a prerelease
tag; numeric fields and `AssemblyInformationalVersion` are unaffected. A tag checkout runs in
detached HEAD and matches no branch pattern, so building from a tag requires adding the tag pattern.

- **Artifact names come from `SemVer2`, not from `SimpleVersion`.** The [packaging pipeline](../../.azure/pipelines/build-bravo.yaml) uses `NBGV_SemVer2`. On public builds, `SimpleVersion` drops the prerelease tag: using it would give a preview and the release that follows it the same file names. Internal `SemVer2` values also carry the commit identifier.

- **Verify release versions from the intended committed ref.** The generated version depends on configuration, Git history and whether the ref is a public release. A local build with uncommitted version changes is not evidence of the version that the release pipeline will produce.

- **The height needs full history.** Shallow clones make NBGV fail or compute a wrong number, so both
[CI](../../.github/workflows/ci.yml) and [packaging](../../.azure/pipelines/build-bravo.yaml) check out with unlimited depth.
