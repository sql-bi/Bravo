# Code conventions

Rules that apply to code in this repository, beyond what `.editorconfig` enforces automatically.

## Files

New or rewritten `.cs` files are UTF-8 **with** BOM and use CRLF. This is not configured anywhere: it is what
every existing file under `src/` is, and `.editorconfig` sets `charset` only for project and resource files.
A tool that defaults to UTF-8 without BOM, or to LF, turns a small edit into a whole-file diff.

## Naming and design

Follow the Microsoft .NET naming and design guidelines. Where a pattern already in the repository diverges
from them, the guidelines win: a local precedent is not an argument for repeating a mistake.

## Types

Assign primary constructor parameters to explicit `private readonly` fields. Do not reference a parameter
directly from the body of the type: an explicit field declares the dependency, its lifetime and its
mutability at the top of the type, where they can be read.

## Comments

Comment only what the code cannot express: origin, constraints, or rationale. Never restate the code. Document
the member, not its callers. Usage rules belong to the caller or to docs/. Keep summaries short and semantic
and exclude implementation details; put them in inline comments when they matter.

## Testability

Design for testability from the start:

- keep classes small and focused on one responsibility;
- inject dependencies through DI instead of relying on static state;
- pass external state — process, registry, filesystem, clock, network — through dependencies that tests can control.

A type that can only be tested by running the application is not finished.
