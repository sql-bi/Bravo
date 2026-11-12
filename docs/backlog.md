# Bravo: backlog

The open work toward 1.1.0, in the order to take it ([ADR 0007](adr/0007-redesign-on-a-development-branch.md)).
Status: `[ ]` to do, `[~]` in progress, `[-]` dropped (say why); a done item leaves the backlog.

An item is a component. Its line gives the starting hypothesis and what to explore before the design is
proposed; neither is a decision. One component is in progress at a time. A stage is closed when its exit
criterion can be checked on `develop-vnext`.

## T2 Skeleton

Exit: the new executable starts, a second start activates the first, it shows the current user interface and
opens the options through the new API and the generated client.

- [ ] H-04 Window and webview. Hypothesis: one component isolates the webview and stays neutral to its engine.
  Explore: the native webview of Avalonia; webview process failures; running the user interface in a plain
  browser for development and tests. Also:
  - Gives the page the address and the token of the API, which the host knows only after its start, and
    decides the origin of the page: the API allows no origin until then. A browser asks before each request
    from another origin and keeps the answer for a few seconds, because the API sets no maximum age for it.
  - The page reads the events of the host and acts on an activation. A page that is loaded again reads the
    events that the host still keeps, an activation already served included.
  - The user interface reads the `code` of a problem: today it reads the status and the `instance` member that
    the previous host wrote. It numbers the values of an enum that the host names, converted today for the
    options alone. It takes its first options and the policies from the configuration of the page, not from
    the API.
  - To decide, whether a development build is an instance of its own beside the installed application: today
    they are one instance, and the process test is skipped while an instance runs.
  - Updates the user interface dependencies that `npm audit` reports and makes the npm step of CI blocking.

## T3 Distribution

Exit: GitHub Actions produces a signed, installable preview with release notes; installed over 1.0.14 it keeps
the four guarantees of [the specification](spec.md#guarantees-for-users-of-10x).

- [ ] D-01 Installer. No hypothesis: the technology is open. Explore: MSI, MSIX, Velopack, Inno Setup and what
  well-known desktop applications choose. Criteria: the four guarantees, per-user and per-machine installation,
  enterprise deployment, external tool registration, signing, WinGet, licence. Also: a running instance during
  an upgrade, because 1.0.x and 1.1.0 do not recognize each other; the resource folders of System.CommandLine,
  one for each language, in the output of the build; the ASP.NET Core runtime, which the application needs
  beside the Windows Desktop one.
- [ ] D-02 Release. Hypothesis: GitHub Actions builds, signs, publishes a GitHub release with generated notes
  and changelog, and updates the feed. Explore: signing with a federated identity; changelog tools; how the
  version is computed.
- [ ] D-03 Updates. Hypothesis: a notification, as today. Explore: automatic update, if the installer
  technology offers it.

## T4 Cross-cutting services

Exit: in the installed preview the settings are migrated, the policies are honored, the logs are visible, a
crash produces a report, telemetry runs on a supported SDK.

- [ ] S-01 User settings. Hypothesis: read once, immutable snapshot, atomic save, migration from 1.0.x. Also:
  the store behind `GET` and `PUT /options` is in memory, so the options start from their defaults at every
  start; a save that fails is an error with a code of its own.
- [ ] S-02 Policies. Hypothesis: the same keys; the templates shipped in a subfolder of the installation. Also:
  `/options` knows no policy, so an option that a policy sets is returned and replaced like any other.
- [ ] S-03 Network and proxy. Hypothesis: system proxy and auto-configuration detected without settings,
  integrated credentials first, a prompt as the last resort, a connectivity check with clear messages. Explore:
  the proxy in .NET, in the webview, in the sign-in library and in the Analysis Services client libraries.
  Also: the proxy options that `/options` accepts today, an address and a bypass list of regular expressions,
  which nothing reads yet.
- [ ] S-04 Diagnostics and logging. Hypothesis: in-memory log shown in the user interface, plus the event log.
  Also: the host has no logging provider yet, so the warning of an owner of the instance that cannot create
  its pipe reaches no log, and neither does a request of the API that failed: the `traceId` of a problem
  matches no log entry, and an error that a use case foresaw is answered and leaves no trace.
- [ ] S-05 Errors and crash reporting. Material: the branch `feat/crash-reporter-process`. Explore: a reporter
  as an application of its own or as a mode of the main executable. Also: replaces the minimal last-chance
  exception handling; a service of the host that fails stops the host, which closes the window with no
  message; the exceptions of tasks that nobody observed; a server of the API that cannot open its port, which
  fails the start of the host.
- [ ] S-06 Telemetry. Hypothesis: a supported SDK; the classic Application Insights SDK is retired on
  2027-03-31. Explore: Application Insights 3 or OpenTelemetry; consent.

## T5 Access to models

Exit: sign-in, the lists of reports and datasets, a model opened from Power BI Desktop and from the service.

- [ ] M-01 Local Power BI Desktop. Hypothesis: isolated components for the instances, their ports and the
  report name. Also: the report name from the parent process, and `--ppid`.
- [ ] M-02 Model connections. Hypothesis: a tabular session behind a port, with an in-memory model for tests.
- [ ] M-03 Sign-in and the Power BI service. Hypothesis: interaction behind a port. Explore: the guidance for
  desktop applications; the Windows authentication broker (#879).

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
