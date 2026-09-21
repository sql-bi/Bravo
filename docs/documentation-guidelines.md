# Documentation guidelines

Rules for documents under `docs/`. The reader is an LLM working on this repository. End-user documentation has a separate audience.

## Content and scope

Document constraints, invariants, verified reasons for decisions and facts about external integrations or published artifacts. Include interactions across components when they explain ownership, ordering, thread requirements or failure boundaries that would otherwise require reading several files.

Do not translate individual methods into prose or maintain property lists, file walkthroughs or class catalogs. Link to the implementation instead. A useful runtime description explains the contract between components and the consequence of breaking it.

Keep one coherent topic per document. Update an existing document before creating another; create a new document only for a distinct task or concept. Keep each rule in one authoritative location and link to it elsewhere.

## Facts, rules and exceptions

| Statement | Required form |
| --- | --- |
| Current behavior | Describe what the checked-in implementation does; link to the relevant source, configuration or test |
| Required rule | State the action or invariant, its scope and the consequence of violating it |
| Recommendation | Identify it as a recommendation and state when it applies |
| Exception or limitation | Place it beside the affected rule or behavior; name the condition explicitly |

Do not present a desired behavior as an implemented guarantee. A rule for new changes can differ from existing behavior; label that distinction. When evidence is insufficient, omit the claim or identify the uncertainty instead of inferring a guarantee from a name or comment.

## Retrieval and navigation

- Keep the selective-reading index in [AGENTS.md](../AGENTS.md). Add a document there with an explicit reading condition; do not duplicate the index under `docs/`.
- Use descriptive file names and headings based on repository concepts or tasks.
- Make each section understandable independently: name the component and keep conditions such as Debug/Release, host/frontend or public/internal build beside the claim, including in tables.
- Link to repository-relative paths and stable heading anchors. Use symbols to identify relevant members; avoid source line numbers that drift.
- Link external claims to their authoritative source. Do not copy external reference material into the repository.

## Form

- English, short sections and plain declarative prose. Use direct instructions for required actions.
- Tables for mappings; numbered steps only when order matters across components or in a repository-specific procedure.
- Include prerequisites, working directory and success criteria when a procedure needs them to be executable.
- No filler, first person, intensifiers, reassurance, metaphors or rhetorical questions. Code-comment examples are in [code conventions](design/code-conventions.md#comments).
- Attach rationale to the rule it explains. Avoid unrelated background or generic programming advice.

## Current-state documents

The design documents describe current behavior and applicable rules. Do not use them as changelogs, plans or tutorials for ordinary tasks. Keep dates, change histories and future work out of these documents; current limitations belong beside the behavior they constrain.

## Maintenance

Update documentation in the same change that alters the behavior it describes. Remove claims that no longer hold rather than marking them obsolete. Preserve still-applicable constraints when implementation details change.

Before finishing, verify claims against the affected sources, check relative links and heading anchors, and confirm that the index routes the relevant tasks to the document. Distinguish commands executed from commands verified only by inspection. A documentation-only change does not require running unrelated application tests.
