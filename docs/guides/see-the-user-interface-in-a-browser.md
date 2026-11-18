# How to see the user interface in a browser

To see the user interface in a browser, against a real host
([the design](../design/window-and-webview.md)):

1. In `web/`, `npm ci` once, then `npm run serve:host`. It builds `Bravo.Desktop` and starts it as
   `dotnet run --project ../src/Bravo.Desktop -c Release -- --dev-origin=http://127.0.0.1:8098
   --dev-connection-file=<web>/.dev/host.json` would. The window of Bravo opens, and the command prints
   "The host is ready". Closing the window ends the host.
2. In a second terminal, in `web/`, `npm run serve:UI`: webpack-dev-server, on `127.0.0.1` only.
3. Open `http://127.0.0.1:8098` in Chrome or Edge. The page is loaded again at every change of the sources.

- `web/dev.config.mjs` holds the port, the origin and the path of the file. `dev-host.mjs` is the command
  that starts the host. `dev-server.mjs` holds the options of the development server, which
  `webpack.config.js` gives to `webpack serve` and to no other command.
- The server gives `index.html` the object `window.bravoHost` before its first script, from the file that the
  host wrote, read at every request: after the host is started again, load the page again. Without the file
  the page is a 503 that says how to start the host. The page is given after the checks of the server on the
  host that a request names: a request for another host name gets a 403, not the token.
- A development build that already runs takes the start of `serve:host` like any other start, and writes no
  file: the command says so and ends with 1. Close the running window first. The installed application does
  not matter: it is another instance.
- `web/.dev/host.json` holds the token of that host while it runs. Git ignores the folder; the host deletes
  the file when it stops.
- The two arguments exist only in a development build
  ([Startup and single instance](../design/startup.md)).

The commands and the rules of the user interface: [web/AGENTS.md](../../web/AGENTS.md).
