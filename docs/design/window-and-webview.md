# Window and webview

The main window shows the user interface in a webview, WebView2
([ADR 0015](../decisions/0015-page-under-a-reserved-host-name-api-connection-in-an-object.md)). Everything that knows
the engine is in one folder of `Bravo.Desktop`: a shell on another engine rewrites that folder.

**Window.** The icon of the application, the title of the
[build identity](startup.md), the size that Windows gives a new window, in the
middle of the screen. The webview stays hidden until the first document has loaded: until then the window
shows the background of the user interface, so nothing flashes white. Nothing of the window is kept from one
start to the next (backlog B-34).

**Theme.** The window has the theme of the user, at the start and whenever the options change: the settings
tell that they were replaced, and the window reads them again on its own thread. The page tells the window
nothing: it changes the options.

With `Auto` the window has the color mode that Windows gives to applications, the "default app mode" of its
color settings, and follows it when it changes. It is not the mode of Windows itself, which colors the
taskbar and the Start menu: a user can have that one dark and the applications light. The application reads
the app mode where Windows keeps it for the user, the value that the engine of the webview reads to tell the
page its color scheme, so the frame and the page agree. It does not ask Windows Forms, which answers light
on every Windows before 11.

| Part | What the theme sets |
| --- | --- |
| The frame of the window, with its title bar | Dark or light, by a request to the window manager that Windows documents from Windows 11 on (build 22000). Where Windows does not know the request, the frame stays as it is |
| The background of the window and of the webview | The background of the user interface in that mode |
| The webview | The color scheme of its profile, which a page reads as `prefers-color-scheme`: the page has the theme of the user before it has read the options |

On Windows 10 the frame needs two more things, neither of them documented by Windows:

- The request has the same number from version 2004 on, and the number before it from version 1809: the
  application tries the first and, when Windows refuses it, the second, as
  [Qt](https://github.com/qt/qtbase/blob/dev/src/plugins/platforms/windows/qwindowswindow.cpp) does.
- Windows 10 goes on drawing the frame that the window had until the window gains or loses the activation.
  After a change, a window that is shown is told that its activation changed, and then its real state, as
  [Electron](https://github.com/electron/electron/blob/main/shell/browser/ui/win/electron_desktop_window_tree_host_win.cc)
  does. Windows 11 draws the new frame by itself and is told nothing.

The application calls no function that Windows exports by number alone.

**Page.** The webview serves the folder `wwwroot`, beside the executable, under the host name `bravo.invalid`,
reserved for names that are no site ([RFC 6761](https://www.rfc-editor.org/rfc/rfc6761.html#section-6.4)). No
other origin can read those files. The window opens `https://bravo.invalid/index.html`: the origin is the same
at every start, so what the page stores is found again. The engine keeps its data in the folder `WebView2` of
the data folder of the build ([Startup](startup.md)), and is given no browser argument.
The folder that 1.0.x used, `Bravo\.temp`, is neither read nor written, nor removed after an upgrade (backlog
B-35).

**Connection.** Before any script of a document runs, the webview runs a script that defines the object
`window.bravoHost`, with the members `address` and `token`: where the API listens, with no slash at its end,
and the token of the process. The script defines it only in a document whose origin is the origin of the page,
and the page can neither replace it nor change it. In the released application the token is never in an
address, a cookie or a file.

**What the page cannot do.** Everything that the page does not use is off: host objects, the messages of the
webview, the script dialogs and the context menus of the engine, the keys of a browser, the developer tools,
the status bar, the saving of passwords and of form data, pinch zoom, swipe navigation, and the reputation
check of addresses, which has nothing to check. A development build has the developer tools and the keys of a
browser.

- A navigation outside the origin of the page is cancelled, in the window and in a frame; a frame may hold a
  blank document.
- No window opens, and no download starts. A link of the page, and the file of an update, open in the default
  browser of the user, through the host ([updates](updates.md)).
- A permission that a document of another origin asks for is denied. For the origin of the page, reading the
  clipboard is allowed, and the permissions that the application knows and the page does not use are denied,
  each one by its name: microphone, camera, geolocation, notifications, other sensors, multiple automatic
  downloads, reading and writing files, autoplay, local fonts, MIDI system exclusive messages, window
  management, persistent storage, and the one that the engine calls unknown. None of these answers is kept
  in the profile.
- A permission that the application does not know, from a later runtime or from a later version of the
  WebView2 SDK, is not answered by the application: the engine asks the user, and keeps the answer. Denying it
  could stop the page after an update, with nothing that the user could do. A test fails when the SDK names
  a permission that the application has no answer for.

**Failures.** One type holds the rules, with no knowledge of the engine:

| Failure | What happens |
| --- | --- |
| The render process ends | The page is loaded again |
| The browser process ends | The control is disposed and a new one is created, when the engine says that the process has gone |
| Another process of the engine ends, or the page does not answer | Nothing: the engine recovers by itself, or the page may answer again |
| A fourth recovery within 5 minutes | A message, and exit code 4 |
| `wwwroot\index.html` is missing | A message, and exit code 2 |
| The runtime is not installed | A message with the address of the installer of the Evergreen runtime, and exit code 1223 |
| The engine or its control cannot be created, also because the runtime is too old for what the application uses | A message with what the engine reported, and exit code 3 |

An exception of an asynchronous step that is not a failure of the engine leaves the message loop, and ends the
application like any other. Until backlog B-8 the messages are dialogs of their own.

**In a browser.** For development and tests the same page runs in an ordinary browser, against a development
build of the host. Only a development build knows five more arguments of the command line; to a release
build they are arguments that it does not know, and so errors of the request.

| Argument | What the host does |
| --- | --- |
| `--dev-origin=<origin>` | Allows one more origin to call the API: the one of the development server |
| `--dev-connection-file=<path>` | Once it has started, writes the address and the token of the API to the file, a JSON object with the members `address` and `token`; deletes the file when it stops |
| `--dev-update-feed=<address>` | Reads the update feed at the address, an absolute `http` or `https` one: without it a development build reads none ([updates](updates.md)) |
| `--dev-data-folder=<path>` | Keeps the data of the user in the folder, the settings and the profile of the webview, in place of `Bravo.Development` under the local application data: a test starts on data of its own |
| `--dev-policy-key=<key>` | Reads its policies from the key under `HKEY_CURRENT_USER`, such as `Software\Bravo.Tests\Policies`: without it a development build reads none ([policies](policies.md)) |

A development server serves the development bundle and the static files of the page, on the loopback address
only, rebuilds the bundle at every change, and gives the page the same object before any other script, with
the content of that file. The limit is plain: there the token is in a file and in a page that the server gives
to whoever asks for it, so whoever on the machine can read that file or reach that server can call the API of
that development host. A start that finds a development build already running is handed over to it like any
other, and what it asked for the browser has no effect.

**Local Network Access.** The page and the API have different origins, so the checks that Chromium makes on a
page that calls the loopback address apply to it. WebView2 does not make them today
([announcement 126](https://github.com/MicrosoftEdge/WebView2Announcements/issues/126)), and the application
passes no flag. What happens when they are made was measured on a development build, with version 1.0.4314.35
of the SDK and runtime 157.0.4341.46, the checks turned on by setting the environment variable
`WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS` to
`--enable-features=LocalNetworkAccessChecks,msWebViewAllowLocalNetworkAccessChecks`:

- The permission of the page reads `prompt`, and the engine raises no permission request to the application.
  The first request of the page waits, and nothing reaches the host.
- The engine asks the user with a prompt of its own: "bravo.invalid wants to: Access other apps and services
  on this device", with the buttons Block and Allow. Until the user answers, the window shows its background
  and no page.
- After Allow the page starts. At the next start with the same flags and the same data folder there is no
  prompt and the page starts: the answer is kept in the profile.
- With `msWebViewAllowLocalNetworkAccessChecks` alone the result depended on the data folder. On a new one the
  request waited and the prompt showed. On the one that the build had already used there was no prompt and
  the page called the API, while the permission still read `prompt`. The reason is not known.
- A probe outside the application, on a new data folder at every run, gave the same result with both flags and
  with the second one alone, and no effect with `LocalNetworkAccessChecks` alone.

What follows from this is backlog B-40.

## In the code

The main window shows the user interface in WebView2
([ADR 0015](../decisions/0015-page-under-a-reserved-host-name-api-connection-in-an-object.md));
what the page gets and what it cannot do is described above. Only
`src/Bravo.Desktop/WebView/` uses the types of the engine:

| Part | File |
| --- | --- |
| The host name and the origin of the page, and the check that an address belongs to it | `PageAddress.cs` |
| The script that gives the page `window.bravoHost`, the address and the token of the API | `PageConnectionScript.cs` |
| The control: its environment, its settings, what it cancels, its recoveries | `WebViewHost.cs` |
| The answer to a permission that a document asks for | `WebViewPermissionRule.cs` |
| The rules for the failures and their limits, with no type of the engine | `WebViewFailurePolicy.cs`, `WebViewFailure.cs`, `WebViewRecovery.cs`, `WebViewDecision.cs` |
| What the user reads when a failure ends the application | `WebViewFailureMessage.cs`, `WebViewFailureDialog.cs` |
| The folders, and whether the developer tools are on: decided by the application, not by the webview | `WebViewOptions.cs`, `WebViewServiceCollectionExtensions.cs` |

- The name of the origin is in `PageAddress` and nowhere else: `AddWebView` adds it to the origins that the
  API allows.
- Everything of the control runs on the thread of the window. A step that nobody awaits goes through
  `WebViewHost.Observe`: a failure of the engine becomes a decision of the rules, and any other exception
  leaves the message loop and ends the application. Nothing calls the engine, or shows a dialog, from inside
  an event of the engine: it is posted to the window.
- A setting that the page does not need stays off. Turning one on is a change of
  this document in the same commit.
- The theme of the user is applied by `MainWindow.ApplyTheme`, on the thread of the window: at the start,
  when `IUserSettingsStore.Changed` says that the settings were replaced, and when Windows says that a
  setting of the system changed. `ISystemAppMode` says whether Windows asks applications to be dark, read
  from the registry and not from `Application.SystemColorMode`, which says light before Windows 11.
  `WindowColors` decides the colors, `ColorModeInterop` asks Windows for the frame, and
  `WebViewHost.ApplyTheme` sets the color scheme of the profile and the background of the control. The tests
  prove the color scheme with the real engine; the frame is checked by hand, and on Windows 10 apart: there
  `MainWindow.ApplyFrame` also asks for the frame to be drawn again (`ColorModeInterop.RedrawFrame`), which
  no test and no Windows 11 machine runs.
- The page is `wwwroot` beside the executable. The .NET build never runs npm: a development build copies
  `web/wwwroot` as it is, so the window shows the user interface only after `npm run build:UI` in `web/` and
  a build of `Bravo.Desktop`; without the bundle the window stays empty. A release build copies nothing:
  the build of the installer lays the folder out, with the release bundle ([Installer](installer.md)).
- `WebViewHostTests` uses the real engine: a window outside every screen, on a thread with a message loop, on
  a page of the test that reports through the title of its document. It needs the WebView2 runtime, which
  Windows 11 and the Windows runner of continuous integration have. A test waits for an event of the engine,
  never for a time.
- The WebView2 package adds a reference to its WPF control in every project: `Directory.Build.targets` removes
  it, or the build fails with MSB3277.
