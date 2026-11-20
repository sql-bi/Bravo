# Policies

The policies that administrators deployed for 1.0.x keep their effect: the application reads them once, when it
starts ([ADR 0024](../decisions/0024-policies-read-once-at-the-start-and-applied-over-the-settings.md)).

**The keys.** `Software\Policies\SQLBI\Bravo\OptionSettings`, under `HKEY_LOCAL_MACHINE`, then under
`HKEY_CURRENT_USER`. Each value is read on its own, as 1.0.x reads it: the computer gives the values that it has,
the user the others.

| Value | Kind | What it sets |
| --- | --- | --- |
| `TelemetryEnabled` | DWORD | Telemetry |
| `UpdateCheckEnabled` | DWORD | The update check ([updates](updates.md)) |
| `UseSystemBrowserForAuthentication` | DWORD | The sign-in in the browser of the system |
| `BuiltInTemplatesEnabled` | DWORD | The templates of Manage Dates that come with the application |
| `CustomTemplatesEnabled` | DWORD | The templates of the user |
| `CustomTemplatesOrganizationRepositoryPath` | String | The folder of the templates of the organization |

- A DWORD is on with 1 and off with 0. Another number, or a value of another kind, is no policy in that key: the
  value of the other key applies, else the choice of the user. A string is read as it is written, empty text
  included; an expandable string is no policy.
- `UpdateChannel`, which the templates of 1.0.x write, is not read: there is one channel
  ([ADR 0001](../decisions/0001-releases-without-update-channels.md)).
- A key that does not exist sets nothing. A key that exists and cannot be read stops the start: a message names
  the key and asks the user to turn to the administrator, and the application ends with exit code 5
  ([startup](startup.md)). Starting without the policies would remove what an administrator set.
- A policy that changes while the application runs applies at its next start.

**A development build** reads no policy. Started with `--dev-policy-key=<key>`, it reads the values of a key
under `HKEY_CURRENT_USER`, such as `Software\Bravo.Tests\Policies`, as its policies, with the same rules. To a
release build the argument is one that it does not know, and so an error of the request
([window and webview](window-and-webview.md)).

## In the code

| Part | Path |
| --- | --- |
| The policies, a value that is never changed | `src/Bravo.Core/Policies/` |
| The reader of the registry, the keys of the application, the error of a key that cannot be read | `src/Bravo.Platform.Windows/Policies/` |
| Which keys a build reads, the message of a key that cannot be read, the registration | `src/Bravo.Desktop/Policies/` |
| The reading, once, before the host is built | `src/Bravo.Desktop/Program.cs` |

- `RegistryPolicyReader` is the one code that reads the policies, and `Main` calls it once, after the
  arbitration: the host gets `ApplicationPolicies` as an instance. A use case that honors a policy takes it as a
  dependency.
- A new policy is a member of `ApplicationPolicies`, read by `RegistryPolicyReader`. A value of 1.0.x keeps its
  name, its key and its values.
- No test reads or writes the policies of the machine that runs it. A test of the reader writes in a key of its
  own under `HKEY_CURRENT_USER\Software\Bravo.Tests`, which it removes. One test writes the keys of the
  application, and runs only on a runner of GitHub Actions, which is thrown away after the run.
- `BravoProcessTests` starts the application with a key of the test that cannot be read.
