# Release

A release is made by a workflow of GitHub Actions that the maintainer starts by hand, and the version of a
build is computed by Nerdbank.GitVersioning
([ADR 0021](../decisions/0021-releases-from-a-workflow-started-by-hand-versioned-by-nerdbank-gitversioning.md)).
The steps of a release are in [the guide](../guides/make-a-release.md).

**Version.** `version.json`, at the root, holds the one value that is written by hand: `1.1.0-beta.1` for a
preview of 1.1.0, `1.1.0` for the stable release. Every build computes its versions from that value and from
the history of git.

| Value | Example | What reads it |
| --- | --- | --- |
| File version | `1.1.0.185` | Windows Installer, as the version of the product; the update feed |
| Semantic version | `1.1.0-beta.1`, and `1.1.0` for a stable release | The title of the window and `GET /application`; the names of the files of a release and its tag |
| Assembly version | `1.1.0.0` | The identity of an assembly |
| Informational version | `1.1.0.185-beta.1+4497d5d308` | Who looks at the properties of a file |

- The fourth number of the file version is the height: the commits since the three numbers of
  `version.json` last changed. A new label of a preview, and the removal of the label, do not start it again:
  the file version orders a preview and its release, `1.1.0.187` and `1.1.0.188`.
- The semantic version ends with the commit, `1.1.0-beta.1.g4497d5d308`, unless the build is a public
  release: one on `main`, the branch that `version.json` names, or one that is told so with
  `-p:PublicRelease=true`, as the build of a release is.
- A build needs the whole history of git, and fails on a shallow clone.
- Windows Installer compares three numbers: a preview and its release are one version to it, and a package
  replaces an installed one of its own version ([Installer](installer.md)).

**Files.** A release has six files, with the names of 1.0.x: an installed 1.0.x asks for its update by a
name that it builds itself, from how it was published and installed.

| File | What it is |
| --- | --- |
| `Bravo.<version>.x64.msi` | The package per machine that carries the .NET runtime |
| `Bravo.<version>.x64-userinstaller.msi` | The package per user that carries the runtime |
| `Bravo.<version>.x64-portable.zip` | The folder of the application with the runtime, as an archive |
| `Bravo.<version>.x64-frameworkdependent.msi` | The package per machine that uses the installed runtime |
| `Bravo.<version>.x64-frameworkdependent-userinstaller.msi` | The package per user that uses the installed runtime |
| `Bravo.<version>.x64-frameworkdependent-portable.zip` | The folder of the application without the runtime, as an archive |

- `<version>` is the semantic version. The tag of a release is `v<version>` and its title `Bravo v<version>`,
  as for 1.0.x.
- An archive is the folder that a package installs, and nothing else: it registers no external tool.

**Workflow.** `release.yml` runs on the commit that it is started on, and does not look at continuous
integration: that the commit is good is the maintainer's check.

| Job | What it does |
| --- | --- |
| `build-web` | Builds the release bundle of the user interface |
| `build` | Builds and signs the six files, checks the signatures, writes the list of downloads and the update feed |
| `test` | Installs, upgrades and removes the signed packages on a runner, as continuous integration does with the ones it builds |
| `publish` | Creates a draft release with the six files; for a stable release, keeps the update feed for the maintainer |

- The release is a draft: nothing is public until the maintainer publishes it, and the tag is created then,
  on the commit of the run. A draft is seen by who can write in the repository.
- A version that has its tag is released: the run stops before it builds. A release of the version that
  exists already, a draft as well, is never replaced: the run stops, and the maintainer deletes the draft.
- A version with a label in `version.json` is a preview: its release is marked as a pre-release, which
  GitHub never shows as the latest one, and it has no update feed.
- The job that signs runs no package of npm: the bundle comes from `build-web`.
- Only `build` can ask for the identity that signs, and only `publish` can write in the repository.

**Signature.** The signature is the one of 1.0.x: the certificate of the publisher, which Azure Key Vault
holds, used through AzureSignTool.

- The identity that signs is federated
  ([OpenID Connect](https://docs.github.com/en/actions/concepts/security/openid-connect)): Azure trusts a
  token that GitHub gives to a run of this repository on one branch, and the repository holds no secret.
  Four variables of the repository name the identity, the vault and the certificate. No environment and no
  reviewer stand between a run and the identity: a workflow pushed to that branch can sign.
- Signed are `Bravo.exe` and the four assemblies of the host in each of the two folders, `Bravo.Setup.exe`,
  and each package. The files are signed before a package or an archive holds them; a package, after it is
  built and validated. An archive is not signed: its files are.
- A signature has a timestamp
  ([RFC 3161](https://learn.microsoft.com/windows/win32/seccrypto/time-stamping-authenticode-signatures)), so
  that it stays valid after the certificate has expired.
- The run checks every signature before it keeps a file: one certificate for all, a chain that Windows
  trusts, a timestamp. The test then checks the signature of each package and of the five installed files.

**Release notes.** The notes are the ones that GitHub generates from the pull requests merged since the
release before, under the headings of `.github/release.yml`, which sorts them by label. Above them the run
writes the list of the downloads. The maintainer corrects the notes in the draft: the ones of 1.1.0, whose
commits arrive in one pull request, are written there.

**Update feed.** An installed 1.0.x learns of a new version from a file, `currentversion.json`, at two
addresses that every installed copy has in itself: the containers `bravo-public` and `bravo-internal` of the
storage account `bravorelease`. For a stable release the run writes that file, and the maintainer uploads
it to both, after the draft is published and its files can be downloaded.

| Field | Value | What 1.0.x does with it |
| --- | --- | --- |
| `version` | The file version, `1.1.0.188` | Reads it as a [`System.Version`](https://learn.microsoft.com/dotnet/api/system.version.parse) and compares it with its own file version |
| `download` | The address of the package per machine that carries the runtime | Asks for the file of its own kind: it adds `-frameworkdependent`, then `-userinstaller` or `-portable` with `.zip`, to the name |
| `changelog` | The address of the releases of the repository | Shows it as a link |
| `channel` | `Stable` | Nothing: the file of today has it |

- The run reads the file back as 1.0.x reads it, and fails when 1.0.x would not be told, or would ask for
  a file that the release does not have.
- The file is UTF-8 with no byte order mark.
- A preview never reaches the feed: 1.0.x cannot read a version with a label.

**A run that publishes nothing.** The same jobs prove the workflow without a release. Such a run starts by
itself on a push to a work branch that changes what a release is made of, and by hand with the input
`dry-run`.

| | A release | A run that publishes nothing |
| --- | --- | --- |
| Version in the names | `1.1.0-beta.1` | `1.1.0-beta.1.g4497d5d308` |
| Certificate | The one of the publisher | One made on the runner, which lives one day |
| Timestamp | Yes | No |
| Draft release | Created | Not created: `publish` does not run |

It proves the build of the six files, the order of the signatures, the installation of signed packages, the
list of downloads and the update feed. It does not prove the sign-in to Azure, Key Vault and AzureSignTool,
the trust of the signature, or the creation of the draft.

## In the code

| Part | Path |
| --- | --- |
| The version: the one value, and the package that every project gets | `version.json`, `Directory.Packages.props` |
| The workflow | `.github/workflows/release.yml` |
| The names of the six files, the list of downloads, the update feed and its check, the check of a signature | `release/Release.psm1` |
| The tests of those functions | `release/tests/Test-Release.ps1` |
| The version of the build, read without building | `installer/Get-BuildVersion.ps1` |
| What signs a file, with which certificate | `installer/Sign-Files.ps1` |
| AzureSignTool, in one version | `.config/dotnet-tools.json` |
| The six files: the targets `BuildRelease`, `BuildPackages` and `BuildArchives` | `installer/Bravo.Installer.wixproj`, `installer/Stage.targets` ([Installer](installer.md)) |
| The headings of the generated notes | `.github/release.yml` |

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
- The names of the six files are written twice: in `Bravo.Installer.wixproj`, which builds them, and in
  `Get-ReleaseFileName`, which says what 1.0.x asks for. The run compares the two, and
  `Test-Release.ps1` holds the names of the files of 1.0.14.
- `Confirm-UpdateFeed` is what an installed 1.0.x does with the feed, step by step. A change of the feed is
  first a change of that function and of its tests.
- The button of GitHub that starts a workflow needs `release.yml` on the default branch. Until the final
  merge the workflow is started with `gh workflow run release.yml --ref <branch>`, which GitHub allows after
  the workflow has run once, as it does on a push to a work branch.
- A step of `release.yml` that publishes, signs with the certificate of the publisher or writes in the
  repository runs only when `DRY_RUN` is `false`, which only a run started by hand can be. No job names an
  environment: a run that names one that does not exist creates it.
- Every action is pinned to the commit of a release, with the version in a comment on the same line, as in
  `ci.yml` ([Continuous integration](continuous-integration.md)).
