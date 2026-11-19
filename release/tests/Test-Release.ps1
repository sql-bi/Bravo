<#
.SYNOPSIS
    Tests the functions of release/Release.psm1.

.DESCRIPTION
    The script builds nothing, installs nothing and publishes nothing: it writes in a temporary folder of
    its own, which it removes. It runs on any machine with PowerShell 7; the checks of a signature need
    Windows.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot '..\Release.psm1') -Force

$script:Checks = 0

function Assert-Equal($Expected, $Actual, [string] $What) {
    $script:Checks++
    if ($Expected -cne $Actual) {
        throw "$What`n  expected: '$Expected'`n  actual:   '$Actual'"
    }
    Write-Host "    ok  $What = '$Actual'"
}

function Assert-True([bool] $Condition, [string] $What) {
    $script:Checks++
    if (-not $Condition) {
        throw "Not true: $What"
    }
    Write-Host "    ok  $What"
}

# The script block fails, and its message has the text.
function Assert-Fails([scriptblock] $Action, [string] $Text, [string] $What) {
    $script:Checks++
    $message = $null
    try {
        & $Action | Out-Null
    }
    catch {
        $message = $_.Exception.Message
    }
    if ($null -eq $message) {
        throw "Did not fail: $What"
    }
    if ($message -notlike "*$Text*") {
        throw "$What`n  failed with: '$message'`n  and not with: '$Text'"
    }
    Write-Host "    ok  $What fails: $message"
}

function Write-Step([string] $Text) {
    Write-Host ''
    Write-Host "=== $Text"
}

$Repository = 'owner/repository'
$Folder = Join-Path ([IO.Path]::GetTempPath()) "bravo-release-tests-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Force $Folder | Out-Null

try {
    Write-Step 'The six files of a release have the names that 1.0.x asks for'
    # The names of the files of 1.0.14, with its version: the ones that its release has.
    $expected = @(
        'Bravo.1.0.14.x64.msi'
        'Bravo.1.0.14.x64-userinstaller.msi'
        'Bravo.1.0.14.x64-portable.zip'
        'Bravo.1.0.14.x64-frameworkdependent.msi'
        'Bravo.1.0.14.x64-frameworkdependent-userinstaller.msi'
        'Bravo.1.0.14.x64-frameworkdependent-portable.zip'
    )
    Assert-Equal ($expected -join ' ') (@(Get-ReleaseFileName '1.0.14') -join ' ') 'the names of the files'
    Assert-Equal 'Bravo.1.1.0-beta.1.x64.msi' @(Get-ReleaseFileName '1.1.0-beta.1')[0] 'the name of a package of a preview'

    Write-Step 'The list of downloads links every file of the release'
    $notes = New-ReleaseNotes -Version '1.1.0' -Repository $Repository
    foreach ($name in Get-ReleaseFileName '1.1.0') {
        Assert-True ($notes.Contains("(https://github.com/$Repository/releases/download/v1.1.0/$name)")) "the list links $name"
    }
    Assert-True ($notes.StartsWith('## Download')) 'the list begins with its heading'

    Write-Step 'The update feed of a stable release is what an installed 1.0.x reads'
    $feed = Join-Path $Folder 'currentversion.json'
    New-UpdateFeed -Path $feed -Version '1.1.0' -FileVersion '1.1.0.188' -Repository $Repository
    $bytes = [IO.File]::ReadAllBytes($feed)
    Assert-Equal ([byte][char] '{') $bytes[0] 'the first byte of the file: no byte order mark'
    $document = Get-Content $feed -Raw | ConvertFrom-Json
    Assert-Equal 'channel version download changelog' ($document.PSObject.Properties.Name -join ' ') 'the fields of the feed'
    Assert-Equal '1.1.0.188' $document.version 'the version, the one of the files'
    Assert-Equal "https://github.com/$Repository/releases/download/v1.1.0/Bravo.1.1.0.x64.msi" $document.download 'the package that 1.0.x starts from'
    Assert-Equal "https://github.com/$Repository/releases" $document.changelog 'the address of the changes'
    Confirm-UpdateFeed -Path $feed -FileName (Get-ReleaseFileName '1.1.0')
    Assert-True $true 'the feed passes the check'

    Write-Step 'A feed that 1.0.x cannot use does not pass the check'
    $files = @(Get-ReleaseFileName '1.1.0')
    $write = { param($Text) [IO.File]::WriteAllText($feed, $Text, [Text.UTF8Encoding]::new($false)) }
    $good = "https://github.com/$Repository/releases/download/v1.1.0/Bravo.1.1.0.x64.msi"
    $changes = "https://github.com/$Repository/releases"

    & $write "{ `"version`": `"1.0.9999.1`", `"download`": `"$good`", `"changelog`": `"$changes`" }"
    Assert-Fails { Confirm-UpdateFeed -Path $feed -FileName $files } 'not above every 1.0.x' 'a version of 1.0'

    & $write "{ `"version`": `"1.1.0-beta.1`", `"download`": `"$good`", `"changelog`": `"$changes`" }"
    Assert-Fails { Confirm-UpdateFeed -Path $feed -FileName $files } '' 'a version that is not numbers'

    & $write "{ `"version`": `"1.1.0.188`", `"changelog`": `"$changes`" }"
    Assert-Fails { Confirm-UpdateFeed -Path $feed -FileName $files } '' 'a feed with no download'

    & $write "{ `"version`": `"1.1.0.188`", `"download`": `"$good`", `"changelog`": `"$changes`" }"
    Assert-Fails { Confirm-UpdateFeed -Path $feed -FileName ($files -ne 'Bravo.1.1.0.x64-frameworkdependent-portable.zip') } 'Bravo.1.1.0.x64-frameworkdependent-portable.zip' 'a release without one of the six files'

    $perUser = "https://github.com/$Repository/releases/download/v1.1.0/Bravo.1.1.0.x64-userinstaller.msi"
    & $write "{ `"version`": `"1.1.0.188`", `"download`": `"$perUser`", `"changelog`": `"$changes`" }"
    Assert-Fails { Confirm-UpdateFeed -Path $feed -FileName $files } 'is not a file of the release' 'a download that is not the package per machine that carries the runtime'

    if ($IsWindows) {
        Write-Step 'A signature is checked'
        $unsigned = Join-Path $Folder 'unsigned.ps1'
        Set-Content -Path $unsigned -Value "'not signed'"
        Assert-Fails { Confirm-Signature -Path $unsigned } 'is not signed' 'a file with no signature'

        # PowerShell itself: a file that its publisher signed, with a timestamp, and that Windows trusts.
        $signed = (Get-Process -Id $PID).Path
        $thumbprint = Confirm-Signature -Path $signed -Trusted
        Assert-True ($thumbprint -match '^[0-9A-F]{40}$') 'the check returns the thumbprint of the certificate'
        Assert-Equal $thumbprint (Confirm-Signature -Path $signed -Thumbprint $thumbprint) 'a file signed by the certificate that is asked for'
        Assert-Fails { Confirm-Signature -Path $signed -Thumbprint ('0' * 40) } 'another certificate' 'a file signed by another certificate'
    }
}
finally {
    Remove-Item $Folder -Recurse -Force
}

Write-Host ''
Write-Host "$script:Checks checks passed."
