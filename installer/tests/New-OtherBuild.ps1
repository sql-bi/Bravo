<#
.SYNOPSIS
    Makes another build of the two packages that use the installed runtime, for the tests of the packages.

.DESCRIPTION
    The tests install one build of a package over another one of the same version, as the release of a
    version is installed over a preview of it. A package gets its product code when it is built, so two
    builds of the same commit are two products with the same files.

    The script builds the two packages, per machine and per user, moves them to their folder, and removes
    what the build keeps of them: the build of the packages that follows makes them again, with product
    codes of their own.

    It installs nothing, and runs on any machine that can build the packages.

.PARAMETER Folder
    Where the two packages go.

.PARAMETER BuildArguments
    What the build of the packages is given besides: what the build that follows is given, such as the
    signature.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Folder,
    [string[]] $BuildArguments = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $root 'installer\Bravo.Installer.wixproj'
$packages = Join-Path $root 'artifacts\installer\en-US'
$intermediate = Join-Path $root 'artifacts\obj\Bravo.Installer'

foreach ($scope in 'perMachine', 'perUser') {
    dotnet build $project -c Release -warnaserror -t:BuildPackage "-p:BravoScope=$scope" -p:BravoSelfContained=false @BuildArguments
    if ($LASTEXITCODE -ne 0) {
        throw "The build of the package $scope ended with the exit code $LASTEXITCODE."
    }
}

New-Item -ItemType Directory -Force $Folder | Out-Null
$built = @(Get-ChildItem -Path $packages -Filter '*-frameworkdependent*.msi')
if ($built.Count -ne 2) {
    throw "The build left $($built.Count) packages that use the installed runtime in $packages, and not two."
}
$built | Move-Item -Destination $Folder -Force

# Without what the build keeps of a package, the next build links it again: another product code.
Get-ChildItem -Path $intermediate -Directory -Recurse -Filter 'frameworkdependent-per*' | Remove-Item -Recurse -Force

Get-ChildItem -Path $Folder -Filter '*.msi' | ForEach-Object { "Another build: $($_.FullName)" }
