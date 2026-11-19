<#
.SYNOPSIS
    Reads the version of the build, the one that the packages of this commit get.

.DESCRIPTION
    Nerdbank.GitVersioning computes the version from version.json and from the history of git, in a target of
    the build. The script runs that target and returns:

        Version       the semantic version, which names the files and is in the title of the window
        FileVersion   the version of the files of the application, which is the version of the product

    It builds nothing. That target comes with a package, so the script restores the packages of the installer
    project first: the project is not in the solution, and a restore or a build of the solution leaves it out.

.PARAMETER PublicRelease
    Whether the build is a public release, which has the semantic version alone, or not, and then has the
    commit after it. Without the parameter the branch decides, as version.json says.
#>
[CmdletBinding()]
param([bool] $PublicRelease)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'Bravo.Installer.wixproj'
$arguments = @($project, '-restore', '-t:ReadBuildVersion', '-getProperty:BravoVersion', '-getProperty:BravoFileVersion')
if ($PSBoundParameters.ContainsKey('PublicRelease')) {
    $arguments += "-p:PublicRelease=$($PublicRelease.ToString().ToLowerInvariant())"
}

$output = dotnet msbuild @arguments
if ($LASTEXITCODE -ne 0) {
    throw "The version of the build could not be read: dotnet msbuild ended with the exit code $LASTEXITCODE.`n$output"
}

$properties = ($output | ConvertFrom-Json).Properties
[pscustomobject]@{
    Version     = $properties.BravoVersion
    FileVersion = $properties.BravoFileVersion
}
