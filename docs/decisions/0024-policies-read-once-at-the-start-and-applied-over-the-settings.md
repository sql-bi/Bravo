# 0024. Policies read once at the start and applied over the settings

- Status: Accepted
- Date: 2026-11-20

## Context

Administrators deploy the policies of 1.0.x as registry values under `Software\Policies\SQLBI\Bravo\OptionSettings`,
with the templates of `policies/`; the fourth guarantee ([specification](../spec.md#guarantees-for-users-of-10x))
keeps their effect. A
[policy-aware application](https://learn.microsoft.com/windows/apps/get-started/line-of-business/policy-aware-settings)
puts a policy over the preference, shows the effective value, and never copies a policy into the user's settings.

## Decision

- **The values of 1.0.x**: five DWORD, 1 or 0, and `CustomTemplatesOrganizationRepositoryPath`, a string not
  expanded. Each is read in `HKEY_LOCAL_MACHINE`, then in `HKEY_CURRENT_USER`; a value of another kind or number
  is absent in that hive. `UpdateChannel` is not read ([ADR 0001](0001-releases-without-update-channels.md)).
- **Read once**, after the arbitration: a change applies at the next start, as in 1.0.x.
- **A key that cannot be read stops the start**, with a message and exit code 5. This supersedes
  [ADR 0005](0005-policy-failures-stop-startup.md), whose failure ended in the crash dialog.
- **Over the settings, never in them**: `/options` is unchanged. `GET /policies` returns the policies in force,
  and the page locks their options as 1.0.x does.
- **The update check turned off** makes no request: `GET /releases/latest` answers the error
  `UpdateCheckDisabled`. Turned on, the check at the start runs whatever the setting.
- **A policy whose feature is not built** is read and returned.
- **A development build** reads no policy unless given a key under `HKEY_CURRENT_USER`: `--dev-policy-key=<key>`.
- **The templates**, revision 1.2, are installed in `policies` by every package and archive. `UpdateChannel`
  returns, marked as no longer read, so that an administrator can set it to Not Configured. Their supported
  versions name Bravo, not Windows.

## Rejected alternatives

- Policies followed while Bravo runs: a notification thread, an event and a page reload, for a Group Policy
  refresh every 90 minutes.
- Effective values in `/options`, a change of a locked option refused: the API loses the user's choice.
- An unreadable key taken as not configured: a restriction would vanish silently.
- The templates as a release asset: the installation and the repository hold them.

## Consequences

- An administrator who imported an earlier revision in Intune deletes it before importing 1.2.
- `BuiltInTemplatesEnabled` and `CustomTemplatesOrganizationRepositoryPath` enter the templates with their
  features (backlog B-16, B-17).
- The key keeps `OptionSettings`: a shorter one would make administrators deploy again.
- Tests read keys of their own; the real key is proven only on a runner.
