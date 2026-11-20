# Bravo: backlog

Next ID: B-37

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

As of 2026-11-19: the items of stage T3 Distribution are done, and so is B-4, the first item of T4
Cross-cutting services; B-5 is next. B-1, left in T2, waits for the release of TypeScript 7.1 and ts-loader 10. The first signed preview, the exit of
T3, waits for the maintainer: the identity that signs and the four variables of the repository
([guide](guides/make-a-release.md)).

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

No item is left: the exit waits for the first signed preview.

### T4 Cross-cutting services

Exit: in the installed preview the settings are migrated, the policies are honored, the logs are visible, a
crash produces a report, telemetry runs on a supported SDK.

- [ ] B-5 (was S-02) Policies. Hypothesis: the same keys; the templates shipped in a subfolder of the
  installation. Also: `/options` knows no policy, so an option that a policy sets is returned and replaced like
  any other; the page starts with no policy, so its options dialog locks nothing; the packages of D-01 do not
  carry the templates of `policies/` yet. Also, from B-3: the policy `UpdateCheckEnabled` turned off stops
  every request to the update feed ([ADR 0022](decisions/0022-updates-read-from-the-feed-of-1-0-x-and-downloaded-by-the-browser.md)):
  `GET /releases/latest` then reads nothing, and the page, which asks at its start and in the options, tells
  nothing. To decide: the answer of the route, a code of its own or the one of a failed check. Also, from B-4:
  a policy applies over the settings of the user, which the store reads and writes without it, and is never
  written in their file ([settings](design/settings.md)).
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
  the DevTools protocol (`Browser.grantPermissions`) made the same request succeed. Also, from B-3: the update
  feed is read by the typed client `HttpReleaseFeed` of `IHttpClientFactory`, so the proxy can be set on every
  client in one place; behind a proxy that asks for credentials, an answer 407, the check fails with
  `UpdateCheckFailed`. Also, from B-4: the proxy of the user is kept in the settings file as 1.0.x keeps it, and
  one whose address or bypass list `/options` would refuse is read as the default proxy.
- [ ] B-7 (was S-04) Diagnostics and logging. Hypothesis: in-memory log shown in the user interface, plus the
  event log. Also: the host has no logging provider yet, so the warning of an owner of the instance that cannot
  create its pipe reaches no log, and neither does a request of the API that failed: the `traceId` of a problem
  matches no log entry, and an error that a use case foresaw is answered and leaves no trace. The diagnostics
  pane of the page asks for the log of the host every 5 seconds, a call that has no route. Also, from B-3: an
  update check that fails keeps its cause, the status of the answer or the exception, in the `BravoException`,
  and nothing writes it. Also, from B-4: a settings file that cannot be read or written is a warning of
  `FileUserSettingsStore`, which reaches no log; the page tells the user, and writes the error in its own
  diagnostic log.
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
    application: the settings start from it when there is no settings file ([settings](design/settings.md)).
  - From the release: an event of the installer carries the semantic version as the version of the product
    and the file version as its build, as in 1.0.x; outside a public release the first ends with the
    commit. The workflow of a release holds no secret: a value of the telemetry that is not in the sources
    needs a variable of the repository, or a secret, which the release does not have yet.
  - From B-3: the page raises the event `Link` also when the user downloads an update, which 1.0.x did not:
    it downloaded the file in a frame, and raised no event.

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
- [ ] B-36 The videos of the help dialog. The dialog of a feature shows its video in a frame of the Vimeo
  player (`player.vimeo.com`, `web/view/help-dialog.ts`), as 1.0.x does; the webview cancels a navigation of a
  frame outside the origin of the page ([design](design/window-and-webview.md)), so the video does not start.
  Hypothesis: the frame may navigate to the player alone, with the permissions that it asks for (autoplay,
  fullscreen); or the video opens in the default browser of the user. Explore: what an embedded player needs
  from the engine (storage and cookies of another origin, permissions), and what the window tests can prove
  without reaching the network.

### T7 Replacement of `main`

Exit: manual checks complete and a preview tried in the field; the final pull request merged with a merge
commit; `v1.1.0` released from GitHub Actions and announced by the feed; old branches and pull requests
removed; the Azure DevOps project retired; dependency updates configured again.

- [ ] B-31 The release from `main`. After the final merge the release changes branch
  ([guide](guides/make-a-release.md)): the federated credential of the identity that signs names a branch,
  so `main` gets one and the one of `develop-vnext` is removed; `release.yml` is then started from the
  button of GitHub; the first stable release needs its update feed uploaded to the two containers; the
  pipeline of Azure DevOps and its client secret are retired with the project. Also: the dependency updates
  cover the tool manifest `.config/dotnet-tools.json`, the package `Nerdbank.GitVersioning` and the actions
  of the two workflows.

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
- B-32 An attestation of provenance for the files of a release
  ([artifact attestations](https://docs.github.com/en/actions/concepts/security/artifact-attestations)), which
  ties each file to the commit and to the run that built it.
- B-33 The name of the product in the signature of a package: Windows shows the description of a signature
  when a package asks for administrator rights, and the signature of 1.0.x has none.
- B-34 The size and the position of the window, kept from one start to the next: neither 1.0.x nor 1.1.0 keeps
  them.
- B-35 The profile of the webview of 1.0.x, `%LOCALAPPDATA%\Bravo\.temp`, removed after an upgrade: nothing
  reads it, and it stays until the removal of the package removes the data folder.

## Open decisions

Each is answered by the design of the component that it names.

- B-29 Is the crash reporter a mode of the main executable or an application of its own? B-8.
