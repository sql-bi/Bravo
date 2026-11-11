# Bravo: specification

What Bravo is, what version 1.1.0 must be, and what has been decided so far. The reasons are in
[docs/adr/](adr/); the open work is in [backlog.md](backlog.md).

## The product

Bravo for Power BI is a Windows desktop tool for authors of Power BI models. It opens a model from Power BI
Desktop, from the Power BI service or from a VPAX file and offers:

| Feature | What it does |
| --- | --- |
| Analyze Model | Shows the size of tables and columns and what is not used; imports and exports VPAX files |
| Format DAX | Formats the measures of a model |
| Manage Dates | Creates date tables and time intelligence measures from templates |
| Export Data | Exports tables to CSV and Excel files |
| Template Development | Lets template authors create and test date templates with Visual Studio Code |

Around the features: sign-in to the Power BI service, options, a diagnostics pane, a notification when a new
version exists, and the registration as an external tool of Power BI Desktop.

## Version 1.1.0

Version 1.1.0 is a redesign, not a feature release ([ADR 0007](adr/0007-redesign-on-a-development-branch.md)).

| In scope | Out of scope |
| --- | --- |
| Repository layout, host, API, installer, CI/CD, release process | New features |
| Changes to the TypeScript user interface that the new API requires | A rewrite of the TypeScript user interface |
| | Manage Calendars; a cross-platform shell |

Fixed constraints: the host runs on .NET 10, the user interface is written in TypeScript.

## Guarantees for users of 1.0.x

Whatever a component decides, these four hold:

1. A 1.0.x installation is told about 1.1.0 through the update feed it reads today.
2. 1.1.0 installs over 1.0.14 without a manual uninstall.
3. The user's settings survive the upgrade, read as they are or migrated once.
4. The policies that administrators have already deployed keep their effect.

Everything else is open to redesign: the technology of the installer, the routes of the API, the format of
files, the name of the single-instance pipe, the arguments of the external tool registration.

## Rules for every component

- **The cycle.** A component is designed before it is written: requirements, exploration, options, decision,
  implementation, result. The decision is the maintainer's and becomes an ADR of at most 400 words.
- **Requirements come from `main`.** The code on `main` is the specification of today's behavior, including
  what was learned in the field. Read it with `git show main:<path>`; run it from a worktree outside this folder
  to compare results.
- **References.** An exploration looks at well-known, well-engineered projects and at the official
  documentation, and names them.
- **Isolated components.** Code that other tools also need, such as the discovery of the local Power BI Desktop
  instances and of their ports or the report name taken from a window title, is designed as an independent
  component: its own folder and namespace, its own tests, only the .NET base libraries, everything else received
  as a parameter, a public surface designed as if it were already a package. It is not a separate project or
  package yet.
- **Networks.** Bravo is used in enterprises by people who are not technicians. Every connection it makes must
  work at the first attempt behind proxy servers, firewalls and proxy auto-configuration scripts, with no
  complex configuration.

## Architecture decided so far

Nothing beyond ADR 0007. Each component adds its part here when its ADR is accepted.

## Open questions

Each is answered by the component named in brackets.

- Which technology builds the installer, and how many variants are released? [installer]
- Does a per-user installation register Bravo in Power BI Desktop? Today only the per-machine one does.
  [installer]
- Does the user interface keep calling the host over HTTP on loopback, or through the message channel of the
  webview? [host architecture]
- Is the crash reporter a mode of the main executable or an application of its own? [errors and crash
  reporting]
- Does Template Development keep its current routes for the Visual Studio Code extension, or does the
  extension change with the API? [features]
- How is the version computed and how are release notes and the changelog produced? [release]
