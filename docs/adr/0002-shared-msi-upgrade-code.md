# 0002. One UpgradeCode for the per-user and the per-machine MSI

- Status: Accepted
- Date: 2026-10-19

## Context

Bravo ships two MSI packages from the same build: [Bravo.wxs](../../installer/wix/src/Bravo/Bravo.wxs),
installed per machine, and [Bravo-perUser.wxs](../../installer/wix/src/Bravo/Bravo-perUser.wxs), installed
per user. Both declare the same `UpgradeCode`, take the MSI `ProductVersion` from the file version
([versioning](../design/versioning.md)), and author `MajorUpgrade` with `AllowSameVersionUpgrades="yes"` and
`AllowDowngrades="no"`.

Windows Installer behavior that constrains the choice, from the Microsoft documentation:

| Behavior | Source |
| --- | --- |
| A major upgrade is never installed across installation contexts: a per-user product is upgraded only by a per-user installation, a per-machine product only by a per-machine one | [Major Upgrades](https://learn.microsoft.com/windows/win32/msi/major-upgrades) |
| Only the first three fields of `ProductVersion` are compared; the fourth is ignored | [ProductVersion](https://learn.microsoft.com/windows/win32/msi/productversion) |
| `FindRelatedProducts` runs at the first installation only, not in maintenance mode or uninstallation | [FindRelatedProducts](https://learn.microsoft.com/windows/win32/msi/findrelatedproducts-action) |
| One `UpgradeCode` can cover the versions and languages of a single product; each stand-alone product has its own | [Using an UpgradeCode](https://learn.microsoft.com/windows/win32/msi/using-an-upgradecode) |
| A dual-purpose package can install in either context; the context of the first installation is used for all later updates, repairs and removal | [Single Package Authoring](https://learn.microsoft.com/windows/win32/msi/single-package-authoring) |

`AllowSameVersionUpgrades="yes"` makes WiX set `msidbUpgradeAttributesVersionMaxInclusive`, so an installed
product with the same three fields is treated as an older version and removed
([WiX MajorUpgrade](https://docs.firegiant.com/wix/schema/wxs/majorupgrade/)). Without it, two packages that
differ only in the fourth field install side by side as two products. The inclusive maximum triggers the ICE61
warning ([ICE61](https://learn.microsoft.com/windows/win32/msi/ice61)), which the
[packaging pipeline](../../.azure/pipelines/build-bravo.yaml) suppresses.

## Decision

Keep one `UpgradeCode` for both packages. They are one product in two installation contexts, like a
dual-purpose package.

## Rejected alternatives

- A separate `UpgradeCode` per package: the two contexts never upgrade each other in either case, so nothing
  changes for users. The new package would no longer find installations made with the current code, and would
  install beside them, unless its `Upgrade` table kept the current code permanently.

## Consequences

- Each package upgrades installations of its own context. A per-user and a per-machine installation can
  coexist on the same computer, under distinct product names in Programs and Features, and neither removes
  the other.
- Builds of the same `Major.Minor.Patch` replace each other in either direction, only because of
  `AllowSameVersionUpgrades`: a preview upgrades to its release, and a preview installed over its release is
  not blocked as a downgrade.
- A lower `Major.Minor.Patch` is blocked by `AllowDowngrades="no"`; for the consequence on `1.0.x` see
  [ADR 0001](0001-releases-without-update-channels.md).
- The `UpgradeCode` does not tell the two packages apart; the product name and the product code do. Bravo has
  no winget manifest.
