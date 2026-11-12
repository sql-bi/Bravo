# Bravo: backlog

The open work toward 1.1.0, in the order to take it ([ADR 0007](adr/0007-redesign-on-a-development-branch.md)).
Status: `[ ]` to do, `[~]` in progress, `[-]` dropped (say why); a done item leaves the backlog.

An item is a component. Its line gives the starting hypothesis and what to explore before the design is
proposed; neither is a decision. One component is in progress at a time. A stage is closed when its exit
criterion can be checked on `develop-vnext`.

## T2 Skeleton

Exit: the new executable starts, a second start activates the first, it shows the current user interface and
opens the options through the new API and the generated client.

- [ ] H-05 User interface on the new contract. The user interface reads the `code` of a problem: today it
  reads the status and the `instance` member that the previous host wrote. It numbers the values of an enum
  that the host names, converted today for the options alone. Also: updates the user interface dependencies
  that `npm audit` reports and makes the npm step of CI blocking. Also, left by the window and the webview:
  - The calls that are not on the generated client never leave the page: `Utils.Request.ajax` adds the request
    header `Access-Control-Allow-Origin`, which the API does not allow, so the browser engine stops them. Sent
    without that header, their token has no scheme: the API answers 401, which the page takes for a missing
    sign-in and answers with the sign-in dialog.
  - At every start one of them fails, the update check (`api/GetCurrentVersion`), and leaves a rejected
    promise that nothing handles. The page also replaces the options once at every start, right after it has
    read them.
  - `host.changeTheme` told the window the theme of the user, for its title bar: the window follows the color
    mode of the system until the page can tell it.
  - A page that cannot start says so in one line of English text.
  - The development server is started through the API of webpack-dev-server, because the webpack-cli of the
    repository is older than the one that the server asks for.

## T3 Distribution

Exit: GitHub Actions produces a signed, installable preview with release notes; installed over 1.0.14 it keeps
the four guarantees of [the specification](spec.md#guarantees-for-users-of-10x).

- [ ] D-01 Installer. No hypothesis: the technology is open. Explore: MSI, MSIX, Velopack, Inno Setup and what
  well-known desktop applications choose. Criteria: the four guarantees, per-user and per-machine installation,
  enterprise deployment, external tool registration, signing, WinGet, licence. Also: a running instance during
  an upgrade, because 1.0.x and 1.1.0 do not recognize each other; the resource folders of System.CommandLine,
  one for each language, in the output of the build; the ASP.NET Core runtime, which the application needs
  beside the Windows Desktop one. Also, for the webview:
  - The WebView2 runtime: the application does not install it, and without it ends with a message and exit
    code 1223. Windows 11 has it; the installer checks or installs it elsewhere.
  - `wwwroot` beside the executable: a release build copies nothing there. The folder is the static files of
    `web/wwwroot` with the release bundle of `artifacts/web` (`js/main.js`, `css/main.css` and the licence
    text of the bundle). Without `wwwroot\index.html` the application ends with exit code 2.
  - The assemblies of the WebView2 package and its loader,
    `runtimes\win-<architecture>\native\WebView2Loader.dll` for three architectures, in the output of the
    build.
  - The profile of the webview of 1.0.x, `%LOCALAPPDATA%\Bravo\.temp`: 1.1.0 keeps its own in
    `%LOCALAPPDATA%\Bravo\WebView2` and neither reads nor removes the old folder. What the page stored there
    is not carried over: the origin of the page changed as well.
- [ ] D-02 Release. Hypothesis: GitHub Actions builds, signs, publishes a GitHub release with generated notes
  and changelog, and updates the feed. Explore: signing with a federated identity; changelog tools; how the
  version is computed. Also: the release builds with `-p:BravoReleaseBuild=true`, or what it publishes is a
  development build, with another instance, another data folder and the arguments for a browser; the title of
  the window and `GET /application` show the informational version of the assembly, which nothing sets yet.
- [ ] D-03 Updates. Hypothesis: a notification, as today. Explore: automatic update, if the installer
  technology offers it. Also: the page started the download of an update in a frame of its own, and the webview
  cancels a frame navigation outside the origin of the page and starts no download; the update check has no
  route, and fails at every start of the page.

## T4 Cross-cutting services

Exit: in the installed preview the settings are migrated, the policies are honored, the logs are visible, a
crash produces a report, telemetry runs on a supported SDK.

- [ ] S-01 User settings. Hypothesis: read once, immutable snapshot, atomic save, migration from 1.0.x. Also:
  the store behind `GET` and `PUT /options` is in memory, so the options start from their defaults at every
  start; a save that fails is an error with a code of its own; the registry value of 1.0.x that hides the
  version in the title of the window (`applicationTitleVersionHidden`), which 1.1.0 does not read; the size
  and the position of the window, which are not kept.
- [ ] S-02 Policies. Hypothesis: the same keys; the templates shipped in a subfolder of the installation. Also:
  `/options` knows no policy, so an option that a policy sets is returned and replaced like any other; the
  page starts with no policy, so its options dialog locks nothing.
- [ ] S-03 Network and proxy. Hypothesis: system proxy and auto-configuration detected without settings,
  integrated credentials first, a prompt as the last resort, a connectivity check with clear messages. Explore:
  the proxy in .NET, in the webview, in the sign-in library and in the Analysis Services client libraries.
  Also: the proxy options that `/options` accepts today, an address and a bypass list of regular expressions,
  which nothing reads yet; the webview is given no browser argument, where 1.0.x passed it the proxy. Also,
  Local Network Access ([announcement 126](https://github.com/MicrosoftEdge/WebView2Announcements/issues/126)):
  when WebView2 offers the permission, the application grants it to the origin of the page; until then a
  permission that its version of the SDK cannot name is left to the prompt of the engine. Measured with the
  checks turned on by feature flags ([specification](spec.md#window-and-webview)): the request of the page
  waits, without reaching the host, for a prompt of the engine with Block and Allow; the application gets no
  permission request; the answer of the user is kept in the profile. In a probe outside the application, a
  grant through the DevTools protocol (`Browser.grantPermissions`) made the same request succeed.
- [ ] S-04 Diagnostics and logging. Hypothesis: in-memory log shown in the user interface, plus the event log.
  Also: the host has no logging provider yet, so the warning of an owner of the instance that cannot create
  its pipe reaches no log, and neither does a request of the API that failed: the `traceId` of a problem
  matches no log entry, and an error that a use case foresaw is answered and leaves no trace.
- [ ] S-05 Errors and crash reporting. Material: the branch `feat/crash-reporter-process`. Explore: a reporter
  as an application of its own or as a mode of the main executable. Also: replaces the minimal last-chance
  exception handling; a service of the host that fails stops the host, which closes the window with no
  message; the exceptions of tasks that nobody observed; a server of the API that cannot open its port, which
  fails the start of the host; the failures of the webview, which end the application with dialogs of their
  own and the exit codes 2, 3, 4 and 1223, and whose recoveries, a page loaded again or a new control, leave
  no trace.
- [ ] S-06 Telemetry. Hypothesis: a supported SDK; the classic Application Insights SDK is retired on
  2027-03-31. Explore: Application Insights 3 or OpenTelemetry; consent. Also: the page gets no telemetry
  settings, creates no telemetry client and sends nothing.

## T5 Access to models

Exit: sign-in, the lists of reports and datasets, a model opened from Power BI Desktop and from the service.

- [ ] M-01 Local Power BI Desktop. Hypothesis: isolated components for the instances, their ports and the
  report name. Also: the report name from the parent process, and `--ppid`; an activation with a server opens
  a report: the page gets `serverName` and `databaseName` in the `Activation` event and has `App.openReport`,
  which takes a report as the list of the local reports gives it. Today the page logs such an activation and
  opens nothing.
- [ ] M-02 Model connections. Hypothesis: a tabular session behind a port, with an in-memory model for tests.
- [ ] M-03 Sign-in and the Power BI service. Hypothesis: interaction behind a port. Explore: the guidance for
  desktop applications; the Windows authentication broker (#879). Also: the account that the page remembers
  stays in the storage of the page, and is lost once at the upgrade from 1.0.x; an activation whose server is
  of the Power BI service opened a dataset (`App.openDataset`).

## T6 Features

Exit, for each feature: the same results as `main` on the sample models; its row of the manual checks ticked.

- [ ] F-01 Analyze Model.
- [ ] F-02 Format DAX.
- [ ] F-03 Export Data. The export is the first operation that reports progress: a resource of its own,
  created, read and cancelled through its routes
  ([ADR 0014](adr/0014-api-conventions-and-a-contract-kept-as-a-file.md)).
- [ ] F-04 Manage Dates. To settle: where the date templates live; `README.md` links them at their path on
  `main`.
- [ ] F-05 Template Development. The Visual Studio Code extension as published (`sql-bi/BravoTemplateEditor`
  1.0.1, commit `31e4793`) must keep working unchanged
  ([ADR 0012](adr/0012-http-on-loopback-between-user-interface-and-host.md)). What it expects from the host:
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
    ([ADR 0014](adr/0014-api-conventions-and-a-contract-kept-as-a-file.md)). Its token is a second
    authentication scheme, read from a header without a scheme name and accepted by that group alone: the
    fallback policy of the API names only the scheme of the user interface. To decide, which token the routes
    that the user interface also calls accept, and the status of an error that a use case foresaw, 409
    elsewhere.

## T7 Replacement of `main`

Exit: manual checks complete and a preview tried in the field; the final pull request merged with a merge
commit; `v1.1.0` released from GitHub Actions and announced by the feed; old branches and pull requests
removed; the Azure DevOps project retired; dependency updates configured again.

## After 1.1.0

- Rewrite of the TypeScript user interface.
- Manage Calendars.
- The Visual Studio Code extension for templates joins this repository.
- A cross-platform shell.
- WinGet (#898).
