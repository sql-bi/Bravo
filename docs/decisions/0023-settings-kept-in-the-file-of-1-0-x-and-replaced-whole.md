# 0023. Settings kept in the file of 1.0.x and replaced whole

- Status: Accepted
- Date: 2026-11-19

## Context

1.0.x keeps the settings of the user in `%LOCALAPPDATA%\Bravo\usersettings.json`, for an installed and a
portable copy alike: JSON with the enums as numbers. A portable 1.0.x, or 1.0.x after a downgrade, reads the
same file. On this branch the settings live in memory, and the third guarantee
([specification](../spec.md#guarantees-for-users-of-10x)) asks that they survive the upgrade.

## Decision

- **The file of 1.0.x**: its folder, name and JSON names, the enums as numbers; a development build has its
  own folder. Members that the application does not know, such as `updateChannel`, are written back
  ([`JsonExtensionData`](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/handle-overflow)).
  No member gives a version until 1.0.x can no longer read the file.
- **Read once, at the start**, into an immutable value. A member that is missing, of another type or
  out of range takes its default. Without a file, the defaults and, for telemetry, the registry value
  `applicationTelemetryEnabled`, as 1.0.x reads it. A file that cannot be opened or is not JSON counts as no
  file, with a notice on the page; the next save replaces it.
- **Replaced whole**: written to a temporary file in the same folder, flushed, moved over the old one, retried
  briefly while another program holds it
  ([Microsoft](https://learn.microsoft.com/windows/win32/fileio/deprecation-of-txf), VS Code). The value
  changes only after the file. A save that fails is the error `SettingsSaveFailed`, which the page shows.
- **Two writers**, a 1.0.x and a 1.1.x: the last one wins.
- **Around the settings**: the window is not kept, as in 1.0.x; `applicationTitleVersionHidden` hides the
  version in the title again; a development build takes `--dev-data-folder` for the tests.

## Rejected alternatives

- A new file with a version, migrated once: two files drift apart, and a downgrade loses the changes made
  since.
- The same file in a new format: 1.0.x could no longer read it.
- Configuration with `reloadOnChange`: it cannot write
  ([dotnet/runtime#65593](https://github.com/dotnet/runtime/issues/65593)).
- A lock against a second writer: no reference application locks its settings file.

## Consequences

- The enums become names in the file only when no 1.0.x reads it.
- The file holds the choices of the user alone: a policy (backlog B-5) applies over them.
- A removal deletes the data folder of who removes, per machine too, as with 1.0.14; the webview profile of
  1.0.x, `.temp`, stays until then.
