# Tests of the user interface

Two kinds of tests prove the user interface
([ADR 0016](../decisions/0016-closed-error-codes-generated-types-and-tests-in-the-user-interface.md),
[ADR 0018](../decisions/0018-tests-of-the-user-interface-in-the-window-with-playwright.md)):

| Kind | Where they run | What they prove |
| --- | --- | --- |
| Unit tests, on Vitest | In Node.js, in a simulated browser | The modules that read the contract, and the start of the page, with the host replaced |
| Tests of the window, on Playwright Test | In the window of `Bravo.exe`, on Windows | The page that ships, inside the shell that ships, against its own host |

A test of the window starts the executable that the build wrote, with the development bundle beside it, and
drives its webview through the DevTools protocol. The test opens the protocol, on a port that the engine
chooses: the application never opens the port by itself and has no option for it. Each test has an
application of its own, with a new data folder, which holds its settings and the profile of its webview: the
test names it with the argument `--dev-data-folder` of a development build
([Window and webview](window-and-webview.md)), so that no test reads or writes the data of the developer. The
test closes the application as a user closes it, and it must end with exit code 0; no process of the
application or of its webview outlives the test, and neither does the data folder.

The WebView2 runtime reads the browser argument from its environment variable
`WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS`, unless the process is elevated: an elevated process ignores it and
honors the policies of the machine
([Microsoft](https://learn.microsoft.com/microsoft-edge/webview2/concepts/security#for-an-elevated-host-app-use-appropriate-override-flags)).
The hosted Windows runner of GitHub Actions runs every process elevated. There, and only there, the test
gives the same value as a policy of the machine for `Bravo.exe`, the value `Bravo.exe` of the key
`AdditionalBrowserArguments` under `HKLM\SOFTWARE\Policies\Microsoft\Edge\WebView2`, and removes it after
the test and once more at the end of the run. A value that is already there is not replaced: the test fails.
Elsewhere the tests never write that policy: in an elevated terminal they fail, and say to run them from a
terminal that is not elevated. The one key that a test writes on every machine is a key of its own under
`HKCU\Software\Bravo.Tests`, with the policies of its application, which the application reads through
`--dev-policy-key` and the test removes ([policies](policies.md)).

- The page has a fixed size in every test, 1280×800, whatever the size of the window, and a test can take a
  screenshot of it. The repository keeps no reference image: the rendering changes with the machine. A
  visual comparison is made between two builds, on one machine, one after the other.
- The application has one instance: one test runs at a time, and the tests do not run together with the
  tests of the host, which start the same executable. A development build that already runs takes the start
  of a test: the test fails and says so.
- A test reaches no service outside the machine: the application reads the update feed from a server of the
  test, and a request that would open the browser of the user is answered by the test
  ([updates](updates.md)).
- Only a test that answers a request in place of the host routes the requests of the page. While the page has
  a route, Playwright answers every CORS preflight in place of the host and turns off the cache: the other
  tests keep proving the CORS of the host.
- A test never reads the token of the host. A trace of a failed test holds the headers of its requests, and
  so the token of the host of that test, whose process has ended.

A change of the tools of the bundle is compared on the screens of the window
([ADR 0019](../decisions/0019-the-stylesheet-through-the-css-support-of-webpack-not-minified.md)), with a command that
runs on request, and not with the tests of the window or in continuous integration. It takes a fixed set of
screens of the bundle beside the executable, in the themes Dark and Light: the welcome page, every section of
the options dialog, the page of a feature that has no route yet, and the same page with the diagnostics pane
open, whose times are written as one fixed time. It compares two sets file by file, byte for byte, names the
screens that differ or that one set alone has, and fails when there is one. The sets stay in `artifacts/`.

How to run them and what each file holds: [web/AGENTS.md](../../web/AGENTS.md).
