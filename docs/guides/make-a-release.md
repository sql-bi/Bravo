# How to make a release

For the maintainer, when a commit is to become a preview or a stable release. What a release is made of,
and why: [the design](../design/release.md).

## Before the first release

Once, and again when the branch that releases changes: `develop-vnext` until the final merge, `main` after it.

1. In Microsoft Entra, give the release an identity: an app registration, or a user-assigned managed
   identity. Add a federated credential to it for GitHub Actions:

   | Field | Value |
   | --- | --- |
   | Issuer | `https://token.actions.githubusercontent.com` |
   | Subject | `repo:sql-bi/Bravo:ref:refs/heads/<branch>` |
   | Audience | `api://AzureADTokenExchange` |

   The subject is matched to the letter, and names one branch: a second branch is a second credential.
2. On the Key Vault that holds the certificate of the publisher, let the identity sign and read the
   certificate. With role-based access, the roles `Key Vault Crypto User` and `Key Vault Reader`. With access
   policies, the key permissions Get and Sign and the certificate permission Get.
3. In the repository, under Settings, Secrets and variables, Actions, Variables, add four variables:

   | Variable | Value |
   | --- | --- |
   | `SIGNING_CLIENT_ID` | The client id of the identity |
   | `SIGNING_TENANT_ID` | The id of its tenant |
   | `SIGNING_VAULT_URL` | The address of the vault, `https://<name>.vault.azure.net` |
   | `SIGNING_CERTIFICATE_NAME` | The name of the certificate in the vault |

None of the four is a secret. The repository needs no secret and no environment.

## Steps

1. Set the version in `version.json`: `1.1.0-beta.1` for a preview, `1.1.0` for the stable release. The change
   reaches the branch as every other change does.
2. Check that continuous integration is green on the commit to release: the release does not look.
3. Start the release on the branch, at the root of the repository:

   ```bash
   gh workflow run release.yml --ref <branch>
   ```

   Once `release.yml` is on `main`, the button "Run workflow" of GitHub does the same.
4. Follow the run:

   ```bash
   gh run watch
   ```

5. Open the draft among the releases of the repository. Its six files can be downloaded by who can write in
   the repository: try them on a machine that can be thrown away. Correct the notes, then publish the draft.
6. For a stable release only, after the draft is published: download the artifact `update-feed` of the run,
   and upload its `currentversion.json` to the two containers that an installed 1.0.x reads.

   ```bash
   gh run download <run id> --name update-feed
   az storage blob upload --auth-mode login --account-name bravorelease --container-name bravo-public --name currentversion.json --file currentversion.json --overwrite true --content-type application/json
   az storage blob upload --auth-mode login --account-name bravorelease --container-name bravo-internal --name currentversion.json --file currentversion.json --overwrite true --content-type application/json
   ```

To try the workflow and publish nothing, add `-f dry-run=true` to the command of step 3: the files are signed
with a certificate made on the runner, and no draft is created.

## Check

- The summary of the run lists the six files and, for a stable release, shows the update feed.
- The draft has the six files. The properties of a package, under Digital Signatures, name the publisher and a
  timestamp.
- After step 6, an installed 1.0.x says that a new version exists, and its download is the file of its own
  kind.

## If something goes wrong

- The step "Sign in to Azure" fails with `AADSTS70021`: no federated credential has the subject of the run.
  Check the branch in the subject.
- "The tag v... exists": the version of `version.json` is released already. Set the next one.
- "A release v... exists already": an earlier run left a draft. Delete the draft, then start again.
- A run that failed after the draft was created left the draft: nothing is public. Delete it.
