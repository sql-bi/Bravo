# Code conventions

Rules that apply to code in this repository, beyond what `.editorconfig` enforces automatically.

## Files

New or rewritten `.cs` files are UTF-8 **with** BOM and use CRLF. A tool that defaults to UTF-8 without BOM,
or to LF, turns a small edit into a whole-file diff.

## Naming and design

Follow the Microsoft .NET naming and design guidelines. Where a pattern already in the repository diverges
from them, the guidelines win: a local precedent is not an argument for repeating a mistake.

## Comments

Comment only what the code cannot express: origin, constraints, or rationale. Never restate the code. Document
the member, not its callers. Usage rules belong to the caller or to docs/.

A `<summary>` is one sentence, third person, stating what the member is or does. Constraints and rationale go in
`<remarks>`. Implementation details go in inline comments, next to the code they explain.

Plain prose only, in comments and XML documentation alike: state the fact, do not perform it. A comment written
as a voice is copied in the same voice by the next reader, and the tone spreads faster than the content.

| Do not write | Write |
| --- | --- |
| "We safely allow the runtime to continue." | "The runtime continues." |
| "Catastrophic error while handling the exception." | "The handler failed while handling the exception." |
| "This ensures accurate crash dumps." | "The stack is intact at the throw site." |
| "There is nobody left to tell." | "The dialog is closed; the outcome is not reported." |

Banned: first person; narration of the next statement; intensifiers and reassurance such as "safely", "simply",
"properly", "gracefully", "robust", "catastrophic"; metaphors and personification; rhetorical questions;
exclamation marks.

## Logging

A type that logs through `[LoggerMessage]` is `partial`, and the generated log methods live in a companion file
named `<TypeName>.Log.cs` next to the main file; the main file contains only the calls. Log methods inline in
the main file bury the flow of the type under attributes and message strings.

Each log method has its own `EventId`, unique within the type, so that an event can be filtered in the Event
Log without parsing the message. The level follows the outcome: `Warning` when the application continues,
`Error` when the operation visible to the user failed. Example: [BravoApplicationInstance.Log.cs](../../src/Host/BravoApplicationInstance.Log.cs).
