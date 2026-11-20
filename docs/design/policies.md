# Policies

The policies that administrators deployed for 1.0.x keep their effect: the application reads them once, when it
starts, and applies them over the settings of the user, which keep the choices of the user
([ADR 0024](../decisions/0024-policies-read-once-at-the-start-and-applied-over-the-settings.md)).

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

**Over the settings.** A policy is never written in the file of the settings ([settings](settings.md)), and
`/options` does not know it: the options are the choices of the user, which come back when a policy is removed.

- `GET /policies` returns the policies in force ([API](api.md)). A member that is absent is a policy that is not
  set.
- The page reads the policies at its start, with the options. The option of a policy shows the value of the
  policy, cannot be changed, and says that the organization manages it.
- With `UpdateCheckEnabled` off the host makes no request to the update feed: `GET /releases/latest` is the
  error `UpdateCheckDisabled`, and the page neither checks at its start nor offers the check in its options. On,
  the page checks at its start whatever the choice of the user.
- A policy whose feature has not moved yet is read and returned all the same: telemetry (backlog B-9), the
  sign-in (backlog B-12), the templates (backlog B-16, B-17).

**The templates.** `policies/` holds the administrative templates, revision 1.2: `bravo.admx` and `sqlbi.admx`,
with their texts in `en-US/`. Every package and every archive installs them in the folder `policies` of the
application ([installer](installer.md)).

- They have the five policies of the templates of 1.0.x, with the same key, the same names and the same
  values: a policy that an administrator deployed from the templates of 1.0.x is the one of these.
- `UpdateChannel` stays, named obsolete, so that an administrator can set it to Not Configured and remove the
  value that it wrote.
- Their supported versions name Bravo for Power BI, not a version of Windows.
- `BuiltInTemplatesEnabled` and `CustomTemplatesOrganizationRepositoryPath` have no template, as in 1.0.x: they
  enter the templates with their features.
- An administrator who imported an earlier revision in Intune deletes it before importing 1.2.

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
| The route, and the policies as they travel | `src/Bravo.Api/Policies/` |
| The update check that a policy turns off | `src/Bravo.Core/Updates/UpdateCheck.cs` |
| The page: the policies of the options | `web/main.ts`, `optionPolicy` in `web/controllers/options.ts` |
| The templates | `policies/` |

- `RegistryPolicyReader` is the one code that reads the policies, and `Main` calls it once, after the
  arbitration: the host gets `ApplicationPolicies` as an instance. A use case that honors a policy takes it as a
  dependency.
- The page finds the policy of an option by the name of the option: a policy has the name of its option in
  `/options`, which `PolicyDetails` keeps.
- A new policy is a member of `ApplicationPolicies` and of `PolicyDetails`, read by `RegistryPolicyReader`, with
  a policy in `bravo.admx` and its texts in `bravo.adml`, whose revisions both go up. A value of 1.0.x keeps its
  name, its key and its values.
- No test reads or writes the policies of the machine that runs it. A test of the reader writes in a key of its
  own under `HKEY_CURRENT_USER\Software\Bravo.Tests`, which it removes. `PolicyTemplateTests` reads each
  template, and the values that each policy writes, as the application reads them. One test writes the keys of
  the application, and runs only on a runner of GitHub Actions, which is thrown away after the run.
- The tests of the window give the application a key of the test (`policies` in `web/window-tests/bravo.ts`).
  `BravoProcessTests` starts the application with a key of the test that cannot be read, and the test of the
  packages the installed application with a key of the computer that cannot be read.
