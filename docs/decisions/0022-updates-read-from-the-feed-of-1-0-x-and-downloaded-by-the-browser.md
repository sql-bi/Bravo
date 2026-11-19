# 0022. Updates read from the feed of 1.0.x and downloaded by the browser

- Status: Accepted
- Date: 2026-11-19

## Context

An installed 1.0.x reads the update feed `currentversion.json`, compares its `version` with its own file
version and tells the user; the page downloads the file in a frame of the webview
([release](../design/release.md)). On this branch the check has no route, and the webview cancels the frame.
Nothing of the installer updates by itself: Windows Installer acts when it is run,
[UAC patching](https://learn.microsoft.com/windows/win32/msi/user-account-control--uac--patching) covers signed
patches only, the [`Update` element](https://docs.firegiant.com/wix/schema/wxs/update/) of WiX bundles only.

## Decision

- **The feed of 1.0.x**, `bravo-public/currentversion.json`, decoded as 1.0.x decodes it. Its four-number
  `version` is compared with the file version of the host: a preview learns of its release.
- **When**: as today, at the start of the page, in silence, and in the options. The beep of 1.0.x stays.
- **The file of the copy** is named from `download` as 1.0.x names it, by `installFolder` and the runtime of the
  build; without the registry value, the archive.
- **The browser downloads it**, through a route that opens in the default browser an address of a host that
  1.0.x trusts, for every link of the page.
- **A development build does not check**, as in
  [VS Code](https://github.com/microsoft/vscode/blob/main/src/vs/platform/update/electron-main/abstractUpdateService.ts),
  unless given a feed: `--dev-update-feed=<address>`.
- **One HTTP client of the host's factory**: backlog B-6 sets the proxy of every client in one place.
- **Turned off**: the setting skips the check at the start; the policy, once backlog B-5 reads it, stops every
  request.

## Rejected alternatives

- The application downloads, checks the signature, runs the package and is restarted by
  [Restart Manager](https://learn.microsoft.com/windows/win32/msi/using-windows-installer-with-restart-manager):
  a resource with progress, temporary files, and still an elevation prompt per machine.
- A silent update in the background: per machine it needs an elevated service, and administrators lose control.
- The GitHub API: [60 requests an hour](https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api)
  per address, which a whole company can share.
- A periodic check: it needs the last check kept, and Bravo runs briefly.

## Consequences

- [ADR 0001](0001-releases-without-update-channels.md) holds: one feed, stable releases only, no channel.
- The feed keeps its fields, and a release its six names, while installed 1.0.x copies exist.
- Today's feed is UTF-16 with a byte order mark: the host decodes text, never bytes.
- Behind a proxy that asks for credentials, the check fails until backlog B-6.
