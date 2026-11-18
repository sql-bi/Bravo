# Design

These documents describe the previous implementation, the one on `main`: the paths they name exist there. On
this branch they are requirements and prior art for the component that replaces each part, not a description
of the redesigned system, whose architecture is for now in
[the specification](../spec.md#architecture-decided-so-far).

| Document | Content |
| --- | --- |
| [`architecture.md`](previous/architecture.md) | Composition and ownership of resources, the threads of the user interface and of the server, the contract between the frontend and the API, the single instance, the boundaries of the configuration |
| [`diagnostics.md`](previous/diagnostics.md) | How the host reports exceptions: the paths of a report, startup and fatal errors, handled exceptions, the local log and the diagnostics pane, the consent to telemetry |
| [`versioning.md`](previous/versioning.md) | The single version computed by Nerdbank.GitVersioning: its source, the values derived from it, the rules |
