# 0013. Startup in a fixed order, one instance by mutex and pipe

- Status: Accepted
- Date: 2026-11-12

## Context

Kept from `main` ([ADR 0003](0003-fatal-exceptions-end-the-process.md),
[ADR 0004](0004-bootstrap-services-outside-the-container.md),
[design](../design/architecture.md#single-instance-contract)): the order of `Main`, exceptions that end the
process, objects created before the host, a second process that creates nothing, one instance for each user
and session.

## Decision

- `Main` is synchronous, on an STA thread, with an exit code. Order: Windows Forms configuration;
  last-chance exception handling, behind an interface until backlog S-05; instance arbitration, before disk
  and registry; host; window and message loop; host stopped and disposed; ownership released.
- The host follows the
  [pattern for Windows Forms](https://learn.microsoft.com/dotnet/desktop/winforms/advanced/how-to-use-host-builder).
  Closing the window stops the host; a host that stops on its own closes the window. `ShutdownTimeout` is 5
  seconds, the time Windows gives an application
  [when a session ends](https://learn.microsoft.com/windows/win32/shutdown/wm-queryendsession). The shell
  replaces `ConsoleLifetime`, whose signals never reach a windowed process
  ([Generic Host](https://learn.microsoft.com/dotnet/core/extensions/generic-host)). Extension methods of
  `IServiceCollection` register the services.
- Ownership is a named mutex of the current user and session
  ([`NamedWaitHandleOptions`](https://learn.microsoft.com/dotnet/api/system.threading.namedwaithandleoptions));
  an abandoned one is acquired.
- Hand-over is a named pipe in the same scope
  ([`PipeOptions`](https://learn.microsoft.com/dotnet/api/system.io.pipes.pipeoptions)): a message with a
  version and a size limit, timeouts, an answer. New names: 1.0.x and 1.1.0 do not recognize each other. An
  owner that cannot create the pipe starts and logs it.
- A second process that cannot deliver, also because privilege levels differ
  ([issue 506](https://github.com/sql-bi/Bravo/issues/506)), tells the user and exits with a non-zero code:
  it never becomes an owner. It retries for 10 seconds: an owner may be shutting down.
- The second process calls
  [`AllowSetForegroundWindow`](https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-allowsetforegroundwindow)
  for the owner, which
  [activates](https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-setforegroundwindow) its
  window.
- Every activation, the first start included, is a request on one bounded, single-consumer queue.
- System.CommandLine 2.0 parses `--server`, `--database` and `--ppid`; errors travel with the request, never
  stopping the start.

References: `icsharpcode/ILSpy` (`ILSpy/AppEnv/SingleInstance.cs`), `ShareX/ShareX`
(`ShareX.HelpersLib/SingleInstanceManager.cs`), `microsoft/terminal`
(`src/cascadia/WindowsTerminal/WindowEmperor.cpp`).

## Rejected alternatives

- One pipe as lock and channel: owning the instance and reaching it would be one question.
- The Visual Basic application model (`dotnet/winforms`, `SingleInstanceHelpers.vb`): it owns the message
  loop, activates only the form, gives up after 2.5 seconds.
- `AppInstance` of the Windows App SDK: an unpackaged application
  [must deploy its runtime](https://learn.microsoft.com/windows/apps/windows-app-sdk/deploy-unpackaged-apps).
- An instance for each privilege level: two writers of the same user data.

## Consequences

- An upgrade can meet a running 1.0.x instance that 1.1.0 does not see (backlog D-01).
