# Continuous integration

One GitHub Actions workflow, `.github/workflows/ci.yml`, runs on every push to `main`, to `develop-vnext` and
to a work branch, on the pull requests to `main` and on request
([ADR 0010](../decisions/0010-continuous-integration-in-one-workflow.md)).

| Job | Runs on | What fails it |
| --- | --- | --- |
| `build-and-test` | Windows | A lock file out of date; a warning of the compiler, of an analyzer or of MSBuild; a failed test |
| `build-and-test-linux` | Linux | The same, on the projects of `Bravo.Portable.slnf`: the ones that do not target Windows |
| `build-web` | Linux | `npm ci`; a failed unit test of the user interface; the release build of the user interface, which generates the client of the host API and checks the types of the sources and of the unit tests before it bundles: a call that no longer fits the OpenAPI document fails it, and so does a type error in a test |
| `test-window` | Windows | The development bundle, the build of `Bravo.Desktop`, a failed test of the window, a policy of the webview that the tests left on the runner: it keeps the report of a failed run for 3 days |
| `build-installer` | Windows | The release bundle; the restore of the installer in locked mode; a publish of the host; a warning of WiX; a rule of Windows Installer that a package breaks: it keeps the packages for 3 days |
| `test-installer` | Windows | A real installation, upgrade or removal of a package that ends with another exit code, or leaves something else, than the [installer](installer.md) says: it keeps the logs of a failed run for 3 days |
| `audit` | Windows | A NuGet package with a known vulnerability of any severity, referenced directly or not, of the solution or of the installer; vulnerability data that cannot be read; an npm package that ships with the user interface and has a known advisory |

`build-and-test` is the status check that `main` requires: its name does not change. `audit` runs
`npm audit` twice ([ADR 0016](../decisions/0016-closed-error-codes-generated-types-and-tests-in-the-user-interface.md)):
without the development dependencies, where an advisory of any severity fails the job, and on every package,
where the advisories of the build tools are reported and fail nothing. `test-window` runs the tests of the
window ([tests of the user interface](user-interface-tests.md)) in a job of its own, so that the required
check stays as it is; the traces in the report of a failed run hold the token of a host whose process has
ended, and the report is kept for 3 days only. Its last step runs whatever the tests did, and shows that no
policy of the webview is left on the runner. The token of a run can only read the repository, the checkout
keeps no credentials, every action is pinned to a full commit SHA, and neither the .NET CLI nor the test
platform sends usage data.

## In the code

`.github/workflows/ci.yml` runs on every push to `main`, to `develop-vnext` and to a work branch (`vnext/**`),
on the pull requests to `main` and on request
([ADR 0010](../decisions/0010-continuous-integration-in-one-workflow.md)). A new push cancels the run in progress
of the same work branch or pull request; on `main` and on `develop-vnext` every pushed commit is built. A job
stops after 15 minutes.

| Job | What it runs |
| --- | --- |
| `build-and-test` | On Windows: the restore in locked mode, the build, the tests |
| `build-and-test-linux` | The same on Linux, on `Bravo.Portable.slnf`: the projects that do not target Windows |
| `build-web` | In `web/`: `npm ci`, then `npm test` and `npm run build:Release`, each of which generates the client of the host API first; the build checks the types of the sources and of the unit tests before webpack runs |
| `test-window` | On Windows: `npm ci` and `npm run build:UI` in `web/`, the restore in locked mode and the build of `Bravo.Desktop`, then `npm run test:window`; when a test fails, its report; at the end, whatever happened, a step that fails if a policy of the webview is left |
| `build-installer` | On Windows: `npm ci` and `npm run build:Release` in `web/`, the restore of the installer in locked mode, the four packages; the packages are kept as the artifact `installer-packages` |
| `test-installer` | On Windows, after `build-installer`: its packages and the ones of 1.0.14 from the public release, then `installer/tests/Test-Packages.ps1`; when a step fails, the logs of Windows Installer as the artifact `installer-logs` |
| `audit` | A restore of the solution and one of the installer that fail on a NuGet package with a known vulnerability, then `npm audit` on the packages that ship with the user interface, which fails on an advisory, and on all of them, which reports |

What a run checks beyond the commands of the table above:

- A job that builds checks out the whole history of git (`fetch-depth: 0`): the version of a build counts
  the commits, and the build fails on a shallow clone ([Release](release.md)). `build-web`, `test-installer`
  and `audit` build nothing of .NET, and check out the one commit.
- The build has `-warnaserror`. A warning in a project fails a local build as well; the switch adds the
  warnings raised outside the projects, at the level of the solution.
- `audit` restores with `-p:AuditPipeline=true`, which turns the NuGet audit warnings into errors: a package
  with a known vulnerability, and also vulnerability data that cannot be read. On a local machine the
  restore needs `--force`: without it NuGet skips a restore that is up to date, and the audit with it.
- `npm audit --omit=dev` looks at the dependencies that ship with the user interface, and an advisory of any
  severity fails the job. A second step runs `npm audit` on every package, the build tools included: it
  reports and does not fail the job, so its error annotation on a green run is expected.
- The tests of `build-and-test` and of `test-window` start `Bravo.exe` and use the real WebView2 engine. The
  runtime is not in the list of the software of the runner image: it comes with the Windows image that the
  runner is built on.
- The runner runs every process elevated, and an elevated webview ignores the environment variables of the
  WebView2 runtime: `test-window` gives the webview its two values as policies of the machine, which its
  last step shows to be gone ([User interface](../../web/AGENTS.md)).
- When a test of the window fails, `test-window` keeps the folder `artifacts/window-tests` as the artifact
  `window-tests-report` for 3 days: the HTML report, with the trace and a screenshot of each failed test. A
  trace holds the token of the host of its test, whose process has ended.

- `build-installer` compiles `Bravo.Setup.exe` ahead of time with the C++ build tools of the runner image,
  and its build validates the packages against the rules of Windows Installer. The artifact
  `installer-packages` is kept for 3 days: the packages of a run, to look at their dialogs on a machine that
  can be thrown away. They are not signed: the workflow of a release signs
  ([Release](release.md)).
- The packages have the version of the build, which `build-installer` reads and tells `test-installer`: the
  semantic version, which on a work branch ends with the commit, and the file version. No workflow names a
  version.
- `test-installer` installs for real, on the runner alone: every run of Windows Installer is silent, and its
  log is in `artifacts/installer-logs`, kept for 3 days as `installer-logs` when a step failed. The test
  prints each check, and what it observes of 1.0.14: its removal, and a package of the other context. The
  packages that use the installed runtime run on the .NET 10 runtimes of the runner image: the test stops
  at its start, and names the runtime, when the image has none.

`build-and-test` is the status check that `main` requires: do not rename the job and do not give it a matrix.
Every action is pinned to the commit of a release, with the version in a comment on the same line.

A release is not made here: `.github/workflows/release.yml` is a workflow of its own, started by hand, which
does not wait for this one ([Release](release.md)).
