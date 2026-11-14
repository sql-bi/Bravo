# 0018. Tests of the user interface in the window, with Playwright

- Status: Accepted
- Date: 2026-11-14

## Context

The unit tests of the user interface run in a simulated browser
([ADR 0016](0016-closed-error-codes-generated-types-and-tests-in-the-user-interface.md)): nothing proves the
page in a real engine, and H-08 must compare the screens of two builds. WebView2 opens the DevTools protocol
for a browser argument ([Playwright](https://playwright.dev/docs/webview2),
[Microsoft](https://learn.microsoft.com/microsoft-edge/webview2/how-to/playwright)).

## Decision

- **The tests drive the window of `Bravo.exe`** through the DevTools protocol of its webview: the page that
  ships, in the shell that ships, against its host. The test, not the application, opens the port and gives
  a new profile, in environment variables. An elevated process ignores them
  ([Microsoft](https://learn.microsoft.com/microsoft-edge/webview2/concepts/security#for-an-elevated-host-app-use-appropriate-override-flags)),
  and the hosted runner of GitHub Actions is elevated: there alone, the test writes the two values as
  policies of the machine for `Bravo.exe`, and removes them after each test.
- **The runner is Playwright Test**, which attaches with `connectOverCDP` and downloads no browser.
- **One application per test, one test at a time**: the application has one instance.
- **No reference image in the repository**: rendering varies with the machine
  ([visual comparisons](https://playwright.dev/docs/test-snapshots)). A visual comparison is between two
  builds, in one run on one machine; H-08 is the first.
- **Continuous integration runs them in a job of their own** on Windows, after building the bundle and the
  application.

## Rejected alternatives

- The page in a browser, from the development server, alone or as one more test: not what ships.
- [Vitest browser mode](https://vitest.dev/guide/browser/why): not an end-to-end runner, by its
  documentation, and not on the bundle of webpack.
- [Edge WebDriver](https://learn.microsoft.com/microsoft-edge/webview2/how-to/webdriver): it must match the
  runtime version, which updates by itself.
- Inside `build-and-test`, the required check of `main`, which keeps its name and length.
- Starting `Bravo.exe` without elevation on the runner: no documented way with its one administrator account.
- An argument of the development build that sets the browser argument through the API: it changes the
  application, and Microsoft warns against shipping
  [browser flags](https://learn.microsoft.com/microsoft-edge/webview2/concepts/webview-features-flags).

## Consequences

- Over the DevTools protocol Playwright has less fidelity
  ([connectOverCDP](https://playwright.dev/docs/api/class-browsertype#browser-type-connect-over-cdp)): a test
  proves first what it needs.
- A running development build, or an elevated terminal outside GitHub Actions, stops the tests, which say
  so.
- The tests do not run with `dotnet test`, nor on Linux.
- A trace of a failed test holds the token of its host, whose process has ended.
