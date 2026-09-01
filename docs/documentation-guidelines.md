# Documentation guidelines

Rules for writing and updating the documents under `docs/`. The reader is an LLM working on this repository,
not an end user.

## What a document contains

Write what the code cannot state:

- constraints and invariants, each with the consequence of violating it;
- the reason behind a choice, when the result alone does not explain it;
- facts that live outside the repository: external services, deployed clients, published artifacts.

Do not restate the code: no property or method lists, no file walkthroughs, no description of a control flow
that can be read in the source. Anything copied from the code drifts as soon as the code changes. Reference a
file by link when the reader needs it; do not reproduce its content.

## Scope

One document, one topic. A document named after a topic covers that topic only. Adjacent material belongs to
the adjacent document, or does not exist yet.

## Form

- Schematic: short sections, tables for enumerable facts, one idea per paragraph.
- Direct, professional language. Short sentences. No filler, no narration.
- English.
- A rule states what holds and what breaks when it is violated.
- Motivation is a clause attached to the rule it justifies, never a section of its own.

## What a document is not

- Not a changelog: no dates, no "decision taken on", no "supersedes", no history of what changed.
- Not a plan: no open items, no TODO, no status. Planned work belongs to a plan document.
- Not a tutorial: no step-by-step walkthrough of ordinary tasks.

A document describes the current state as if it had always been that way.

## Maintenance

Update a document in the same change that alters the behaviour it describes. When a rule stops holding, delete
it; do not annotate it as obsolete.
