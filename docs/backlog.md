# Bravo: backlog

The open work toward 1.1.0, in the order to take it ([ADR 0007](adr/0007-redesign-on-a-development-branch.md)).
Status: `[ ]` to do, `[~]` in progress, `[-]` dropped (say why); a done item leaves the backlog.

An item is a component. Its line gives the starting hypothesis and what to explore before the design is
proposed; neither is a decision. One component is in progress at a time. A stage is closed when its exit
criterion can be checked on `develop-vnext`.

## T2 Skeleton

Exit: the new executable starts, a second start activates the first, it shows the current user interface and
opens the options through the new API and the generated client.

- [ ] H-01 Host architecture. Hypothesis: portable core and API projects, Windows adapters, a shell; the
  compiler keeps Windows out of the core. Explore: .NET applications with a web user interface; HTTP on loopback
  or the message channel of the webview. Also: fills the table of allowed project references; decides how the
  architecture tests load Windows-only assemblies; adds the solution filter of the Linux job of CI with the
  first Windows-only project.
- [ ] H-02 Startup, lifetime and single instance. Hypothesis: today's order, from the exception handler to the
  window. Explore: the generic host in desktop applications; single instance in well-known desktop applications.
- [ ] H-03 API and the contract with the user interface. Decided
  ([ADR 0012](adr/0012-http-on-loopback-between-user-interface-and-host.md)): HTTP with minimal APIs, served on
  loopback with a token for each process, host filtering and CORS restricted to the origin of the user
  interface; an OpenAPI document; a generated TypeScript client. Hypothesis for what is open: routes by
  resource, standard errors with stable codes, a versioned OpenAPI document, the generated client behind the
  single module that calls the host today. Explore: REST guidelines; client generators; long operations by
  polling or events.
- [ ] H-04 Window and webview. Hypothesis: one component isolates the webview and stays neutral to its engine.
  Explore: the native webview of Avalonia; webview process failures; running the user interface in a plain
  browser for development and tests. Also: updates the user interface dependencies that `npm audit` reports
  and makes the npm step of CI blocking.

## T3 Distribution

Exit: GitHub Actions produces a signed, installable preview with release notes; installed over 1.0.14 it keeps
the four guarantees of [the specification](spec.md#guarantees-for-users-of-10x).

- [ ] D-01 Installer. No hypothesis: the technology is open. Explore: MSI, MSIX, Velopack, Inno Setup and what
  well-known desktop applications choose. Criteria: the four guarantees, per-user and per-machine installation,
  enterprise deployment, external tool registration, signing, WinGet, licence.
- [ ] D-02 Release. Hypothesis: GitHub Actions builds, signs, publishes a GitHub release with generated notes
  and changelog, and updates the feed. Explore: signing with a federated identity; changelog tools; how the
  version is computed.
- [ ] D-03 Updates. Hypothesis: a notification, as today. Explore: automatic update, if the installer
  technology offers it.

## T4 Cross-cutting services

Exit: in the installed preview the settings are migrated, the policies are honored, the logs are visible, a
crash produces a report, telemetry runs on a supported SDK.

- [ ] S-01 User settings. Hypothesis: read once, immutable snapshot, atomic save, migration from 1.0.x.
- [ ] S-02 Policies. Hypothesis: the same keys; the templates shipped in a subfolder of the installation.
- [ ] S-03 Network and proxy. Hypothesis: system proxy and auto-configuration detected without settings,
  integrated credentials first, a prompt as the last resort, a connectivity check with clear messages. Explore:
  the proxy in .NET, in the webview, in the sign-in library and in the Analysis Services client libraries.
- [ ] S-04 Diagnostics and logging. Hypothesis: in-memory log shown in the user interface, plus the event log.
- [ ] S-05 Errors and crash reporting. Material: the branch `feat/crash-reporter-process`. Explore: a reporter
  as an application of its own or as a mode of the main executable.
- [ ] S-06 Telemetry. Hypothesis: a supported SDK; the classic Application Insights SDK is retired on
  2027-03-31. Explore: Application Insights 3 or OpenTelemetry; consent.

## T5 Access to models

Exit: sign-in, the lists of reports and datasets, a model opened from Power BI Desktop and from the service.

- [ ] M-01 Local Power BI Desktop. Hypothesis: isolated components for the instances, their ports and the
  report name.
- [ ] M-02 Model connections. Hypothesis: a tabular session behind a port, with an in-memory model for tests.
- [ ] M-03 Sign-in and the Power BI service. Hypothesis: interaction behind a port. Explore: the guidance for
  desktop applications; the Windows authentication broker (#879).

## T6 Features

Exit, for each feature: the same results as `main` on the sample models; its row of the manual checks ticked.

- [ ] F-01 Analyze Model.
- [ ] F-02 Format DAX.
- [ ] F-03 Export Data.
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
