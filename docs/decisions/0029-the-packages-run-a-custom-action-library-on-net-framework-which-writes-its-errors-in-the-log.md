# 0029. The packages run a custom action library on .NET Framework, which writes its errors in the log

- Status: Accepted
- Date: 2026-11-27

## Context

The packages run `Bravo.Setup.exe`
([ADR 0020](0020-windows-installer-packages-built-with-wix-which-run-a-program-of-the-host.md)), whose error
reaches the log of an installation as an exit code alone: a program has no handle to the installation
([error messages](https://learn.microsoft.com/windows/win32/msi/returning-error-messages-from-custom-actions)).
Microsoft asks that a custom action write its errors in the log
([best practices](https://learn.microsoft.com/windows/win32/msi/windows-installer-best-practices)).

## Decision

- **A custom action library**, `Bravo.Setup`, built with the DTF of WiX 5.0.2, in place of the program. An
  action reads `CustomActionData` and writes the error of a file or a folder with `Session.Log`; DTF writes an
  exception that nothing expected. `Return="ignore"` stays.
- **In a process of its own**, a `rundll32` that ends with the action
  ([SfxCA](https://github.com/wixtoolset/wix/blob/v5.0.2/src/dtf/SfxCA/SfxCA.cpp)), from the `Binary` table.
- **.NET Framework 4.7.2, as 1.0.x and as Power BI Desktop**
  ([requirements](https://learn.microsoft.com/power-bi/fundamentals/desktop-get-the-desktop#minimum-requirements)),
  with no `sku` in `CustomAction.config`: an older 4.x runs the library instead of refusing it.
- **C# 7.3**, the version of .NET Framework
  ([versioning](https://learn.microsoft.com/dotnet/csharp/language-reference/language-versioning)). Nullable
  reference types, which need C# 8, are off in this project alone.
- **No package but DTF**: JSON with `DataContractJsonSerializer`; the send with `WebRequest`, through the proxy
  of the system with the Windows credentials, adding TLS 1.2 to the protocols of .NET Framework 4.0 that an
  action gets ([TLS](https://learn.microsoft.com/dotnet/framework/network-programming/tls)).
- **The clock is `DateTimeOffset.UtcNow`, read in one place** under a `#pragma` with its reason: .NET Framework
  has no `TimeProvider`. It is the one exception to [ADR 0009](0009-rules-enforced-by-the-build.md).

## Rejected alternatives

- A native library compiled with Native AOT: Windows Installer may unload it, which .NET does not
  [support](https://learn.microsoft.com/dotnet/core/deploying/native-aot/libraries).
- `WixQuietExec`: an extension, and the program on disk at the removal too.
- A library that starts the program and logs what it writes: two technologies and two processes.
- .NET Framework 4.6.2: the tests need 4.7.2.
- `System.Text.Json`, `Microsoft.Bcl.TimeProvider`: binding redirects that only `CustomAction.config` could
  hold.

## Consequences

- The build needs no C++ build tools.
- A second target framework, with tests on .NET Framework 4.7.2.
- Where .NET Framework is older than 4.7.2, an action that calls a later API fails, and the log says why.
- The send of an event from an installation is checked by hand.
- Compilation ahead of time, the reason of [ADR 0028](0028-telemetry-as-in-1-0-x-on-the-classic-sdk.md) for an
  installer sender of its own, no longer holds.
