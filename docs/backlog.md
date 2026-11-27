# Bravo: backlog

Next ID: B-48

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

As of 2026-11-27: the items of stage T4 Cross-cutting services are done but B-46, which is next: the classic SDK of
the host retires on 2027-03-31, whatever the date of 1.1.0. Then B-47, the one item left of T3 Distribution, and
B-10, the first item of T5. Two items of T2 wait for a release of their tools: B-1 for TypeScript 7.1 and
ts-loader 10, B-40 for a stable WebView2 SDK with the permission of Local Network Access. The first signed preview,
the exit of T3 and the place where the exit of T4 is checked, waits for the maintainer: the identity that signs and
the four variables of the repository ([guide](guides/make-a-release.md)). Telemetry sends to the instrumentation
key of 1.0.x until the maintainer gives the connection string of its resource ([telemetry](design/telemetry.md)).

## Next

### T2 Skeleton

Exit: the new executable starts, a second start activates the first, it shows the current user interface and
opens the options through the new API and the generated client.

- [ ] B-1 (was H-09) TypeScript 7, the native compiler. Not before TypeScript 7.1 and ts-loader 10 are released:
  7.0 has no API, 7.1 has a new one, and ts-loader 10 requires it. Hypothesis: the check of the types moves to
  the native `tsc`, and ts-loader 10 translates.
- [ ] B-40 Local Network Access in the webview ([announcement
  126](https://github.com/MicrosoftEdge/WebView2Announcements/issues/126)). Not before a stable WebView2 SDK has
  the permission of the announcement, which a prerelease SDK has: the page calls the host on the loopback
  address, and the application then grants that permission to the origin of the page. Until then a permission
  that the application does not know, from a later runtime or from a later SDK, is left to the prompt of the
  engine. Measured with the checks turned on by feature flags ([specification](design/window-and-webview.md)):
  the request of the page waits, without reaching the host, for a prompt of the engine with Block and Allow;
  the application gets no permission request; the answer of the user is kept in the profile. In a probe outside
  the application, a grant through the DevTools protocol (`Browser.grantPermissions`) made the same request
  succeed. To watch: a runtime that turns the checks on before the application grants the permission.

### T3 Distribution

Exit: GitHub Actions produces a signed, installable preview with release notes; installed over 1.0.14 it keeps
the four guarantees of [the specification](spec.md#guarantees-for-users-of-10x).

- [ ] B-47 The text of an error of `Bravo.Setup.exe` in the log of the installation, which today has its exit code
  alone (1, 2, 3): a program that a package runs has no handle to the installation
  ([Returning Error Messages from Custom Actions](https://learn.microsoft.com/windows/win32/msi/returning-error-messages-from-custom-actions)),
  and Microsoft asks that a custom action write its errors in the log
  ([Windows Installer Best Practices](https://learn.microsoft.com/windows/win32/msi/windows-installer-best-practices)).
  Chosen by the maintainer: a custom action library on .NET Framework 4.7.2 with DTF, as 1.0.x
  (`WixToolset.Dtf.CustomAction` 5.0.2), which writes its errors with `session.Log`. Its code runs in a process of
  its own, which ends with the action: nothing of it stays in the process of Windows Installer, not even a send
  past its time-out. The library stays in the `Binary` table, not installed, and `Return="ignore"` stays. The
  costs: about 930 lines ported, with `System.Text.Json` and `Microsoft.Bcl.TimeProvider` as packages and
  `HttpClientHandler` for the send; the tests on .NET Framework 4.7.2, the oldest that xUnit v3 runs on. To decide
  with the design: the C# version, 7.3 on .NET Framework unless a project sets it, which no project does today
  ([build](design/build.md)). Rejected: a native DLL compiled with Native AOT, which Windows Installer may
  unload while .NET does not support that
  ([native libraries](https://learn.microsoft.com/dotnet/core/deploying/native-aot/libraries)); WixQuietExec (an
  extension, and the program on disk at the removal too). The maintainer asks for the amendment of
  [ADR 0020](decisions/0020-windows-installer-packages-built-with-wix-which-run-a-program-of-the-host.md), which
  rejected this library, at the start of the branch of this component.

### T4 Cross-cutting services

Exit: in the installed preview the settings are migrated, the policies are honored, the logs are visible, a
crash produces a report, telemetry runs on a supported SDK.

- [ ] B-46 The telemetry of the host and of the reporter on a supported SDK, before 2027-03-31 whatever the date of
  1.1.0: Microsoft then ends the support of the classic SDK of Application Insights ([classic
  API](https://learn.microsoft.com/previous-versions/azure/azure-monitor/app/classic-api), [ADR
  0028](decisions/0028-telemetry-as-in-1-0-x-on-the-classic-sdk.md)). Hypothesis:
  OpenTelemetry with the exporter of Azure Monitor. Explore: an offline store and the end of a run, which the
  classic channel gives today and the exporter keeps in a folder of its own, for 48 hours
  ([offline storage](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-configuration#offline-storage-and-automatic-retries));
  the same names and values in the resource; compilation ahead of time does not concern the host. Also the SDK of
  the page, `@microsoft/applicationinsights-web` 2.8.18: version 3 has been out since 2023, and Microsoft requires
  an update when a newer GA version has been out for more than a year
  ([support guidance](https://learn.microsoft.com/troubleshoot/azure/azure-monitor/app-insights/telemetry/sdk-support-guidance)).

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
  for tests. Also, from B-6: the Analysis Services client libraries take no proxy from the application.
  Hypothesis: they read the default proxy of the process
  ([`HttpClient.DefaultProxy`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient.defaultproxy)),
  which the host and the reporter of a crash set from the settings before they send; to prove with a proxy of
  a test ([network](design/network.md)).
- [ ] B-12 (was M-03) Sign-in and the Power BI service. Hypothesis: interaction behind a port. Explore: the
  guidance for desktop applications; the Windows authentication broker (#879). Also: the account that the page
  remembers stays in the storage of the page, and is lost once at the upgrade from 1.0.x; an activation whose
  server is of the Power BI service opened a dataset (`App.openDataset`); a missing sign-in is an error with a
  code of its own, which the page answers with the sign-in dialog: it no longer takes a 401 for one, and at its
  start it still tries the sign-in with the account that it remembers, a call that has no route. Also, from
  B-5: the policy `UseSystemBrowserForAuthentication` is read and returned by `GET /policies`, and the page
  locks its option; the sign-in honors it ([policies](design/policies.md)). Also, from B-6: the sign-in library takes
  its HTTP client from the factory of the host
  ([`WithHttpClientFactory`](https://learn.microsoft.com/entra/msal/dotnet/advanced/httpclient)), so that its
  requests have the proxy of the settings, its credentials and the codes of a failure; a sign-in window, of
  the library or of the system, is a browser, which follows the proxy of Windows
  ([network](design/network.md)). To decide with the design: a client reports its own time-out as
  `ServerUnreachable`, which a library that is given a client cannot do; the handler of every client could
  keep the time instead.

### T6 Features

Exit, for each feature: the same results as `main` on the sample models; its row of the manual checks ticked.

- [ ] B-13 (was F-01) Analyze Model.
- [ ] B-14 (was F-02) Format DAX. From B-6: its requests go through a client of `IHttpClientFactory`, which has
  the proxy and the codes of a failure ([network](design/network.md)).
- [ ] B-15 (was F-03) Export Data. The export is the first operation that reports progress: a resource of its
  own, created, read and cancelled through its routes
  ([ADR 0014](decisions/0014-api-conventions-and-a-contract-kept-as-a-file.md)).
- [ ] B-16 (was F-04) Manage Dates. To settle: where the date templates live; `README.md` links them at their
  path on `main`. Also, from B-5: the policy `BuiltInTemplatesEnabled` is read and returned by
  `GET /policies`, and has no template, as in 1.0.x: it enters `bravo.admx` with the feature, in a new
  revision of the templates ([policies](design/policies.md)).
- [ ] B-17 (was F-05) Template Development. From B-5: the policies `CustomTemplatesEnabled` and
  `CustomTemplatesOrganizationRepositoryPath` are read and returned by `GET /policies`, the folder as the
  administrator wrote it; the page locks the option of the first. The folder has no template, as in 1.0.x: it
  enters `bravo.admx` with the feature, in a new revision of the templates ([policies](design/policies.md)).
  The Visual Studio Code extension as published
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
  fullscreen); or the video opens in the default browser of the user. The maintainer expects the player to stay
  in the webview: an indication for the design, not yet a decision. Explore: what an embedded player needs from
  the engine (storage and cookies of another origin, permissions), and what the window tests can prove without
  reaching the network. Also, from B-6 and B-9: the webview is given the proxy of the user as it applies to the
  endpoint of the telemetry, the one server outside the machine that the page calls today, and gives the
  credentials of the user only to the proxy that the settings choose for that endpoint
  ([network](design/network.md)). A player in the webview goes another way than the settings in two cases:
  exclusions of a Custom proxy that leave out the endpoint and not the player send the player direct, and the
  other way round through the proxy against its exclusion; a script of the system that sends the player to
  another proxy has the request for credentials of that proxy cancelled. A player that stays in the webview so
  needs two changes of `WebViewProxy`:
  - The engine gets the exclusions of a Custom proxy, not one choice computed for the endpoint of the telemetry.
    Its bypass list has no regular expressions: how to translate the exclusions is part of the design.
  - The credentials go to every proxy that Bravo uses, and not to whoever asks for them, since a site can ask
    for Basic too: to none with None, to the address of the proxy with Custom, to the proxy of Windows with
    System and a fixed proxy. With System and a script, the request names the proxy that asks and not the
    destination: the case to decide.

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
- B-37 The templates of the policies in the languages of the user interface: today in English alone, as in
  1.0.x.
- B-38 Policies for the options that have none, such as the proxy and the diagnostic level: the page already
  locks an option by its name.
- B-39 The policies read inside the critical section of Group Policy
  ([`EnterCriticalPolicySection`](https://learn.microsoft.com/windows/win32/api/userenv/nf-userenv-entercriticalpolicysection)),
  as Microsoft documents for an application that reads several policy entries: no refresh of Group Policy
  changes them while the start reads them. To weigh: the section can hold the start for up to 10 minutes.
- B-41 A connection test: the user asks the application whether it reaches the sites that it needs, and reads
  for each one the proxy server on the way and why it did not answer. A first one, a route of the host and a
  button of the proxy options, left before the first preview, because how it works did not convince the
  maintainer: it is at commit `5f8b931b`. To decide before it returns: which sites it asks, the ones of the
  sign-in, of the Power BI service, of DAX Formatter, of the updates and of the telemetry, and of which
  cloud; whether it tries the options on screen or the ones that are kept; how long the user waits for a
  site that does not answer ([network](design/network.md)).
- B-42 A command that opens the folder of the log files, in the diagnostics pane or in the options: today the
  user finds `%LOCALAPPDATA%\Bravo\Logs` by hand ([diagnostics](design/diagnostics.md)).
- B-43 A notice at the next start after a run that did not end: a stack overflow, `FailFast`, a crash of native
  code and a hang leave no report and no end in the log of the run ([diagnostics](design/diagnostics.md)).
- B-44 The application started again after a crash, when the user asks for it in the dialog of the reporter.
- B-45 A watchdog of the thread of the window, which records a hang with its stack: today Windows shows that the
  application does not answer, and the log has nothing of it.

## Open decisions

Each is answered by the design of the component that it names.

None is open.
