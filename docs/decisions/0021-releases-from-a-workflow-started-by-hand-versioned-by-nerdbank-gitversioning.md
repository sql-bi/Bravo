# 0021. Releases from a workflow started by hand, versioned by Nerdbank.GitVersioning

- Status: Accepted
- Date: 2026-11-19

## Context

1.0.x is built and signed by a pipeline of Azure DevOps; its GitHub release and its update feed are made by
hand. Backlog B-30 asked how the version and the notes are produced.

## Decision

- **The version is the one of [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning)**,
  from `version.json`, in every build: its file version orders two builds, its semantic version names the
  files.
- **A separate workflow, `release.yml`, started by hand**
  ([`workflow_dispatch`](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#workflow_dispatch)).
  It does not wait for continuous integration. It signs the application and the packages, installs them on
  a runner, and creates a draft release, which the maintainer publishes.
- **The signature is the one of 1.0.x**, AzureSignTool with the certificate of the publisher in Key Vault,
  through a federated identity
  ([OpenID Connect](https://docs.github.com/en/actions/concepts/security/openid-connect)): no secret, no
  environment, no reviewer.
- **The notes are the ones that GitHub generates**
  ([generated notes](https://docs.github.com/en/repositories/releasing-projects-on-github/automatically-generated-release-notes)),
  under the list of downloads.
- **Six files, with the names of 1.0.x.**
- **A version with a pre-release label is a preview**: a pre-release on GitHub, no feed.
- **For a stable release the workflow writes the feed file**, checked as 1.0.x reads it; the maintainer
  uploads it.
- **A run that publishes nothing proves the workflow**: on a push to a work branch, with a certificate made
  on the runner.
- **The application is restored in locked mode**: `Bravo.Desktop` declares `win-x64`.

## Rejected alternatives

- The version read from a tag: a preview and its release would share one file version.
- The release as a run of continuous integration on a tag: it complicates that workflow.
- An environment with a reviewer: the maintainer starts every release himself.
- A changelog kept by hand; notes from the commits, written for developers.
- The feed written by the workflow: one more identity.
- A client secret: it expires.

## Consequences

- Every job that builds clones the whole history: the version counts the commits.
- The federated credential names a branch: a workflow pushed to that branch can sign.
- Until the final merge it is started with `gh workflow run`: the button of GitHub needs the file
  on `main`.
- The generated notes list pull requests: the ones of 1.1.0 are edited by hand.
- The run that publishes nothing proves neither the sign-in nor Key Vault.
