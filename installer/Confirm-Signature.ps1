<#
.SYNOPSIS
    Checks the Authenticode signature of files, and returns the thumbprint of the certificate that signed them.

.DESCRIPTION
    Each file is signed, all by one certificate, and none was changed after it was signed. The script fails
    at the first file that is not. It changes nothing.

.PARAMETER Path
    The files to check.

.PARAMETER Thumbprint
    The thumbprint of the certificate that the files must be signed by. Without it, the certificate of the
    first file is the one of all.

.PARAMETER Trusted
    Asks for what the signature of a release must have: a chain that Windows trusts, and a timestamp.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string[]] $Path,
    [string] $Thumbprint = '',
    [switch] $Trusted
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

foreach ($file in $Path) {
    $signature = Get-AuthenticodeSignature -LiteralPath $file
    if ($null -eq $signature.SignerCertificate -or $signature.Status -in 'NotSigned', 'HashMismatch') {
        throw "The file is not signed, or was changed after it was signed: $file ($($signature.Status))"
    }
    if ($Thumbprint -eq '') {
        $Thumbprint = $signature.SignerCertificate.Thumbprint
    }
    if ($signature.SignerCertificate.Thumbprint -ne $Thumbprint) {
        throw "The file is signed by another certificate, $($signature.SignerCertificate.Subject): $file"
    }
    if ($Trusted -and ($signature.Status -ne 'Valid' -or $null -eq $signature.TimeStamperCertificate)) {
        throw "The signature is not trusted, or has no timestamp: $file ($($signature.Status): $($signature.StatusMessage))"
    }
    Write-Host "Signed by $($signature.SignerCertificate.Subject) ($($signature.Status)): $file"
}

$Thumbprint
