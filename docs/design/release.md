# Release

The version of a build is computed by Nerdbank.GitVersioning
([ADR 0021](../decisions/0021-releases-from-a-workflow-started-by-hand-versioned-by-nerdbank-gitversioning.md)).

**Version.** `version.json`, at the root, holds the one value that is written by hand: `1.1.0-beta.1` for a
preview of 1.1.0, `1.1.0` for the stable release. Every build computes its versions from that value and from
the history of git.

| Value | Example | What reads it |
| --- | --- | --- |
| File version | `1.1.0.185` | Windows Installer, as the version of the product |
| Semantic version | `1.1.0-beta.1`, and `1.1.0` for a stable release | The title of the window and `GET /application` |
| Assembly version | `1.1.0.0` | The identity of an assembly |
| Informational version | `1.1.0.185-beta.1+4497d5d308` | Who looks at the properties of a file |

- The fourth number of the file version is the height: the commits since the three numbers of
  `version.json` last changed. A new label of a preview, and the removal of the label, do not start it again:
  the file version orders a preview and its release, `1.1.0.187` and `1.1.0.188`.
- The semantic version ends with the commit, `1.1.0-beta.1.g4497d5d308`, unless the build is a public
  release: one on `main`, the branch that `version.json` names, or one that is told so with
  `-p:PublicRelease=true`.
- A build needs the whole history of git, and fails on a shallow clone.
- Windows Installer compares three numbers: a preview and its release are one version to it, and a package
  replaces an installed one of its own version ([Installer](installer.md)).

## In the code

`Directory.Packages.props` adds the package `Nerdbank.GitVersioning` to every project, and `version.json`
configures it.

- No project sets `Version`, `FileVersion` or `InformationalVersion`, and a build is not given them: the
  package ignores a value that it did not compute.
- `version.json` is read from the commit, not from the working copy. An edit counts from the commit that
  holds it: until then a build has the committed version, with the height 0.
- `cloudBuild` is turned off in `version.json`: on a runner of GitHub Actions the package would otherwise
  write the version in the variables of the run at every build, and nothing reads them.
- `pathFilters` stays `:/`, the root: every commit raises the height. With a narrower filter, the commit that
  turns a preview into its release, which changes `version.json` alone, would have the file version of the
  commit before it.
- The version that a user reads is the semantic version. The package computes it in a target of the build,
  so `Bravo.Desktop.csproj` writes it in the metadata of the assembly, `BravoVersion`, after that target;
  `BuildIdentity` reads it once ([Startup](startup.md)). The informational version that the package writes
  has the height in it, and is not shown.
