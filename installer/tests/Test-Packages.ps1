<#
.SYNOPSIS
    Installs, upgrades and removes the installer packages for real, and checks what each step leaves.

.DESCRIPTION
    The script changes the machine it runs on: it installs and removes Bravo, per machine and per user, and
    writes in the data folder of the application. It runs on a runner of GitHub Actions and nowhere else.

    Every step is a silent installation, as a deployment tool runs it. The log of each run of Windows
    Installer is kept in the log folder.

.PARAMETER PackagesFolder
    The folder with the four packages of the version under test and the package of the next version.

.PARAMETER PreviousFolder
    The folder with the packages of 1.0.14 that use the installed runtime, per machine and per user.

.PARAMETER Version
    The version of the packages under test.

.PARAMETER NextVersion
    The version of the package that upgrades them.

.PARAMETER LogFolder
    Where the logs of Windows Installer go.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $PackagesFolder,
    [Parameter(Mandatory)] [string] $PreviousFolder,
    [Parameter(Mandatory)] [string] $Version,
    [Parameter(Mandatory)] [string] $NextVersion,
    [Parameter(Mandatory)] [string] $LogFolder
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($env:GITHUB_ACTIONS -ne 'true') {
    throw 'This script installs and removes Bravo. It runs on a runner of GitHub Actions only.'
}

$UpgradeCode = '{888BDB48-B16A-4D9A-8FE6-0206897B1162}'
$RegistryKey = 'Software\SQLBI\Bravo'
$ChoiceNames = 'desktopShortcutEnabled', 'programMenuShortcutEnabled', 'installerTelemetryEnabled', 'applicationTelemetryEnabled'
$DataFolder = Join-Path $env:LOCALAPPDATA 'Bravo'
$ExternalToolFile = Join-Path ${env:CommonProgramFiles(x86)} 'Microsoft Shared\Power BI Desktop\External Tools\bravo.pbitool.json'

# What differs between the two installation contexts.
$Scopes = @{
    perMachine = @{
        Suffix        = ''
        ProductName   = 'Bravo for Power BI'
        RegistryRoot  = 'HKLM:'
        InstallFolder = Join-Path $env:ProgramFiles 'SQLBI\Bravo\'
        Desktop       = [Environment]::GetFolderPath('CommonDesktopDirectory')
        ProgramMenu   = [Environment]::GetFolderPath('CommonPrograms')
        Assignment    = '1'
    }
    perUser    = @{
        Suffix        = '-userinstaller'
        ProductName   = 'Bravo for Power BI (User)'
        RegistryRoot  = 'HKCU:'
        InstallFolder = Join-Path $env:LOCALAPPDATA 'Programs\SQLBI\Bravo\'
        Desktop       = [Environment]::GetFolderPath('DesktopDirectory')
        ProgramMenu   = [Environment]::GetFolderPath('Programs')
        Assignment    = '0'
    }
}

New-Item -ItemType Directory -Force $LogFolder | Out-Null

# Windows Installer is given full paths.
$PackagesFolder = (Resolve-Path $PackagesFolder).Path
$PreviousFolder = (Resolve-Path $PreviousFolder).Path
$LogFolder = (Resolve-Path $LogFolder).Path
$script:Checks = 0
$script:LogNumber = 0

# --- Checks -------------------------------------------------------------------------------------------------

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

function Write-Step([string] $Text) {
    Write-Host ''
    Write-Host "=== $Text"
}

# --- Windows Installer --------------------------------------------------------------------------------------

function Get-PackagePath([string] $Scope, [bool] $SelfContained, [string] $PackageVersion = $Version) {
    $runtime = if ($SelfContained) { '' } else { '-frameworkdependent' }
    $path = Join-Path $PackagesFolder "Bravo.$PackageVersion.x64$runtime$($Scopes[$Scope].Suffix).msi"
    if (-not (Test-Path $path)) { throw "The package is missing: $path" }
    return $path
}

function Get-PreviousPackagePath([string] $Scope) {
    $path = Join-Path $PreviousFolder "Bravo.1.0.14.x64-frameworkdependent$($Scopes[$Scope].Suffix).msi"
    if (-not (Test-Path $path)) { throw "The package of 1.0.14 is missing: $path" }
    return $path
}

# Runs Windows Installer with no dialog and no restart, and returns its exit code.
function Invoke-WindowsInstaller([string] $Arguments, [string] $Name) {
    $script:LogNumber++
    $log = Join-Path $LogFolder ('{0:00}-{1}.log' -f $script:LogNumber, $Name)
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $process = Start-Process -FilePath 'msiexec.exe' -ArgumentList "$Arguments /qn /norestart /l*v `"$log`"" -Wait -PassThru
    Write-Host ("    msiexec {0} -> exit code {1} in {2:N0} s" -f $Arguments, $process.ExitCode, $watch.Elapsed.TotalSeconds)

    # A package ignores the exit code of what it runs, so a command that failed shows in the log alone.
    $failed = @(Select-String -Path $log -Pattern 'returned actual error code' | ForEach-Object { $_.Line })
    if ($failed.Count -gt 0) {
        throw "An action of the package failed:`n  $($failed -join "`n  ")"
    }

    return $process.ExitCode
}

function Install-Package([string] $Path, [string] $Name, [string] $Properties = '') {
    $arguments = "/i `"$Path`" $Properties".TrimEnd()
    return Invoke-WindowsInstaller $arguments $Name
}

function Uninstall-Product([string] $ProductCode, [string] $Name, [string] $Properties = '') {
    $arguments = "/x $ProductCode $Properties".TrimEnd()
    return Invoke-WindowsInstaller $arguments $Name
}

# 1.0.14 sends the events of its installer for real, at a removal whatever was chosen at the installation.
# On the runner every run of its packages is told not to.
$NoTelemetry = 'INSTALLERTELEMETRYENABLED=0'

$script:Installer = New-Object -ComObject WindowsInstaller.Installer

function Get-InstallerProperty([string] $Name, [object[]] $Arguments) {
    return $script:Installer.GetType().InvokeMember($Name, 'GetProperty', $null, $script:Installer, $Arguments)
}

# The installed products of the UpgradeCode, of both installation contexts.
function Get-Products {
    $products = @()
    foreach ($code in @(Get-InstallerProperty 'RelatedProducts' @($UpgradeCode))) {
        $products += [pscustomobject]@{
            Code       = $code
            Name       = Get-InstallerProperty 'ProductInfo' @($code, 'ProductName')
            Version    = Get-InstallerProperty 'ProductInfo' @($code, 'VersionString')
            Assignment = Get-InstallerProperty 'ProductInfo' @($code, 'AssignmentType')
        }
    }
    foreach ($product in $products) {
        Write-Host "    installed: $($product.Name) $($product.Version) $($product.Code) assignment=$($product.Assignment)"
    }
    return , $products
}

function Assert-NoProduct {
    $products = Get-Products
    Assert-Equal 0 $products.Count 'the number of installed products'
}

function Assert-SingleProduct([string] $Scope, [string] $ExpectedVersion) {
    $products = Get-Products
    Assert-Equal 1 $products.Count 'the number of installed products'
    Assert-Equal $Scopes[$Scope].ProductName $products[0].Name 'the name of the product'
    Assert-Equal "$ExpectedVersion.0" $products[0].Version 'the version of the product'
    Assert-Equal $Scopes[$Scope].Assignment $products[0].Assignment 'the installation context (1 is per machine)'
    return $products[0]
}

# --- What an installation leaves ----------------------------------------------------------------------------

function Get-RegistryValue([string] $Scope, [string] $Name) {
    $key = Join-Path $Scopes[$Scope].RegistryRoot $RegistryKey
    $item = Get-ItemProperty -Path $key -Name $Name -ErrorAction SilentlyContinue
    if ($null -eq $item) { return $null }
    return $item.$Name
}

function Get-Choices([string] $Scope) {
    $choices = [ordered]@{}
    foreach ($name in $ChoiceNames) { $choices[$name] = Get-RegistryValue $Scope $name }
    return $choices
}

function Format-Choices($Choices) {
    return ($Choices.Keys | ForEach-Object { "$_='$($Choices[$_])'" }) -join ' '
}

function Get-ShortcutTarget([string] $Folder, [string] $Scope) {
    $path = Join-Path $Folder "$($Scopes[$Scope].ProductName).lnk"
    if (-not (Test-Path $path)) { return $null }
    return (New-Object -ComObject WScript.Shell).CreateShortcut($path).TargetPath
}

# What Installed apps shows of a product of Windows Installer: the properties that the installer keeps under
# the account that owns the installation, which is the system for an installation per machine. The key is
# named by the product code with the digits of each of its parts in reverse order.
function Get-EntryInInstalledApps([string] $Scope, [string] $ProductCode) {
    $owner = if ($Scope -eq 'perMachine') { 'S-1-5-18' } else { [Security.Principal.WindowsIdentity]::GetCurrent().User.Value }
    $digits = $ProductCode.Trim('{', '}').Replace('-', '')
    $packed = ''
    foreach ($length in 8, 4, 4, 2, 2, 2, 2, 2, 2, 2, 2) {
        $part = $digits.Substring($packed.Length, $length).ToCharArray()
        [array]::Reverse($part)
        $packed += -join $part
    }
    $key = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Installer\UserData\$owner\Products\$packed\InstallProperties"
    return Get-ItemProperty -Path $key -ErrorAction SilentlyContinue
}

function Read-ExternalTool {
    if (-not (Test-Path $ExternalToolFile)) { return $null }
    return Get-Content $ExternalToolFile -Raw | ConvertFrom-Json
}

# Everything that a package of this version installs, with the choices given.
function Assert-Installed([string] $Scope, [string] $ExpectedVersion, [string] $InstallFolder, $ExpectedChoices, [bool] $SelfContained) {
    $product = Assert-SingleProduct $Scope $ExpectedVersion
    $executable = Join-Path $InstallFolder 'Bravo.exe'

    Assert-Equal "$ExpectedVersion.0" (Get-Item $executable).VersionInfo.FileVersion 'the version of the file of Bravo.exe'
    foreach ($file in 'Bravo.dll', 'Bravo.Setup.exe', 'WebView2Loader.dll', 'wwwroot\index.html', 'wwwroot\js\main.js', 'wwwroot\css\main.css') {
        Assert-True (Test-Path (Join-Path $InstallFolder $file)) "$file is installed"
    }
    foreach ($entry in 'Bravo.xml', 'runtimes', 'it') {
        Assert-True (-not (Test-Path (Join-Path $InstallFolder $entry))) "$entry is not installed"
    }
    Assert-Equal $SelfContained (Test-Path (Join-Path $InstallFolder 'coreclr.dll')) 'the package carries the .NET runtime'

    Assert-Equal $InstallFolder (Get-RegistryValue $Scope 'installFolder') 'the registry value installFolder'
    Assert-Equal (Format-Choices $ExpectedChoices) (Format-Choices (Get-Choices $Scope)) 'the choices in the registry'

    $desktopTarget = if ($ExpectedChoices.desktopShortcutEnabled -eq '1') { $executable } else { $null }
    $programMenuTarget = if ($ExpectedChoices.programMenuShortcutEnabled -eq '1') { $executable } else { $null }
    Assert-Equal $desktopTarget (Get-ShortcutTarget $Scopes[$Scope].Desktop $Scope) 'the target of the desktop shortcut'
    Assert-Equal $programMenuTarget (Get-ShortcutTarget $Scopes[$Scope].ProgramMenu $Scope) 'the target of the Start menu shortcut'

    $entry = Get-EntryInInstalledApps $Scope $product.Code
    Assert-True ($null -ne $entry) 'the product is listed in Installed apps'
    Assert-Equal $Scopes[$Scope].ProductName $entry.DisplayName 'the name in Installed apps'
    Assert-Equal "$ExpectedVersion.0" $entry.DisplayVersion 'the version in Installed apps'

    $tool = Read-ExternalTool
    if ($Scope -eq 'perMachine') {
        Assert-True ($null -ne $tool) 'the external tool is registered'
        Assert-Equal $executable $tool.path 'the executable of the external tool registration'
        Assert-Equal 'Bravo' $tool.name 'the name of the external tool'
        Assert-Equal '--server="%server%" --database="%database%"' $tool.arguments 'the arguments of the external tool'
    }
    else {
        Assert-True ($null -eq $tool) 'no external tool is registered'
    }

    return $product
}

function Assert-Removed([string] $Scope, [string] $InstallFolder) {
    Assert-NoProduct
    Assert-True (-not (Test-Path $InstallFolder)) "the folder $InstallFolder is removed"
    Assert-True ($null -eq (Get-RegistryValue $Scope 'installFolder')) 'the registry value installFolder is removed'
    Assert-True ($null -eq (Get-ShortcutTarget $Scopes[$Scope].Desktop $Scope)) 'the desktop shortcut is removed'
    Assert-True ($null -eq (Get-ShortcutTarget $Scopes[$Scope].ProgramMenu $Scope)) 'the Start menu shortcut is removed'
    Assert-True (-not (Test-Path $ExternalToolFile)) 'the external tool registration is removed'
}

# --- The data of the user -----------------------------------------------------------------------------------

function Write-UserData {
    New-Item -ItemType Directory -Force $DataFolder | Out-Null
    Set-Content -Path (Join-Path $DataFolder 'usersettings.json') -Value '{ "marker": "settings of the test" }'
    Set-Content -Path (Join-Path $DataFolder 'any-file.txt') -Value 'a file of the test'
}

function Test-UserData {
    $settings = Join-Path $DataFolder 'usersettings.json'
    $other = Join-Path $DataFolder 'any-file.txt'
    if (-not (Test-Path $settings) -or -not (Test-Path $other)) { return $false }
    return ((Get-Content $settings -Raw).Trim() -ceq '{ "marker": "settings of the test" }') -and ((Get-Content $other -Raw).Trim() -ceq 'a file of the test')
}

function Remove-UserData {
    if (Test-Path $DataFolder) { Remove-Item $DataFolder -Recurse -Force }
}

# --- The application ----------------------------------------------------------------------------------------

function Start-Application([string] $InstallFolder) {
    $process = Start-Process -FilePath (Join-Path $InstallFolder 'Bravo.exe') -PassThru
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while ($process.MainWindowHandle -eq [IntPtr]::Zero) {
        if ($process.HasExited) { throw "Bravo ended with exit code $($process.ExitCode) before it showed its window." }
        if ($watch.Elapsed.TotalSeconds -gt 60) { $process.Kill(); throw 'Bravo did not show its window in 60 seconds.' }
        Start-Sleep -Milliseconds 200
        $process.Refresh()
    }
    return $process
}

# The engine of the webview holds files of the data folder for a moment after the application has ended.
function Wait-WebViewEnd {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while ($watch.Elapsed.TotalSeconds -lt 30) {
        $left = @(Get-CimInstance Win32_Process -Filter "Name = 'msedgewebview2.exe'" | Where-Object { $_.CommandLine -like "*$DataFolder*" })
        if ($left.Count -eq 0) { return }
        Start-Sleep -Milliseconds 500
    }
    throw 'The processes of the webview did not end in 30 seconds.'
}

# Starts the installed application, checks which build and which runtime it is, and closes its window.
function Assert-ApplicationRuns([string] $InstallFolder, [string] $ExpectedVersion, [bool] $SelfContained) {
    $process = Start-Application $InstallFolder
    try {
        Assert-Equal "Bravo for Power BI - v$ExpectedVersion" $process.MainWindowTitle 'the title of the window, the one of a release build'
        $runtime = ($process.Modules | Where-Object { $_.ModuleName -eq 'coreclr.dll' }).FileName
        $carried = $runtime.StartsWith($InstallFolder, [StringComparison]::OrdinalIgnoreCase)
        Assert-Equal $SelfContained $carried "the .NET runtime is the one of the package ($runtime)"
        $process.CloseMainWindow() | Out-Null
        Assert-True ($process.WaitForExit(15000)) 'the application ends when its window is closed'
        Assert-Equal 0 $process.ExitCode 'the exit code of the application'
    }
    finally {
        if (-not $process.HasExited) { $process.Kill() }
    }
    Wait-WebViewEnd
}

# --- Scenarios ----------------------------------------------------------------------------------------------

$AllOn = [ordered]@{ desktopShortcutEnabled = '1'; programMenuShortcutEnabled = '1'; installerTelemetryEnabled = '1'; applicationTelemetryEnabled = '1' }
$AllOff = [ordered]@{ desktopShortcutEnabled = ''; programMenuShortcutEnabled = ''; installerTelemetryEnabled = ''; applicationTelemetryEnabled = '' }

# A first installation, the application, an upgrade while the application runs, the removal.
function Test-InstallUpgradeRemove([string] $Scope) {
    $folder = $Scopes[$Scope].InstallFolder

    Write-Step "$Scope`: a first installation"
    Remove-UserData
    Assert-NoProduct
    Assert-Equal 0 (Install-Package (Get-PackagePath $Scope $false) "$Scope-install") 'the exit code of the installation'
    Assert-Installed $Scope $Version $folder $AllOn $false | Out-Null

    Write-Step "$Scope`: the installed application starts and ends"
    Assert-ApplicationRuns $folder $Version $false

    Write-Step "$Scope`: an upgrade while the application runs"
    Write-UserData
    $running = Start-Application $folder
    try {
        $exitCode = Install-Package (Get-PackagePath $Scope $false $NextVersion) "$Scope-upgrade-while-running"
        Assert-Equal 0 $exitCode 'the exit code of the upgrade: no restart of Windows is asked for'
        Assert-True ($running.WaitForExit(15000)) 'the running application was closed'
        Assert-Equal 0 $running.ExitCode 'the exit code of the application that was closed'
    }
    finally {
        if (-not $running.HasExited) { $running.Kill() }
    }
    Wait-WebViewEnd
    Assert-True (@(Get-Process Bravo -ErrorAction SilentlyContinue).Count -eq 0) 'the application is not started again'
    $product = Assert-Installed $Scope $NextVersion $folder $AllOn $false
    Assert-True (Test-UserData) 'the data of the user is as it was'

    Write-Step "$Scope`: the removal"
    Assert-Equal 0 (Uninstall-Product $product.Code "$Scope-remove") 'the exit code of the removal'
    Assert-Removed $Scope $folder
    if ($Scope -eq 'perUser') {
        Assert-True (-not (Test-Path $DataFolder)) 'the removal of the package per user removes the data folder of the user'
    }
    else {
        Assert-True (Test-UserData) 'the removal of the package per machine leaves the data of the user'
    }
    Remove-UserData
}

# The public properties of 1.0.x on the command line: the same line gives 1.0.14 and this version the same
# choices, shortcuts and folder.
function Test-PublicProperties {
    $folder = 'C:\Bravo Custom\'
    $properties = "INSTALLFOLDER=`"$($folder.TrimEnd('\'))`" DESKTOPSHORTCUTENABLED=0 PROGRAMMENUSHORTCUTENABLED=1 $NoTelemetry APPLICATIONTELEMETRYENABLED=no TELEMETRYUSERID=someone"
    $expected = [ordered]@{ desktopShortcutEnabled = ''; programMenuShortcutEnabled = '1'; installerTelemetryEnabled = ''; applicationTelemetryEnabled = '' }

    Write-Step 'perMachine: the public properties, given to 1.0.14'
    Assert-Equal 0 (Install-Package (Get-PreviousPackagePath 'perMachine') 'properties-1.0.14-install' $properties) 'the exit code of the installation of 1.0.14'
    $previous = (Get-Products)[0]
    $previousChoices = Format-Choices (Get-Choices 'perMachine')
    $previousFolder = Get-RegistryValue 'perMachine' 'installFolder'
    $previousDesktop = $null -ne (Get-ShortcutTarget $Scopes.perMachine.Desktop 'perMachine')
    $previousProgramMenu = $null -ne (Get-ShortcutTarget $Scopes.perMachine.ProgramMenu 'perMachine')
    Write-Host "    1.0.14: folder='$previousFolder' $previousChoices desktop=$previousDesktop programMenu=$previousProgramMenu"
    Assert-Equal 0 (Uninstall-Product $previous.Code 'properties-1.0.14-remove' $NoTelemetry) 'the exit code of the removal of 1.0.14'
    Assert-NoProduct

    Write-Step 'perMachine: the same public properties, given to this version'
    Assert-Equal 0 (Install-Package (Get-PackagePath 'perMachine' $false) 'properties-install' $properties) 'the exit code of the installation'
    $product = Assert-Installed 'perMachine' $Version $folder $expected $false
    Assert-Equal $previousChoices (Format-Choices (Get-Choices 'perMachine')) 'the choices, as 1.0.14 wrote them'
    Assert-Equal $previousFolder (Get-RegistryValue 'perMachine' 'installFolder') 'the folder, as 1.0.14 wrote it'
    Assert-Equal $previousDesktop ($null -ne (Get-ShortcutTarget $Scopes.perMachine.Desktop 'perMachine')) 'a desktop shortcut, as with 1.0.14'
    Assert-Equal $previousProgramMenu ($null -ne (Get-ShortcutTarget $Scopes.perMachine.ProgramMenu 'perMachine')) 'a Start menu shortcut, as with 1.0.14'

    Assert-Equal 0 (Uninstall-Product $product.Code 'properties-remove') 'the exit code of the removal'
    Assert-Removed 'perMachine' $folder
}

# 1.0.14 installed with some choices turned off, the data of a user, then this version over it.
function Test-UpgradeFromPrevious([string] $Scope, [string] $Properties, $ExpectedChoices) {
    $folder = $Scopes[$Scope].InstallFolder

    Write-Step "$Scope`: 1.0.14 is installed, with $Properties"
    Remove-UserData
    Assert-Equal 0 (Install-Package (Get-PreviousPackagePath $Scope) "$Scope-1.0.14-install" $Properties) 'the exit code of the installation of 1.0.14'
    $previous = (Get-Products)[0]
    Assert-Equal '1.0.9805.22885' $previous.Version 'the version of the product of 1.0.14'
    Assert-Equal $folder (Get-RegistryValue $Scope 'installFolder') 'the folder of 1.0.14'
    $previousChoices = Format-Choices (Get-Choices $Scope)
    Write-Host "    1.0.14: $previousChoices"
    Write-UserData

    Write-Step "$Scope`: this version is installed over 1.0.14"
    Assert-Equal 0 (Install-Package (Get-PackagePath $Scope $false) "$Scope-upgrade-from-1.0.14") 'the exit code of the upgrade'
    $product = Assert-Installed $Scope $Version $folder $ExpectedChoices $false
    Assert-True ($product.Code -ne $previous.Code) '1.0.14 is no longer installed'
    Assert-Equal $previousChoices (Format-Choices (Get-Choices $Scope)) 'the choices, as 1.0.14 wrote them'
    Assert-True (Test-UserData) 'the data of the user is as it was'

    Write-Step "$Scope`: the upgraded application starts and ends"
    Assert-ApplicationRuns $folder $Version $false
    Assert-True (Test-UserData) 'the data of the user is as it was'

    Assert-Equal 0 (Uninstall-Product $product.Code "$Scope-remove-after-upgrade") 'the exit code of the removal'
    Assert-Removed $Scope $folder
    Remove-UserData
}

# What the removal of 1.0.14 does to the data of the user: what a user of 1.0.x sees today.
function Show-PreviousRemoval([string] $Scope) {
    Write-Step "$Scope`: what the removal of 1.0.14 leaves of the data of the user"
    Remove-UserData
    Assert-Equal 0 (Install-Package (Get-PreviousPackagePath $Scope) "$Scope-1.0.14-install-for-removal" $NoTelemetry) 'the exit code of the installation of 1.0.14'
    $previous = (Get-Products)[0]
    Write-UserData
    Assert-Equal 0 (Uninstall-Product $previous.Code "$Scope-1.0.14-remove" $NoTelemetry) 'the exit code of the removal of 1.0.14'
    Assert-NoProduct
    Write-Host "    OBSERVED: after the removal of 1.0.14 $Scope, the data folder exists: $(Test-Path $DataFolder); the data is as it was: $(Test-UserData)"
    Remove-UserData
}

# A package never upgrades an installation of the other context: both stay installed.
function Test-OtherContext([string] $InstalledScope, [string] $NewScope) {
    Write-Step "1.0.14 $InstalledScope is installed, then this version $NewScope"
    Assert-Equal 0 (Install-Package (Get-PreviousPackagePath $InstalledScope) "context-1.0.14-$InstalledScope-install" $NoTelemetry) 'the exit code of the installation of 1.0.14'
    $previous = (Get-Products)[0]
    $exitCode = Install-Package (Get-PackagePath $NewScope $false) "context-$NewScope-install"
    Write-Host "    OBSERVED: the installation of this version $NewScope beside 1.0.14 $InstalledScope ends with exit code $exitCode"
    $products = Get-Products
    Assert-Equal 0 $exitCode 'the exit code of the installation'
    Assert-Equal 2 $products.Count 'the number of installed products: 1.0.14 is not replaced'
    Assert-True ($null -ne ($products | Where-Object Code -EQ $previous.Code)) "1.0.14 $InstalledScope is still installed"
    Assert-Equal $Scopes[$InstalledScope].InstallFolder (Get-RegistryValue $InstalledScope 'installFolder') "the folder of 1.0.14 $InstalledScope"
    Assert-Equal $Scopes[$NewScope].InstallFolder (Get-RegistryValue $NewScope 'installFolder') "the folder of this version $NewScope"

    Write-Host "    OBSERVED: the choices of this version $NewScope beside 1.0.14: $(Format-Choices (Get-Choices $NewScope))"
    foreach ($product in $products) {
        Assert-Equal 0 (Uninstall-Product $product.Code "context-remove-$($product.Version)" $NoTelemetry) "the exit code of the removal of $($product.Name) $($product.Version)"
    }
    Assert-NoProduct
    Remove-UserData
}

# The packages that carry the .NET runtime install, and the application runs on that runtime.
function Test-SelfContained([string] $Scope) {
    $folder = $Scopes[$Scope].InstallFolder

    Write-Step "$Scope`: the package that carries the .NET runtime"
    Assert-Equal 0 (Install-Package (Get-PackagePath $Scope $true) "$Scope-selfcontained-install") 'the exit code of the installation'
    $product = Assert-Installed $Scope $Version $folder $AllOn $true
    Assert-ApplicationRuns $folder $Version $true
    Assert-Equal 0 (Uninstall-Product $product.Code "$Scope-selfcontained-remove") 'the exit code of the removal'
    Assert-Removed $Scope $folder
    Remove-UserData
}

# --- Run ----------------------------------------------------------------------------------------------------

$watch = [Diagnostics.Stopwatch]::StartNew()

Test-InstallUpgradeRemove 'perMachine'
Test-InstallUpgradeRemove 'perUser'
Test-PublicProperties
Test-UpgradeFromPrevious 'perMachine' "DESKTOPSHORTCUTENABLED=0 APPLICATIONTELEMETRYENABLED=0 $NoTelemetry" ([ordered]@{ desktopShortcutEnabled = ''; programMenuShortcutEnabled = '1'; installerTelemetryEnabled = ''; applicationTelemetryEnabled = '' })
Test-UpgradeFromPrevious 'perUser' "PROGRAMMENUSHORTCUTENABLED=0 $NoTelemetry" ([ordered]@{ desktopShortcutEnabled = '1'; programMenuShortcutEnabled = ''; installerTelemetryEnabled = ''; applicationTelemetryEnabled = '1' })
Show-PreviousRemoval 'perMachine'
Show-PreviousRemoval 'perUser'
Test-OtherContext 'perUser' 'perMachine'
Test-OtherContext 'perMachine' 'perUser'
Test-SelfContained 'perMachine'
Test-SelfContained 'perUser'

Write-Step 'Nothing is left'
Assert-NoProduct
Assert-True (-not (Test-Path $Scopes.perMachine.InstallFolder)) 'no folder of an installation per machine'
Assert-True (-not (Test-Path $Scopes.perUser.InstallFolder)) 'no folder of an installation per user'
Assert-True (-not (Test-Path $ExternalToolFile)) 'no external tool registration'
Assert-True (@(Get-Process Bravo -ErrorAction SilentlyContinue).Count -eq 0) 'no process of the application'

Write-Host ''
Write-Host ("{0} checks passed in {1:N0} s." -f $script:Checks, $watch.Elapsed.TotalSeconds)
