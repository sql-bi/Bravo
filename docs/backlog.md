# Bravo: backlog

Next ID: B-31

The open work toward 1.1.0, in the order to take it ([ADR 0007](decisions/0007-redesign-on-a-development-branch.md)).
Status: `[ ]` to do, `[~]` in progress, `[-]` dropped (say why); a done item leaves the backlog.

Every entry has a code, `B-` and a number from one counter. A new entry takes the `Next ID`, which then moves
on by one; a code never changes and is never reused. An entry that had a code of the earlier series, one per
area, keeps it in brackets until it leaves. The other codes of that series, such as `H-08` or `D-01`, name
finished work, which the history of git holds.

An item is a component. Its line gives the starting hypothesis and what to explore before the design is
proposed; neither is a decision. One component is in progress at a time. A stage is closed when its exit
criterion can be checked on `develop-vnext`.

## Now

As of 2026-11-18: the redesign is at stage T3 Distribution. B-1, left in T2, waits for the release of
TypeScript 7.1 and ts-loader 10; nothing blocks the next item.

## Next

### T2 Skeleton

Exit: the new executable starts, a second start activates the first, it shows the current user interface and
opens the options through the new API and the generated client.

- [ ] B-1 (was H-09) TypeScript 7, the native compiler. Not before TypeScript 7.1 and ts-loader 10 are released:
  7.0 has no API, 7.1 has a new one, and ts-loader 10 requires it. Hypothesis: the check of the types moves to
  the native `tsc`, and ts-loader 10 translates.

### T3 Distribution

Exit: GitHub Actions produces a signed, installable preview with release notes; installed over 1.0.14 it keeps
the four guarantees of [the specification](spec.md#guarantees-for-users-of-10x).

- [ ] B-2 (was D-02) Release. Hypothesis: GitHub Actions builds, signs, publishes a GitHub release with
  generated notes and changelog, and updates the feed. Explore: signing with a federated identity; changelog
  tools; how the version is computed. Also: the title of the window and `GET /application` show the
  informational version of the assembly, which only the build of the installer sets, to the version that it is
  given. Also, from D-01:
  - The build of the installer makes the four packages, unsigned, in `artifacts/installer/en-US`, with the
    file names of 1.0.x. It takes the version as `-p:BravoVersion`, which is 1.1.0 when none is given: the
    release computes it and passes it. Windows Installer compares the first three numbers of a version.
  - The signature is the one of 1.0.x, the certificate of the publisher through AzureSignTool: on
    `Bravo.exe` and the assemblies of the host in the folder that a package is made from,
    `artifacts/installer-stage/<version>/<runtime>`, and on `Bravo.Setup.exe` in the folder of its publish
    beside it, before the package is built, and then on each package. The build has no step between the two
    yet.
  - The installed 1.0.x asks for a file whose name it builds itself, adding `-frameworkdependent`,
    `-userinstaller` or `-portable` (a `.zip`) to the name in the feed, so those files exist. The portable
    one is the folder that a package is made from.
  - The publishes of that build restore the packages of `win-x64` and read and write no lock file, so that
    the committed ones do not change: the release decides whether that graph is locked.
  - The packages of a run of continuous integration are its artifact `installer-packages`, kept for 3 days.
- [ ] B-3 (was D-03) Updates. Hypothesis: a notification, as today. Explore: automatic update, if the installer
  technology offers it. Also: the page started the download of an update in a frame of its own, and the webview
  cancels a frame navigation outside the origin of the page and starts no download; the update check has no
  route, so the page asks the host for nothing: at its start it tells no new version, and in the options it
  says that the check failed. Also, from D-01: the registry value `installFolder`, under `HKLM` for a package
  per machine and under `HKCU` for one per user, tells the application which package installed it, and so
  which of the four files is its update; a silent installation closes a running application and does not
  start it again.

### T4 Cross-cutting services

Exit: in the installed preview the settings are migrated, the policies are honored, the logs are visible, a
crash produces a report, telemetry runs on a supported SDK.

- [ ] B-4 (was S-01) User settings. Hypothesis: read once, immutable snapshot, atomic save, migration from
  1.0.x. Also: the store behind `GET` and `PUT /options` is in memory, so the options start from their defaults
  at every start; a save that fails is an error with a code of its own; the registry value of 1.0.x that hides
  the version in the title of the window (`applicationTitleVersionHidden`), which 1.1.0 does not read; the size
  and the position of the window, which are not kept; the tests of the user interface (H-07), which start
  the application: once the settings are kept on disk they need a data folder of their own. Also, from D-01:
  the removal of a package removes the data folder of who removes it, with the settings, as in 1.0.x; the
  profile of the webview of 1.0.x, `%LOCALAPPDATA%\Bravo\.temp`, stays on the disk after an upgrade, where
  nothing reads it or removes it.
- [ ] B-5 (was S-02) Policies. Hypothesis: the same keys; the templates shipped in a subfolder of the
  installation. Also: `/options` knows no policy, so an option that a policy sets is returned and replaced like
  any other; the page starts with no policy, so its options dialog locks nothing; the packages of D-01 do not
  carry the templates of `policies/` yet.
- [ ] B-6 (was S-03) Network and proxy. Hypothesis: system proxy and auto-configuration detected without
  settings, integrated credentials first, a prompt as the last resort, a connectivity check with clear messages.
  Explore: the proxy in .NET, in the webview, in the sign-in library and in the Analysis Services client
  libraries. Also: the proxy options that `/options` accepts today, an address and a bypass list of regular
  expressions, which nothing reads yet; the webview is given no browser argument, where 1.0.x passed it the
  proxy. Also, Local Network Access ([announcement
  126](https://github.com/MicrosoftEdge/WebView2Announcements/issues/126)): when WebView2 offers the permission,
  the application grants it to the origin of the page; until then a permission that the application does not
  know, from a later runtime or from a later SDK, is left to the prompt of the engine. Measured with the checks
  turned on by feature flags ([specification](design/window-and-webview.md)): the request of the page waits,
  without reaching the host, for a prompt of the engine with Block and Allow; the application gets no permission
  request; the answer of the user is kept in the profile. In a probe outside the application, a grant through
  the DevTools protocol (`Browser.grantPermissions`) made the same request succeed.
- [ ] B-7 (was S-04) Diagnostics and logging. Hypothesis: in-memory log shown in the user interface, plus the
  event log. Also: the host has no logging provider yet, so the warning of an owner of the instance that cannot
  create its pipe reaches no log, and neither does a request of the API that failed: the `traceId` of a problem
  matches no log entry, and an error that a use case foresaw is answered and leaves no trace. The diagnostics
  pane of the page asks for the log of the host every 5 seconds, a call that has no route.
- [ ] B-8 (was S-05) Errors and crash reporting. Material: the branch `feat/crash-reporter-process`. Explore: a
  reporter as an application of its own or as a mode of the main executable. Also: replaces the minimal
  last-chance exception handling; a service of the host that fails stops the host, which closes the window with
  no message; the exceptions of tasks that nobody observed; a server of the API that cannot open its port, which
  fails the start of the host; the failures of the webview, which end the application with dialogs of their
  own and the exit codes 2, 3, 4 and 1223, and whose recoveries, a page loaded again or a new control, leave
  no trace. Also, from D-01: the packages install the symbols of the four assemblies of the host, so that a
  trace names its lines; `Bravo.Setup.exe` is compiled ahead of time and has none.
- [ ] B-9 (was S-06) Telemetry. Hypothesis: a supported SDK; the classic Application Insights SDK is retired on
  2027-03-31. Explore: Application Insights 3 or OpenTelemetry; consent. Also: the page gets no telemetry
  settings, creates no telemetry client and sends nothing. Also, from D-01:
  - The packages raise the events `Install` and `Uninstall` through `Bravo.Setup.exe`, which builds each
    event with the data of 1.0.x and passes it to `IInstallationEventSink`. The only implementation sends
    nothing: B-9 writes the one that sends. An installation waits for the program, so the sink returns
    within a few seconds and never fails for the network; the program has no package reference, and is
    compiled ahead of time.
  - The identifier of who installs is computed in `Bravo.Setup`, as 1.0.x computes it; the application
    needs the same one for its own events.
  - A removal reports only when who installed chose the telemetry of the installer: the package reads the
    choice from the registry. To compare the counts of the removals with the ones of 1.0.x, take this into
    account.
  - The registry value `applicationTelemetryEnabled` is the choice of who installs for the telemetry of the
    application: nothing reads it yet.

### T5 Access to models

Exit: sign-in, the lists of reports and datasets, a model opened from Power BI Desktop and from the service.

- [ ] B-10 (was M-01) Local Power BI Desktop. Hypothesis: isolated components for the instances, their ports and
  the report name. Also: the report name from the parent process, and `--ppid`; an activation with a server
  opens a report: the page gets `serverName` and `databaseName` in the `Activation` event and has
  `App.openReport`, which takes a report as the list of the local reports gives it. Today the page logs such an
  activation and opens nothing. The list of the local reports has no route, and the page asks for it every 10
  seconds; the dialog that shows it asks twice when it opens, and uses its table at the second answer, built or
  not.
- [ ] B-11 (was M-02) Model connections. Hypothesis: a tabular session behind a port, with an in-memory model
  for tests.
- [ ] B-12 (was M-03) Sign-in and the Power BI service. Hypothesis: interaction behind a port. Explore: the
  guidance for desktop applications; the Windows authentication broker (#879). Also: the account that the page
  remembers stays in the storage of the page, and is lost once at the upgrade from 1.0.x; an activation whose
  server is of the Power BI service opened a dataset (`App.openDataset`); a missing sign-in is an error with a
  code of its own, which the page answers with the sign-in dialog: it no longer takes a 401 for one, and at its
  start it still tries the sign-in with the account that it remembers, a call that has no route.

### T6 Features

Exit, for each feature: the same results as `main` on the sample models; its row of the manual checks ticked.

- [ ] B-13 (was F-01) Analyze Model.
- [ ] B-14 (was F-02) Format DAX.
- [ ] B-15 (was F-03) Export Data. The export is the first operation that reports progress: a resource of its
  own, created, read and cancelled through its routes
  ([ADR 0014](decisions/0014-api-conventions-and-a-contract-kept-as-a-file.md)).
- [ ] B-16 (was F-04) Manage Dates. To settle: where the date templates live; `README.md` links them at their
  path on `main`.
- [ ] B-17 (was F-05) Template Development. The Visual Studio Code extension as published
  (`sql-bi/BravoTemplateEditor` 1.0.1, commit `31e4793`) must keep working unchanged
  ([ADR 0012](decisions/0012-http-on-loopback-between-user-interface-and-host.md)). What it expects from the host:
  - It finds the host only through `bravo-config.json` at the root of the workspace folder: a JSON object with
    `address`, an absolute `http://` URL with no trailing slash, and `token`, both names in lower case. Bravo
    writes the two values when the user presses "Edit" on a template workspace.
  - Every request carries the token alone as the value of the `Authorization` header, with no scheme, and
    `Content-Type: application/json`, also on a GET. The host must listen on TCP on loopback.
  - It calls four routes: `GET /TemplateDevelopment/GetReports`, `POST /TemplateDevelopment/GetModel`,
    `POST /TemplateDevelopment/GetPreviewChanges` and
    `GET /TemplateDevelopment/PBIDesktopOpenPBIX?path=<pbix>&waitForStarted=true`. The other seven routes of
    the group are called only by the user interface of Bravo.
  - It waits 60 seconds for an answer, which is JSON. An error is a ProblemDetails body; the statuses 401, 403,
    404 and 408 tell the extension that it is not connected.
  - In the API its routes are a group that does not change
    ([ADR 0014](decisions/0014-api-conventions-and-a-contract-kept-as-a-file.md)). Its token is a second
    authentication scheme, read from a header without a scheme name and accepted by that group alone: the
    fallback policy of the API names only the scheme of the user interface. To decide, which token the routes
    that the user interface also calls accept, and the status of an error that a use case foresaw, 409
    elsewhere.

### T7 Replacement of `main`

Exit: manual checks complete and a preview tried in the field; the final pull request merged with a merge
commit; `v1.1.0` released from GitHub Actions and announced by the feed; old branches and pull requests
removed; the Azure DevOps project retired; dependency updates configured again.

## Later

After 1.1.0.

- B-18 Rewrite of the TypeScript user interface.
- B-19 Strict type checking of the user interface, if the rewrite does not come first: with TypeScript 6.0.3
  `strict` reports 560 errors, about 380 of them for `null` and `undefined`, each a judgement on behavior.
- B-20 Manage Calendars.
- B-21 The Visual Studio Code extension for templates joins this repository.
- B-22 A cross-platform shell.
- B-23 WinGet (#898). A manifest names the four packages by their installation context; their product code
  changes at every build, and their UpgradeCode never does.
- B-24 The Microsoft Store, which also takes a signed package of Windows Installer that installs silently.
- B-25 WiX 7, when its maintenance fee is settled.
- B-26 An action of the application that registers it as an external tool of Power BI Desktop for a per-user
  installation, asking for administrator rights. `Bravo.Setup.exe register-external-tool` writes the file,
  and no package installs that program: what the application starts is part of the item.
- B-27 An upgrade that stays in the folder of the installed version: today a package installs in its default
  folder unless `INSTALLFOLDER` is given, as 1.0.x does.
- B-28 A PNG image as the icon of the external tool registration: today it is the image of 1.0.x, an icon file
  under the media type `image/png`. To check in Power BI Desktop.

## Open decisions

Each is answered by the design of the component that it names.

- B-29 Is the crash reporter a mode of the main executable or an application of its own? B-8.
- B-30 How is the version computed and how are release notes and the changelog produced? B-2.
