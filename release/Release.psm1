<#
    What a release needs beyond the build of its files: the names of the six files, the list of downloads
    that goes above the notes that GitHub generates, the update feed that an installed 1.0.x reads, and the
    check of a signature. The workflow of the release calls these functions, and
    release/tests/Test-Release.ps1 tests them.
#>

Set-StrictMode -Version Latest

# Every 1.0.x has a file version of 1.0, a number of days and a number of seconds: all of them are below this.
$AboveEveryFirstVersion = [version] '1.0.65535.65535'

# The six files of a release, with the names that an installed 1.0.x builds to ask for its update: the
# package per machine that carries the runtime, and what 1.0.x adds to its name.
function Get-ReleaseFileName {
    [CmdletBinding()]
    param([Parameter(Mandatory)] [string] $Version)

    foreach ($runtime in '', '-frameworkdependent') {
        "Bravo.$Version.x64$runtime.msi"
        "Bravo.$Version.x64$runtime-userinstaller.msi"
        "Bravo.$Version.x64$runtime-portable.zip"
    }
}

function Get-ReleaseAddress([string] $Repository, [string] $Version, [string] $FileName) {
    "https://github.com/$Repository/releases/download/v$Version/$FileName"
}

# The list of the downloads of a release, which goes above the notes that GitHub generates.
function New-ReleaseNotes {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Version,
        [Parameter(Mandatory)] [string] $Repository
    )

    $link = { param($Text, $FileName) "[$Text]($(Get-ReleaseAddress $Repository $Version $FileName))" }
    $user = '_(installs without requiring admin rights, does not install Bravo as an External Tool)_'
    $portable = '_(unzip and run, does not install Bravo as an External Tool)_'
    $runtime = 'https://dotnet.microsoft.com/download/dotnet/10.0'

    @(
        '## Download (64 bit)'
        '- Bravo self-contained'
        "  - $(& $link 'Self contained' "Bravo.$Version.x64.msi") **(RECOMMENDED)**"
        "  - $(& $link 'Self contained - user' "Bravo.$Version.x64-userinstaller.msi") $user"
        "  - $(& $link 'Self contained - portable' "Bravo.$Version.x64-portable.zip") $portable"
        ''
        "- Bravo framework-dependent - _smaller packages that do not include the .NET runtime: you need to [install separately]($runtime) the .NET Desktop Runtime and the ASP.NET Core Runtime, version 10_"
        "  - $(& $link 'Framework dependent' "Bravo.$Version.x64-frameworkdependent.msi")"
        "  - $(& $link 'Framework dependent - user' "Bravo.$Version.x64-frameworkdependent-userinstaller.msi") $user"
        "  - $(& $link 'Framework dependent - portable' "Bravo.$Version.x64-frameworkdependent-portable.zip") $portable"
        ''
    ) -join "`n"
}

# Writes the update feed of a stable release: the file that an installed 1.0.x reads to learn that a new
# version exists. Its version is the file version, which 1.0.x compares with its own.
function New-UpdateFeed {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $Version,
        [Parameter(Mandatory)] [string] $FileVersion,
        [Parameter(Mandatory)] [string] $Repository
    )

    $feed = [ordered]@{
        channel   = 'Stable'
        version   = $FileVersion
        download  = Get-ReleaseAddress $Repository $Version "Bravo.$Version.x64.msi"
        changelog = "https://github.com/$Repository/releases"
    }

    # UTF-8 with no byte order mark and no other encoding: 1.0.x reads the file as text of a web answer.
    $text = ($feed | ConvertTo-Json) -replace "`r`n", "`n"
    [IO.File]::WriteAllText($Path, "$text`n", [Text.UTF8Encoding]::new($false))
}

# Reads an update feed as an installed 1.0.x does, and fails when 1.0.x would not be told about the release,
# or would ask for a file that the release does not have.
function Confirm-UpdateFeed {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string[]] $FileName
    )

    $text = [Text.UTF8Encoding]::new($false, $true).GetString([IO.File]::ReadAllBytes($Path))
    $document = [Text.Json.JsonDocument]::Parse($text)
    try {
        # 1.0.x reads these three, each as a string, and fails when one is missing.
        $version = $document.RootElement.GetProperty('version').GetString()
        $download = $document.RootElement.GetProperty('download').GetString()
        $changelog = $document.RootElement.GetProperty('changelog').GetString()
    }
    finally {
        $document.Dispose()
    }

    $parsed = [version]::Parse($version)
    if ($parsed -le $AboveEveryFirstVersion) {
        throw "The version of the feed, $version, is not above every 1.0.x: an installed 1.0.x would not be told."
    }
    if (-not [uri]::IsWellFormedUriString($changelog, [UriKind]::Absolute)) {
        throw "The changelog of the feed is not an address: $changelog"
    }

    # The name of the file that 1.0.x asks for: the one of the feed, with what 1.0.x adds for how it was
    # published and installed.
    $address = [uri]::new($download, [UriKind]::Absolute)
    $name = [IO.Path]::GetFileNameWithoutExtension($address.LocalPath)
    $extension = [IO.Path]::GetExtension($address.LocalPath)
    foreach ($runtime in '', '-frameworkdependent') {
        foreach ($asked in "$name$runtime$extension", "$name$runtime-userinstaller$extension", "$name$runtime-portable.zip") {
            if ($FileName -cnotcontains $asked) {
                throw "An installed 1.0.x would ask for $asked, which is not a file of the release."
            }
        }
    }
}

# Checks the Authenticode signature of files: each one is signed, all by one certificate, and none was
# changed after it was signed. Trusted asks for more, what a release must have: a chain that Windows trusts,
# and a timestamp. Returns the thumbprint of the certificate.
function Confirm-Signature {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string[]] $Path,
        [string] $Thumbprint = '',
        [switch] $Trusted
    )

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
}

Export-ModuleMember -Function Get-ReleaseFileName, New-ReleaseNotes, New-UpdateFeed, Confirm-UpdateFeed, Confirm-Signature
