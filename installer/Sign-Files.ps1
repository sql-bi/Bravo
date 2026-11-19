<#
.SYNOPSIS
    Signs files of a release, as the build of the installer packages asks.

.DESCRIPTION
    The build calls this script on the files of the host before a package holds them, and on each package
    after it is built. What it signs with is its first parameter.

    KeyVault: the certificate of the publisher, which Azure Key Vault holds. AzureSignTool signs with it and
    asks a timestamp for the signature, so that it stays valid after the certificate has expired. It reads:
        BRAVO_SIGNING_VAULT_URL      the address of the vault
        BRAVO_SIGNING_CERTIFICATE    the name of the certificate in the vault
        BRAVO_SIGNING_TOKEN          an access token for the vault, of an identity that may sign

    TestCertificate: a certificate of the current user, for a run that publishes nothing. No timestamp is
    asked: the signature is looked at once, in the same run. It reads:
        BRAVO_SIGNING_THUMBPRINT     the thumbprint of the certificate, in the store of the current user

.PARAMETER Signing
    KeyVault or TestCertificate.

.PARAMETER Path
    The files to sign.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [ValidateSet('KeyVault', 'TestCertificate')] [string] $Signing,
    [Parameter(Mandatory, ValueFromRemainingArguments)] [string[]] $Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RequiredVariable([string] $Name) {
    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "The environment variable $Name is not set: signing with $Signing needs it."
    }

    $value
}

foreach ($file in $Path) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        throw "The file to sign is not there: $file"
    }
}

# Every file is checked right after it is signed: a signature that is missing or wrong stops the build.
$confirm = Join-Path $PSScriptRoot 'Confirm-Signature.ps1'

switch ($Signing) {
    'KeyVault' {
        $vault = Get-RequiredVariable 'BRAVO_SIGNING_VAULT_URL'
        $certificate = Get-RequiredVariable 'BRAVO_SIGNING_CERTIFICATE'
        $token = Get-RequiredVariable 'BRAVO_SIGNING_TOKEN'

        # The tool is the one of the manifest of the repository, restored with "dotnet tool restore".
        dotnet tool run azuresigntool sign `
            --azure-key-vault-url $vault `
            --azure-key-vault-certificate $certificate `
            --azure-key-vault-accesstoken $token `
            --file-digest sha256 `
            --timestamp-rfc3161 'http://timestamp.digicert.com' `
            --timestamp-digest sha256 `
            @Path
        if ($LASTEXITCODE -ne 0) {
            throw "AzureSignTool ended with the exit code $LASTEXITCODE."
        }

        # What a release ships has a signature that Windows trusts, with its timestamp: the build stops here
        # when a file has not.
        & $confirm -Path $Path -Trusted | Out-Null
    }

    'TestCertificate' {
        $thumbprint = Get-RequiredVariable 'BRAVO_SIGNING_THUMBPRINT'
        $certificate = Get-Item -LiteralPath "Cert:\CurrentUser\My\$thumbprint"

        foreach ($file in $Path) {
            Set-AuthenticodeSignature -LiteralPath $file -Certificate $certificate -HashAlgorithm SHA256 | Out-Null
        }
        & $confirm -Path $Path -Thumbprint $thumbprint | Out-Null
    }
}
