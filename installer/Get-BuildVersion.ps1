<#
.SYNOPSIS
    Reads the version of the build, the one that the packages of this commit get.

.DESCRIPTION
    Nerdbank.GitVersioning computes the version from version.json and from the history of git, in a target of
    the build. The script runs that target and returns:

        Version       the semantic version, which names the files and is in the title of the window
        FileVersion   the version of the files of the application, which is the version of the product
        IsPrerelease  whether version.json gives the version a label, as for a preview

    It builds nothing.

.PARAMETER PublicRelease
    The version of a release: the semantic version alone. Any other build has the commit after it.
#>
[CmdletBinding()]
param([switch] $PublicRelease)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'Bravo.Installer.wixproj'
$arguments = @($project, '-t:GetBuildVersion', '-getProperty:NuGetPackageVersion', '-getProperty:BuildVersion', '-getProperty:PrereleaseVersion')
if ($PublicRelease) {
    $arguments += '-p:PublicRelease=true'
}

$output = dotnet msbuild @arguments
if ($LASTEXITCODE -ne 0) {
    throw "The version of the build could not be read: dotnet msbuild ended with the exit code $LASTEXITCODE.`n$output"
}

$properties = ($output | ConvertFrom-Json).Properties
[pscustomobject]@{
    Version      = $properties.NuGetPackageVersion
    FileVersion  = $properties.BuildVersion
    IsPrerelease = $properties.PrereleaseVersion -ne ''
}
