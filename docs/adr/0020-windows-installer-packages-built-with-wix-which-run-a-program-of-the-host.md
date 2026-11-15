# 0020. Windows Installer packages built with WiX, which run a program of the host

- Status: Accepted
- Date: 2026-11-15

## Context

1.0.x is installed by Windows Installer packages built with WiX 3.11, per machine and per user under one
UpgradeCode ([ADR 0002](0002-shared-msi-upgrade-code.md)), each in two variants, running a .NET Framework
library. 1.1.0 must install over 1.0.14 with no manual uninstall and keep the settings
([specification](../spec.md#guarantees-for-users-of-10x)); removing 1.0.x outside an upgrade deletes the
data folder.

## Decision

- **Windows Installer packages built with WiX, with the UpgradeCode of 1.0.x**: only a major upgrade
  replaces 1.0.x and keeps the data, comparing the first three fields of a
  [version](https://learn.microsoft.com/windows/win32/msi/productversion).
- **Two packages, per machine and per user**: Windows Installer "will not install major upgrades across
  installation context" ([major upgrades](https://learn.microsoft.com/windows/win32/msi/major-upgrades)).
- **Two variants of each**: one carries the .NET runtime, one uses the installed one
  ([deployment](https://learn.microsoft.com/dotnet/core/deploying/)). Neither installs the WebView2 runtime,
  which updates itself
  ([Evergreen](https://learn.microsoft.com/microsoft-edge/webview2/concepts/evergreen-vs-fixed-version)).
- **WiX 5.0.2** ([documentation](https://docs.firegiant.com/wix/)), with no unneeded extension and no
  custom action library: a later version costs little.
- **A package runs a program of the host**, `Bravo.Setup.exe`: a command line, no window, no .NET runtime.
  It writes and removes the registration as an
  [external tool](https://learn.microsoft.com/power-bi/transform-model/desktop-external-tools-register) of
  Power BI Desktop and reports installations and removals. A fifth project:
  [ADR 0011](0011-host-in-four-projects-with-windows-at-the-edge.md) is amended.
- **The telemetry of the installer stays**: two events, two choices in the dialogs. Sending is backlog S-06.
- **Enterprise deployment is a criterion of every choice**: Intune
  ([line-of-business apps](https://learn.microsoft.com/intune/intune-service/apps/lob-apps-windows)),
  Configuration Manager, Group Policy, silent installation. Every name that a deployment relies on stays.
- **A running application closes when Restart Manager asks**
  ([Windows Installer](https://learn.microsoft.com/windows/win32/msi/using-windows-installer-with-restart-manager),
  [guidelines](https://learn.microsoft.com/windows/win32/rstmgr/guidelines-for-applications)).

## Rejected alternatives

- Inno Setup, Velopack, MSIX: none upgrades an installed Windows Installer package; MSIX and Velopack
  install per user; an executable is no line-of-business app of Intune and no Group Policy deployment.
- A later WiX: from version 6 it asks for a [maintenance fee](https://docs.firegiant.com/wix/osmf/).
- A custom action library on .NET Framework, as 1.0.x.
- `Bravo.exe` as that program: it does not start without its runtime, and its startup would take one more
  branch.
- The Microsoft Store: not part of 1.1.0.

## Consequences

- Only the per-machine package registers the external tool: its folder needs administrator rights.
- An upgrade asks for the package of 1.0.x where Windows no longer has its cached copy
  ([issue 705](https://github.com/sql-bi/Bravo/issues/705)).
- WiX 5 is out of community support ([WiX](https://wixtoolset.org/)).
