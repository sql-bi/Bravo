# Versioning

Bravo has a single Semantic Version, declared in `version.json` and computed by Nerdbank.GitVersioning (NBGV).

## Source of truth

`version.json` at the repository root holds the only hand-edited value:

- `1.1.0` — a released version.
- `1.1.0-beta.1` — a preview of the upcoming `1.1.0`.

One release is one bump: minor for a feature, patch for a fix. The prerelease tag is chosen by hand and
incremented per preview, so that a preview never consumes a patch number of the stable line.

Do not set `Version`, `FileVersion` or `InformationalVersion` in `Bravo.csproj`: NBGV stamps them from
`version.json` at build time.

## Derived values

| Value | Shape | Authoritative for |
| --- | --- | --- |
| `AssemblyFileVersion` | `X.Y.Z.{height}` | ordering two builds; MSI `ProductVersion` |
| `AssemblyInformationalVersion` | `X.Y.Z.{height}[-tag]+{commit}` | diagnostics |
| `AssemblyVersion` | `X.Y.0.0` | assembly identity (`assemblyVersion.precision: minor`) |
| `NBGV_SimpleVersion` | `X.Y.Z` | WiX `-dVersion` |
| `NBGV_SemVer2` | `X.Y.Z[-tag]` | artifact names, git tag, `AppVersion.SemanticVersion` |

`{height}` is the number of commits since the numeric `X.Y.Z` last changed. It is a build counter: it makes
every build uniquely identifiable and orders builds that share the same `X.Y.Z`.

## Rules

- **The prerelease tag never reaches a numeric field.** `AssemblyFileVersion` and `NBGV_SimpleVersion` stay
numeric in every state, so Windows Installer and `System.Version` keep working unchanged.

- **`SemVer2` is the version that the application and the artifacts report.** The
application reads it as `ThisAssembly.NuGetPackageVersion`, which equals `SemVer2` only while
`nuGetPackageVersion.semVer` is `2` in `version.json` and `NBGV_ThisAssemblyIncludesPackageVersion` is set in
`Bravo.csproj`. Without the first, the application reports the SemVer1 form `X.Y.Z-tag-0001-g{commit}`; without
the second, it does not compile. A test in `AppVersionTests` checks the shape.

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

- **Artifact names come from `SemVer2`, not from `SimpleVersion`.** The two are identical for a release and
differ only for a preview, where `SimpleVersion` drops the tag: naming artifacts from it would give a preview
and the release that follows it the same file names.

- **NBGV reads `version.json` from `HEAD`, not from the working copy.** An edit takes effect from the commit
that contains it: a build or a test run on a dirty `version.json` still reflects the committed values, and only
the height resets.

- **The height needs full history.** Shallow clones make NBGV fail or compute a wrong number, so both
pipelines check out with unlimited depth.
