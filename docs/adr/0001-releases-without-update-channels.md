# 0001. Releases without update channels

- Status: Accepted
- Date: 2026-10-19

## Context

The update check reads one of two feeds, selected by the user or by the `UpdateChannel` policy:
`Stable` reads `bravo-public/currentversion.json`, `Dev` reads `bravo-internal/currentversion.json`, both on
`bravorelease.blob.core.windows.net`. The selector is shown to every user, `Dev` serves untested internal
builds, and the channel spreads over settings, policy templates, the API, the frontend and every translation.

Releases are published on GitHub, and a preview carries its prerelease tag in `version.json`
([versioning](../design/versioning.md)). A preview and the release that follows it share `Major.Minor.Patch`
and differ only in the fourth field of the file version.

## Decision

- Every release is published on GitHub, as a prerelease or as a stable release.
- The update check reads `bravo-public/currentversion.json` only. The feed lists stable releases only;
  previews have no feed entry and are downloaded from the GitHub release page.
- The feed `version` is the four-field numeric file version of the release, for example `1.1.0.25`.
- The channel is removed from the application: the selector, the user setting, the `UpdateChannel` policy, the
  API parameter and the strings.
- `bravo-internal/currentversion.json` is updated once, to the first release without channels, and then left
  unchanged.

## Rejected alternatives

- A separate preview feed, or an opt-in to preview notifications: it reintroduces the channel under another
  name, for a preview audience that downloads builds by hand.
- Reading releases from the GitHub API instead of the feed: installed clients read the feed regardless.

## Consequences

- Installed clients on `Dev` read only `bravo-internal`: its single update moves them to a release that reads
  `bravo-public`.
- Both feed URLs are compiled into installed clients and stay available while those clients exist.
- A three-field `version` parses with an undefined fourth field and compares lower than any build of the same
  `Major.Minor.Patch`, so a preview user is never notified of the release. A non-numeric `version` makes every
  installed client fail in `Version.Parse`.
- A `1.0.x` release is no longer possible: installed `1.0` builds carry dated file versions such as
  `1.0.9517.31194`, which compare higher than `1.0.14` in the feed and in the MSI.
- An `UpdateChannel` value set by an administrator is ignored; the release notes say so.
- Existing `usersettings.json` files keep reading correctly: the serializer ignores the removed property.
