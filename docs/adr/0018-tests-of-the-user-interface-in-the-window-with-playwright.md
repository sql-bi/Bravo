# 0018. Tests of the user interface in the window, with Playwright

- Status: Accepted
- Date: 2026-11-14

## Context

The unit tests of the user interface run in a simulated browser
([ADR 0016](0016-closed-error-codes-generated-types-and-tests-in-the-user-interface.md)): nothing proves the
page in a real engine, and H-08 must compare the screens of two builds. WebView2 accepts the DevTools
protocol on a port that an environment variable names ([Playwright](https://playwright.dev/docs/webview2),
[Microsoft](https://learn.microsoft.com/microsoft-edge/webview2/how-to/playwright)).

## Decision

- **The tests drive the window of `Bravo.exe`** through the DevTools protocol of its webview: the page that
  ships, inside the shell that ships, against its own host. The test gives the port and a new profile of
  the webview through environment variables; the application never opens the port itself.
- **The runner is Playwright Test**, which attaches to the webview with `connectOverCDP` and downloads no
  browser. Visual Studio Code tests its own interface with Playwright
  ([test/automation](https://github.com/microsoft/vscode/tree/main/test/automation)).
- **One application for each test, one test at a time**: the application has one instance.
- **No reference image in the repository**: rendering varies with the operating system, the hardware and the
  settings ([visual comparisons](https://playwright.dev/docs/test-snapshots)). A visual comparison is made
  between two builds, in one run on one machine; H-08 is the first.
- **Continuous integration runs the tests in a job of their own**, on Windows, which builds the bundle and the
  application first.

## Rejected alternatives

- The page in a browser, from the development server, alone or as one more test: three processes, and not
  what ships.
- [Vitest browser mode](https://vitest.dev/guide/browser/why): by its own documentation it does not replace an
  end-to-end runner, it runs on Vite and not on the bundle of webpack, and its visual comparison is
  experimental.
- [Microsoft Edge WebDriver](https://learn.microsoft.com/microsoft-edge/webview2/how-to/webdriver): the driver
  must match the version of the WebView2 runtime, which updates by itself.
- The tests inside `build-and-test`, the required status check of `main`, which keeps its name and length.

## Consequences

- Over the DevTools protocol Playwright has less fidelity than over its own protocol
  ([connectOverCDP](https://playwright.dev/docs/api/class-browsertype#browser-type-connect-over-cdp)): what a
  test needs is proved to work before it is used.
- A development build that already runs takes the start of a test: the tests stop and say so.
- The tests do not run with `dotnet test`, which also starts the application, nor on Linux.
- A trace of a failed test holds the token of the API of that test, whose process has ended.
