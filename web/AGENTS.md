# AGENTS.md

Instructions for agents working on the user interface, in `web/`. They add to
[the ones of the repository](../AGENTS.md).

`web/` holds the user interface of the previous implementation: TypeScript and LESS, bundled by webpack. It
reads what the application is, the options, the events and the latest release of the host, opens links in the
browser, tests the connection and asks for the credentials of the proxy server, through the client generated
from the OpenAPI document. Its other calls belong to features that
have not moved: they go through `Host.apiCall`, which sends nothing and fails at once with an error that says
that the feature is not available yet. Its commands run in `web/` and need Node.js 26 with npm:
`package.json` pins that line in `devEngines`, and npm stops with an error on another one.

| Task | Command |
| --- | --- |
| Install the dependencies of the lockfile | `npm ci` |
| Release bundle into `artifacts/web` | `npm run build:Release` |
| Release bundle into another folder | `npm run build:Release -- --output-path <folder>` |
| Development bundle into `web/wwwroot` | `npm run build:UI` |
| Development bundle, rebuilt at every change | `npm run watch:UI` |
| Start a development host for a page in a browser | `npm run serve:host` |
| Serve the page to a browser, rebuilt at every change | `npm run serve:UI` |
| Generate the client of the host API alone | `npm run generate:api` |
| Check the types of the sources and of the tests | `npm run check` |
| Run the unit tests | `npm test` |
| Run the tests of one file | `npx vitest run tests/controllers/host-fetch.test.ts`, after `npm run generate:api` |
| Run the tests of the window, after the two builds that they need | `npm run test:window` |
| Run the tests of the window of one file | `npx playwright test window-tests/start.spec.ts` |
| Take the screens of the window into `artifacts/screens/<name>`, after the same two builds | `npm run screens -- take <name>` |
| Compare two sets of screens, byte for byte | `npm run screens -- compare <name> <name>` |

A bundle is `js/main.js` and `css/main.css`; the release build adds `js/main.js.LICENSE.txt`, the development
build `css/main.css.map`. Every build script first runs `npm run check`, which generates the client of the host
API and checks the types with `tsc`, on the sources and the unit tests, then on the tests of the window; webpack
only translates, with ts-loader in `transpileOnly`
([ADR 0017](../docs/decisions/0017-typescript-6-with-the-types-checked-apart-from-the-bundle.md)). The stylesheet goes
through less-loader alone and the CSS support of webpack, with `url()` left as written; the JavaScript is
minified, the stylesheet is not
([ADR 0019](../docs/decisions/0019-the-stylesheet-through-the-css-support-of-webpack-not-minified.md)).
`watch:UI` and `serve:UI` check once, when they start, and not at each change. `web/wwwroot` holds the static
files that go with the bundle (`index.html`, icons, fonts, images); git ignores the `js` and `css` folders that
the development build adds there. Without `--output-path` webpack writes to `artifacts/web` at the root of the
repository, which git ignores.

The page starts in `web/main.ts`, the same in the window and in a browser:

- It reads the address and the token of the API from `window.bravoHost`, which whoever serves the page has
  set before any script: the webview ([Window and webview](../docs/design/window-and-webview.md)) or the development server.
  It has no global configuration object and no branch for where it runs.
- `start` asks the API for the application data, the options and the policies, then creates the controllers
  in their order. The modules import them from `main.ts`: a value that a module needs when it is loaded, and not
  when it is called, does not exist yet.
- A start that fails, at any point, shows `showStartFailure` (`web/view/start-failure.ts`) in place of the
  page. It has no options to read the language from: it takes the one of the browser, and the two texts it
  needs from the locale that `findLocale` gives for it.
- `HostEvents` (`web/controllers/host-events.ts`) reads the events of the host and is where a new kind of
  event is handled. The page has no message channel with the window.

The client of the host API is generated, not written:

- Every build script first runs [Orval](https://orval.dev) (`orval.config.mjs`), which reads
  `src/Bravo.Api/openapi.json` and writes `web/generated/bravo.ts`: a function for each route, named after its
  operation id, and the types that travel. Git ignores the folder, and a build on a clean clone needs no other
  step. `watch:UI` generates once, when it starts.
- A change of the API reaches the user interface through the document: write the document again
  ([API](../docs/design/api.md)), build, and the type check shows every call that no longer fits.
- Every generated function calls the host through `hostFetch` (`web/controllers/host-fetch.ts`), which adds
  the address and the token, passes on the `AbortSignal` of the caller, and throws a `HostProblem` for an
  error answer.
- A call that fails is an `AppError` (`web/model/exceptions.ts`), built by `AppError.InitFromCallError`: for
  an error answer its text is the one that `web/model/problem-texts.ts` has for the code of the answer. The
  table is typed by the codes of the generated client. An answer with status 409, an error that a use case
  foresaw, is shown by its code; any other one by its status.
- `Host` (`web/controllers/host.ts`) stays the one module that the user interface calls. A method that moves
  to the client goes through `apiClientCall` and returns what the client returns: a value that travels keeps
  its generated type and the names of the host, and nothing converts a name into a number. An enum of the
  user interface that the host also has is replaced by the generated one when its feature moves.
- Orval is pinned to an exact version in `package.json`: another version can write another client.

The options dialog draws each option with `Renderer.Options.render` (`web/helpers/renderer.ts`). An option
that asks before it changes, with `onBeforeChange`, goes back to the value that it had when the answer is no,
and when the question fails: whoever asks tells the user why.

The unit tests of the user interface are in `web/tests`, each at the path of the module that it tests, and
run on [Vitest](https://vitest.dev) in a simulated browser, jsdom
([ADR 0016](../docs/decisions/0016-closed-error-codes-generated-types-and-tests-in-the-user-interface.md)):

- They test the modules that read the contract, which are `host-fetch`, `host-events`, `host` and the
  errors with the table of their texts, and what the page does at its start: the sidebar, the theme, the
  page of a start that failed. They also keep the values of the feature flags of `model/tabular.ts`, which
  travel with the host, and the text that `sanitize-html` leaves of a name. No other view has a unit test:
  what the page does in a real engine is the matter of the tests of the window.
- `main.ts` starts the whole page when it is loaded. A test replaces it, with `vi.mock`, by the parts of
  `tests/main-mock.ts`, which do nothing, and replaces the generated client where the test answers in place
  of the host.
- The tests of what happens after a time, such as the waits of `host-events`, move the clock themselves
  (`vi.useFakeTimers`).
- Vite reads the sources for the tests, and transforms the namespaces of TypeScript only in part: one that
  exports a variable (`export var`, `export let`) is reported as a warning at every run, and the variable
  is not exported. The other namespaces and the enums of this code are read as webpack reads them.
- The tests are in the program of `tsconfig.json`, with the sources: `npm run check`, and so every build,
  checks their types.

The tests of the window are in `web/window-tests`, one file `*.spec.ts` for each part of the page, and run on
[Playwright Test](https://playwright.dev) against the window of the application
([ADR 0018](../docs/decisions/0018-tests-of-the-user-interface-in-the-window-with-playwright.md)):

- They start the `Bravo.exe` that the build wrote, `artifacts/bin/Bravo.Desktop/release/Bravo.exe`, which
  shows the development bundle that its build copied beside it. Before them: in `web/`, `npm run build:UI`;
  then, at the root, `dotnet build src/Bravo.Desktop -c Release`. Without the executable or the bundle a test
  fails with these two commands. They need Windows and the WebView2 runtime, and no browser of Playwright:
  `npx playwright install` is not needed.
- `window-tests/bravo.ts` is the fixture. For each test it starts the application with a new data folder, a
  temporary folder that it names with `--dev-data-folder` and where the application keeps its settings and the
  profile of its webview, and with the DevTools protocol on a port that the engine chooses: the application
  has no option for it. It waits until the engine lists the page, then attaches with
  `connectOverCDP`, because a page that appears later is not announced to Playwright, and waits until
  Playwright tells the page at its address: the engine lists the address before the page commits it. It
  gives the page the size 1280×800, removes the light color scheme that Playwright gives a page by default,
  so that the page has the one of the window, and waits for its start. The application reads the update feed
  from a server of the test, `window-tests/update-feed.ts`, through `--dev-update-feed`: the feed of 1.0.14,
  older than any build, unless the test sets the option `updateFeed`, to another feed or to `null` for none.
  With the option `settingsFile` the test writes the file of the settings in the data folder before the start;
  `bravo.readSettingsFile()` reads it again. With the option `proxyServer`, in place of `settingsFile`, the
  settings name a proxy server of the test, `window-tests/test-proxy.ts`, on the loopback address, which
  answers every request with the update feed or asks for credentials: `bravo.proxy` has its address and the
  addresses that it was asked for. The proxy variables of the environment of the tests, such as `HTTP_PROXY`,
  do not reach the application. With the option `policies`, the values of the policies by their
  names, the test writes them in a key of its own under `HKEY_CURRENT_USER\Software\Bravo.Tests`, which the
  application reads through `--dev-policy-key`, and removes the key at the end (`window-tests/policy-key.ts`).
  At the end it closes the window as a user does, expects exit code 0, waits for the end of the processes of
  the webview of that data folder and removes the folder.
- `window-tests/webview-settings.ts` gives the webview the browser argument that opens the DevTools protocol,
  and the run says in its log where it comes from. A process that is not elevated gives it in the environment
  variable of the WebView2 runtime `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS`. An elevated process, whose
  integrity level is High (`S-1-16-12288` in `whoami /groups`), has its webview ignore it: the hosted runner of
  GitHub Actions is elevated, and there, when `GITHUB_ACTIONS` is `true`, the fixture writes the same value as
  the policy of the machine `AdditionalBrowserArguments` for `Bravo.exe`, under
  `HKLM\SOFTWARE\Policies\Microsoft\Edge\WebView2`, and removes it after each test and in
  `window-tests/global-teardown.ts`. It fails, and replaces nothing, when a value `Bravo.exe` is already there.
  In an elevated terminal elsewhere it writes nothing and fails.
- The application has one instance, so one test runs at a time (`workers: 1`), and never while
  `dotnet test` runs: some tests of the host start the same executable. A development build that already runs
  takes the start of a test, which fails and says to close it. The installed application does not matter.
- A test reads the requests of the page without their headers, and never the token of the host.
- The tests: the start of the page (`start.spec.ts`), the theme and the diagnostic level in the options
  (`options.spec.ts`, through the dialog of `options.ts`), a feature that has no route yet
  (`features.spec.ts`), a second start with the arguments of Power BI Desktop (`activation.spec.ts`), a newer
  release in the update feed and its download (`updates.spec.ts`), the file of the settings of 1.0.14 and one
  that cannot be read (`settings.spec.ts`), the policies that lock the options and turn the update check off
  and on (`policies.spec.ts`), the proxy server of the user, its credentials and the connection test
  (`proxy.spec.ts`).
- A test never opens the browser of the machine: one that clicks a link first calls
  `bravo.answerBrowserRequests()`, which answers `POST /browser/open` with `page.route` and keeps the addresses
  in `bravo.openedInBrowser`. A test never shows the dialog of Windows for the credentials of the proxy server,
  nor deletes from the Credential Manager of the machine: one that asks for the credentials or forgets them
  first calls `bravo.answerProxyCredentialRequests(entered)`, which answers both requests, the first as a user
  who entered credentials or who did not. No other test sets a route: while the page has one, Playwright
  answers every CORS preflight in place of the host and turns off the cache.
- A test waits for an event or a state of the page, never for a time. Only the test of the start waits, 12
  seconds, to say that nothing else happens: longer than the 10 seconds after which the page asks for the
  list of the local reports. Its first start is over before the test can attach, so the test loads the page
  again and watches that start.
- A test that reads a text of the page reads the English one, which the page shows on an English Windows.
- No retry: a test that fails only sometimes is a defect. A test that fails keeps its trace and a
  screenshot in `artifacts/window-tests/results`. The fixture traces the page itself: Playwright Test traces
  only the contexts that it creates, and not one that it attached to. A trace holds the headers of the
  requests, and so the token of the host of that test, whose process has ended.
- `playwright.config.ts` takes the files `*.spec.ts` of `window-tests` only, and Vitest the files `*.test.ts`
  of `tests` only. The tests of the window run in Node.js: they and the configuration are a program of their
  own, `window-tests/tsconfig.json`, with the types of Node.js, which the sources do not get.

The screens of the window compare two builds of the bundle, byte for byte
([ADR 0019](../docs/decisions/0019-the-stylesheet-through-the-css-support-of-webpack-not-minified.md)). The command that
takes and compares them is not a test of the window: neither `npm run test:window` nor continuous integration
runs it.

- `window-tests/take-screens.ts` takes the screens with the fixture of the tests, one application for each
  theme, Dark and Light: the welcome page, each section of the options dialog, the page of a feature that has
  no route yet, and the same page with the diagnostics pane open, whose times it writes as `00:00:00`. Before
  each screen it waits for the fonts and for two frames of the engine.
- `playwright.screens.config.ts` is the configuration of the tests of the window with that file alone; a failed
  run keeps its trace in `artifacts/window-tests/screens-results`. `window-tests/screens.mts`, which Node.js
  runs as it is, is the command: `take` replaces the folder `artifacts/screens/<name>` and runs Playwright Test
  on that configuration; `compare` names each screen that differs or that one set alone has, and ends with 1
  when there is one, with 2 when it cannot compare.
- Rendering changes with the machine: two sets are compared only when one machine took both, one after the
  other. No screen enters the repository.

How to see the user interface in a browser, against a real host, and how to compare two builds of the bundle:
[the guides](../docs/guides/).
