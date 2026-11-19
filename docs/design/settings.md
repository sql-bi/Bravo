# Settings of the user

The settings of the user are kept in the file of 1.0.x, read once and replaced whole
([ADR 0023](../decisions/0023-settings-kept-in-the-file-of-1-0-x-and-replaced-whole.md)).

**The file.** `usersettings.json` in the data folder of the build ([Startup](startup.md)): for an installed or a
portable copy `%LOCALAPPDATA%\Bravo`, the folder of 1.0.x; a development build has a folder of its own. The file
is JSON as 1.0.x writes it: its names, the enums as numbers, indented, UTF-8 without a byte order mark. A 1.0.x
that starts after 1.1.0, portable or after a downgrade, reads the same file.

| Member | Setting | Default |
| --- | --- | --- |
| `telemetryEnabled` | Telemetry, `true` or `false` | `true`, or the choice of the installation |
| `diagnosticLevel` | `0` None, `1` Basic, `2` Verbose | `0` |
| `updateCheckEnabled` | The update check ([updates](updates.md)) | `true` |
| `theme` | `0` Auto, `1` Light, `2` Dark | `0` |
| `proxy` | An object: `type` (`0` None, `1` System, `2` Custom), `useDefaultCredentials`, `address`, `bypassOnLocal`, `bypassList` | `1`, `true`, none, `true`, none; 1.0.x writes `null` until the user changes it |
| `useSystemBrowserForAuthentication` | The sign-in in the browser of the system | `false` |
| `customTemplatesEnabled` | The templates of the user | `true` |
| `customOptions` | The options that only the user interface reads, any JSON value, kept as they are | none |

**Reading.** The file is read once, when the host starts.

- A member that is missing, of another type or out of range is the default of its setting; a proxy whose
  address or bypass list the API would refuse ([API](api.md)) is the default proxy, as 1.0.x reads it. Blank
  text is no value. Comments and commas after the last member are allowed, and the names are case-sensitive.
- Without a file, the settings are the defaults, and telemetry is the choice of the installation: the string
  value `applicationTelemetryEnabled` under `Software\SQLBI\Bravo` of the key of the installation,
  `HKEY_LOCAL_MACHINE` for the package per machine and `HKEY_CURRENT_USER` for the one per user, which the
  packages write ([Installer](installer.md)). A number other than zero is yes and any other text no; a value
  of another kind, a copy that is not installed and a value that cannot be read leave the default. 1.0.x
  reads it in the same way.
- A file that cannot be opened, or that is not a JSON object, counts as no file and is logged, and the page
  tells the user in a notification, through the event `SettingsReadFailed` ([API](api.md)). Nothing renames
  it: the next save replaces it.

**Saving.** Every change of the options replaces the file whole.

- The text goes to `usersettings.json.tmp` in the same folder, is flushed to the disk and moved over the file:
  a reader finds the old settings or the new ones. While another program holds the file, such as an antivirus,
  the save tries again, 5 attempts 100 milliseconds apart.
- The settings change only after the file. A save that fails leaves the settings as they were and is the error
  `SettingsSaveFailed`, which the page shows as a notification.
- The members that the application does not know are written back, after the ones that it knows, at the top
  and in `proxy`: `updateChannel` of 1.0.x, and what a later version adds.
- A 1.0.x and a 1.1.x that run at once both write the file, each one all of it: the last one wins.

The window is not kept, as in 1.0.x. A policy (backlog B-5) applies over the settings and is not written in
the file.

## In the code

| Part | Path |
| --- | --- |
| The settings, a value that is never changed, and the interface of their store | `src/Bravo.Core/Settings/` |
| The file: its members and how each one is read, the store, the replacement of a file | `src/Bravo.Platform.Windows/Settings/` |
| The choice of telemetry of the installation, read from the registry | `src/Bravo.Platform.Windows/Updates/RegistryInstallationReader.cs` |
| The store, registered with the file in the data folder, and the reading at the start that tells the page of a file that cannot be read | `src/Bravo.Desktop/Settings/` |
| The notification of a file that cannot be read | `settingsReadFailed` in `web/controllers/host-events.ts` |

- `UserSettingsDocument` and `ProxySettingsDocument` hold each member of the file as it was read, a
  `JsonElement`, and the members that the application does not know in `[JsonExtensionData]`.
  `DocumentMember` reads a member as the value of a setting. The store keeps the last document, read or
  written, so that a save writes back what it did not know.
- An enum stays a number in the file as long as a 1.0.x can read it: 1.0.x fails the whole file on a name and
  starts with the defaults.
- A new setting is a member of `UserSettings` and of the document, with the name that the file will keep for
  good. 1.0.x drops the members that it does not know when it saves: the new setting is lost after a 1.0.x
  saved the file, and comes back as its default.
- `FileReplacer` is the only code that writes the file; its attempts and its wait are properties, and a test
  sets them with a `FakeTimeProvider`.
- The tests read and write only in a folder of their own: the tests of the host build it on a temporary data
  folder. `Release1014` in the tests holds two files as 1.0.14 writes them and the reader of 1.0.14, with its
  types and options: the proof that 1.0.14 reads what 1.1.0 writes.
