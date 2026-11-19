<#
.SYNOPSIS
    Installs, upgrades and removes the installer packages for real, and checks what each step leaves.

.DESCRIPTION
    The script changes the machine it runs on: it installs and removes Bravo, per machine and per user, and
    writes in the data folder of the application. It runs on a runner of GitHub Actions and nowhere else.

    Every step is a silent installation, as a deployment tool runs it. The log of each run of Windows
    Installer is kept in the log folder.

.PARAMETER PackagesFolder
    The folder with the four packages under test.

.PARAMETER PreviousFolder
    The folder with the packages of 1.0.14 that use the installed runtime, per machine and per user.

.PARAMETER Version
    The semantic version of the packages under test, which names their files and is in the title of the
    window, such as 1.1.0 or 1.1.0-beta.1.

.PARAMETER FileVersion
    The version of the files of the application, which is the version of the product, such as 1.1.0.185.

.PARAMETER SignerThumbprint
    The thumbprint of the certificate that the build signed with. When it is given, the test checks that the
    installed files of the host carry its signature.

.PARAMETER LogFolder
    Where the logs of Windows Installer go.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $PackagesFolder,
    [Parameter(Mandatory)] [string] $PreviousFolder,
    [Parameter(Mandatory)] [string] $Version,
    [Parameter(Mandatory)] [string] $FileVersion,
    [string] $SignerThumbprint = '',
    [Parameter(Mandatory)] [string] $LogFolder
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($env:GITHUB_ACTIONS -ne 'true') {
    throw 'This script installs and removes Bravo. It runs on a runner of GitHub Actions only.'
}

# The packages that use the installed runtime need the three runtimes of .NET 10, which the image of the
# runner has: nothing is installed for them.
$installedRuntimes = dotnet --list-runtimes
foreach ($runtime in 'Microsoft.NETCore.App', 'Microsoft.AspNetCore.App', 'Microsoft.WindowsDesktop.App') {
    if (-not ($installedRuntimes -match "^$runtime 10\.")) { throw "The runner has no $runtime 10." }
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

function Get-PackagePath([string] $Scope, [bool] $SelfContained) {
    $runtime = if ($SelfContained) { '' } else { '-frameworkdependent' }
    $path = Join-Path $PackagesFolder "Bravo.$Version.x64$runtime$($Scopes[$Scope].Suffix).msi"
    if (-not (Test-Path $path)) { throw "The package is missing: $path" }
    return $path
}

function Get-PreviousPackagePath([string] $Scope) {
    $path = Join-Path $PreviousFolder "Bravo.1.0.14.x64-frameworkdependent$($Scopes[$Scope].Suffix).msi"
    if (-not (Test-Path $path)) { throw "The package of 1.0.14 is missing: $path" }
    return $path
}

# Runs Windows Installer with no dialog and no restart, and returns its exit code. ExpectedFailure names an
# action that the test itself makes fail.
function Invoke-WindowsInstaller([string] $Arguments, [string] $Name, [string] $ExpectedFailure = '') {
    $script:LogNumber++
    $log = Join-Path $LogFolder ('{0:00}-{1}.log' -f $script:LogNumber, $Name)
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $process = Start-Process -FilePath 'msiexec.exe' -ArgumentList "$Arguments /qn /norestart /l*v `"$log`"" -Wait -PassThru
    Write-Host ("    msiexec {0} -> exit code {1} in {2:N0} s" -f $Arguments, $process.ExitCode, $watch.Elapsed.TotalSeconds)

    # The actions that run a program, in the order in which the log says that they went into the script. The
    # log has no such line for the moment a commit action or a rollback action runs.
    $actions = @(Select-String -Path $log -Pattern 'Executing op: CustomActionSchedule\(Action=([^,]+),' |
        ForEach-Object { $_.Matches[0].Groups[1].Value })
    if ($actions.Count -gt 0) { Write-Host "    actions in the script: $($actions -join ', ')" }

    # A package ignores the exit code of what it runs, so a command that failed shows in the log alone.
    $failed = @(Select-String -Path $log -Pattern 'returned actual error code' | ForEach-Object { $_.Line })
    if ($ExpectedFailure -ne '') {
        $expected = "*CustomAction $ExpectedFailure returned actual error code*"
        Assert-True (@($failed -like $expected).Count -eq 1) "the action $ExpectedFailure failed, once"
        $failed = @($failed -notlike $expected)
    }
    if ($failed.Count -gt 0) {
        throw "An action of the package failed:`n  $($failed -join "`n  ")"
    }

    return $process.ExitCode
}

function Install-Package([string] $Path, [string] $Name, [string] $Properties = '') {
    $arguments = "/i `"$Path`" $Properties".TrimEnd()
    return Invoke-WindowsInstaller $arguments $Name
}

function Uninstall-Product([string] $ProductCode, [string] $Name, [string] $Properties = '', [string] $ExpectedFailure = '') {
    $arguments = "/x $ProductCode $Properties".TrimEnd()
    return Invoke-WindowsInstaller $arguments $Name $ExpectedFailure
}

# 1.0.14 sends the events of its installer for real, at a removal whatever was chosen at the installation.
# On the runner every run of its packages is told not to.
$NoTelemetry = 'INSTALLERTELEMETRYENABLED=0'

# Installs a package of 1.0.14, which is the only product then, and returns that product.
function Install-Previous([string] $Scope, [string] $Name, [string] $Properties = '') {
    if ($Properties -notmatch 'INSTALLERTELEMETRYENABLED=') { $Properties = "$Properties $NoTelemetry".Trim() }
    $exitCode = Install-Package (Get-PreviousPackagePath $Scope) $Name $Properties
    Assert-Equal 0 $exitCode 'the exit code of the installation of 1.0.14'
    return (Get-Products)[0]
}

function Uninstall-Previous($Product, [string] $Name) {
    Assert-Equal 0 (Uninstall-Product $Product.Code $Name $NoTelemetry) 'the exit code of the removal of 1.0.14'
}

$script:Installer = New-Object -ComObject WindowsInstaller.Installer

function Get-InstallerProperty([string] $Name, [object[]] $Arguments) {
    return $script:Installer.GetType().InvokeMember($Name, 'GetProperty', $null, $script:Installer, $Arguments)
}

function Invoke-ComMethod($Object, [string] $Name, [object[]] $Arguments = @()) {
    return $Object.GetType().InvokeMember($Name, 'InvokeMethod', $null, $Object, $Arguments)
}

# Runs a statement on the tables of a package and returns the first number of its first row, when it has one.
function Invoke-PackageStatement($Database, [string] $Sql) {
    $view = Invoke-ComMethod $Database 'OpenView' @($Sql)
    try {
        Invoke-ComMethod $view 'Execute' | Out-Null
        if ($Sql -notlike 'SELECT *') { return }
        $record = Invoke-ComMethod $view 'Fetch'
        return $record.GetType().InvokeMember('IntegerData', 'GetProperty', $null, $record, @([int] 1))
    }
    finally {
        Invoke-ComMethod $view 'Close' | Out-Null
        [Runtime.InteropServices.Marshal]::FinalReleaseComObject($view) | Out-Null
    }
}

# The action that the test adds to a copy of a package, and the property that turns it on.
$FailingAction = 'FailRemoval'
$FailRemoval = 'FAILREMOVAL=1'

# A copy of a package, changed by the script block, which is given its tables. What a test needs of a package
# that the package does not have is added here, to the copy: no package that the build makes has it.
function New-ChangedPackage([string] $Package, [string] $Name, [scriptblock] $Change) {
    $copy = Join-Path ([IO.Path]::GetTempPath()) "$Name-$([IO.Path]::GetFileName($Package))"
    Copy-Item $Package $copy -Force

    # 1: the tables are changed in a transaction.
    $database = Invoke-ComMethod $script:Installer 'OpenDatabase' @([string] $copy, [int] 1)
    try {
        & $Change $database
        Invoke-ComMethod $database 'Commit' | Out-Null
    }
    finally {
        # Windows Installer opens the file only after this process has let it go.
        [Runtime.InteropServices.Marshal]::FinalReleaseComObject($database) | Out-Null
        [GC]::Collect()
        [GC]::WaitForPendingFinalizers()
    }
    return $copy
}

# A copy of a package whose removal fails when FAILREMOVAL=1 is on its command line: a program that ends with
# an error, in the script, as the last step before the end, when the files are removed already.
function New-PackageWithFailingRemoval([string] $Package) {
    return New-ChangedPackage $Package 'failing-removal' {
        param($database)
        $end = Invoke-PackageStatement $database 'SELECT `Sequence` FROM `InstallExecuteSequence` WHERE `Action` = ''InstallFinalize'''

        # 1074: a program that a property names (50), run in the script (1024) as who removes. Its exit code
        # is not ignored. An action of the package with the same number is one of the last of the script too.
        Invoke-PackageStatement $database ('INSERT INTO `Property` (`Property`, `Value`) VALUES (''FailRemovalProgram'', ''{0}'')' -f $env:ComSpec)
        Invoke-PackageStatement $database ('INSERT INTO `CustomAction` (`Action`, `Type`, `Source`, `Target`) VALUES (''{0}'', 1074, ''FailRemovalProgram'', ''/c exit 1'')' -f $FailingAction)
        Invoke-PackageStatement $database ('INSERT INTO `InstallExecuteSequence` (`Action`, `Condition`, `Sequence`) VALUES (''{0}'', ''REMOVE="ALL" AND FAILREMOVAL="1"'', {1})' -f $FailingAction, ($end - 1))
    }
}

function New-PackageCode {
    return "{$([guid]::NewGuid().ToString().ToUpperInvariant())}"
}

# A copy of a package as another build of the same commit makes it: the same version and the same files,
# another product code and another package code, which are what each build gives a package.
function New-OtherBuild([string] $Package) {
    return New-ChangedPackage $Package 'other-build' {
        param($database)
        Invoke-PackageStatement $database ('UPDATE `Property` SET `Value` = ''{0}'' WHERE `Property` = ''ProductCode''' -f (New-PackageCode))

        # 9: the revision number of the summary information, which is the package code. 1: one value changes.
        $summary = $database.GetType().InvokeMember('SummaryInformation', 'GetProperty', $null, $database, @([int] 1))
        try {
            $summary.GetType().InvokeMember('Property', 'SetProperty', $null, $summary, @([int] 9, [string] (New-PackageCode))) | Out-Null
            Invoke-ComMethod $summary 'Persist' | Out-Null
        }
        finally {
            [Runtime.InteropServices.Marshal]::FinalReleaseComObject($summary) | Out-Null
        }
    }
}

# The installed products of the UpgradeCode, of both installation contexts.
function Get-Products {
    $products = @()
    foreach ($code in @(Get-InstallerProperty 'RelatedProducts' @($UpgradeCode))) {
        # InstalledProductName and VersionString: what Windows Installer registered for the installed product,
        # from the properties that it also writes for Installed apps.
        $product = [pscustomobject]@{
            Code          = $code
            Name          = Get-InstallerProperty 'ProductInfo' @($code, 'ProductName')
            InstalledName = Get-InstallerProperty 'ProductInfo' @($code, 'InstalledProductName')
            Version       = Get-InstallerProperty 'ProductInfo' @($code, 'VersionString')
            Assignment    = Get-InstallerProperty 'ProductInfo' @($code, 'AssignmentType')
        }
        Write-Host "    installed: $($product.Name) $($product.Version) $($product.Code) assignment=$($product.Assignment)"
        $products += $product
    }
    return , $products
}

function Assert-NoProduct {
    $products = Get-Products
    Assert-Equal 0 $products.Count 'the number of installed products'
}

function Assert-SingleProduct([string] $Scope) {
    $products = Get-Products
    Assert-Equal 1 $products.Count 'the number of installed products'
    Assert-Equal $Scopes[$Scope].ProductName $products[0].Name 'the name of the product'
    Assert-Equal $Scopes[$Scope].ProductName $products[0].InstalledName 'the name in Installed apps'
    Assert-Equal $FileVersion $products[0].Version 'the version of the product, the one in Installed apps'
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

# The choices as a package writes them, in the order of the names: 1 for the ones named, nothing for the others.
function New-Choices([string[]] $On) {
    $choices = [ordered]@{}
    foreach ($name in $ChoiceNames) { $choices[$name] = if ($On -contains $name) { '1' } else { '' } }
    return $choices
}

function Get-ShortcutTarget([string] $Folder, [string] $Scope) {
    $path = Join-Path $Folder "$($Scopes[$Scope].ProductName).lnk"
    if (-not (Test-Path $path)) { return $null }
    return (New-Object -ComObject WScript.Shell).CreateShortcut($path).TargetPath
}

function Read-ExternalTool {
    if (-not (Test-Path $ExternalToolFile)) { return $null }
    return Get-Content $ExternalToolFile -Raw | ConvertFrom-Json
}

# Everything that a package of this version installs, with the choices given.
function Assert-Installed([string] $Scope, [string] $InstallFolder, $ExpectedChoices, [bool] $SelfContained) {
    $product = Assert-SingleProduct $Scope
    $executable = Join-Path $InstallFolder 'Bravo.exe'

    Assert-Equal $FileVersion (Get-Item $executable).VersionInfo.FileVersion 'the version of the file of Bravo.exe'
    foreach ($file in 'Bravo.dll', 'WebView2Loader.dll', 'wwwroot\index.html', 'wwwroot\js\main.js', 'wwwroot\css\main.css') {
        Assert-True (Test-Path (Join-Path $InstallFolder $file)) "$file is installed"
    }
    if ($SignerThumbprint -ne '') {
        # The files of the host: the package holds the ones that the build signed, and not older ones.
        $hostFiles = @($executable) + @((Get-ChildItem -Path $InstallFolder -Filter 'Bravo*.dll').FullName)
        & (Join-Path $PSScriptRoot '..\Confirm-Signature.ps1') -Path $hostFiles -Thumbprint $SignerThumbprint | Out-Null
        Assert-True $true "the $($hostFiles.Count) installed files of the host carry the signature of the build"
    }
    foreach ($entry in 'Bravo.Setup.exe', 'Bravo.xml', 'runtimes', 'it') {
        Assert-True (-not (Test-Path (Join-Path $InstallFolder $entry))) "$entry is not installed"
    }
    Assert-Equal $SelfContained (Test-Path (Join-Path $InstallFolder 'coreclr.dll')) 'the package carries the .NET runtime'

    Assert-Equal $InstallFolder (Get-RegistryValue $Scope 'installFolder') 'the registry value installFolder'
    Assert-Equal (Format-Choices $ExpectedChoices) (Format-Choices (Get-Choices $Scope)) 'the choices in the registry'

    $desktopTarget = if ($ExpectedChoices.desktopShortcutEnabled -eq '1') { $executable } else { $null }
    $programMenuTarget = if ($ExpectedChoices.programMenuShortcutEnabled -eq '1') { $executable } else { $null }
    Assert-Equal $desktopTarget (Get-ShortcutTarget $Scopes[$Scope].Desktop $Scope) 'the target of the desktop shortcut'
    Assert-Equal $programMenuTarget (Get-ShortcutTarget $Scopes[$Scope].ProgramMenu $Scope) 'the target of the Start menu shortcut'

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

$UserDataFiles = [ordered]@{
    'usersettings.json' = '{ "marker": "settings of the test" }'
    'any-file.txt'      = 'a file of the test'
}

function Write-UserData {
    New-Item -ItemType Directory -Force $DataFolder | Out-Null
    foreach ($name in $UserDataFiles.Keys) { Set-Content -Path (Join-Path $DataFolder $name) -Value $UserDataFiles[$name] }
}

# A file that was emptied has no text to read: it is not as it was.
function Test-UserData {
    foreach ($name in $UserDataFiles.Keys) {
        $path = Join-Path $DataFolder $name
        if (-not (Test-Path $path)) { return $false }
        $text = Get-Content $path -Raw
        if ($null -eq $text -or $text.Trim() -cne $UserDataFiles[$name]) { return $false }
    }
    return $true
}

function Remove-UserData {
    if (Test-Path $DataFolder) { Remove-Item $DataFolder -Recurse -Force }
}

# The value with which a user of 1.0.x hides the version in the title of the window. No package writes it,
# and a removal leaves it.
$TitleKey = Join-Path 'HKCU:' $RegistryKey
$TitleVersionHiddenName = 'applicationTitleVersionHidden'

function Set-TitleVersionHidden {
    if (-not (Test-Path $TitleKey)) { New-Item -Path $TitleKey -Force | Out-Null }
    New-ItemProperty -Path $TitleKey -Name $TitleVersionHiddenName -PropertyType DWord -Value 1 -Force | Out-Null
}

function Remove-TitleVersionHidden {
    Remove-ItemProperty -Path $TitleKey -Name $TitleVersionHiddenName -ErrorAction SilentlyContinue
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
function Assert-ApplicationRuns([string] $InstallFolder, [bool] $SelfContained, [string] $Title = "Bravo for Power BI - v$Version") {
    $process = Start-Application $InstallFolder
    try {
        Assert-Equal $Title $process.MainWindowTitle 'the title of the window, the one of a release build'
        $runtime = ($process.Modules | Where-Object { $_.ModuleName -eq 'coreclr.dll' }).FileName
        $carried = $runtime.StartsWith($InstallFolder, [StringComparison]::OrdinalIgnoreCase)
        Assert-Equal $SelfContained $carried "the .NET runtime is the one of the package ($runtime)"
        $process.CloseMainWindow() | Out-Null
    }
    catch {
        if (-not $process.HasExited) { $process.Kill() }
        throw
    }
    Assert-ApplicationEnds $process 'the application ends when its window is closed'
}

# The application ends by itself, with success, and the processes of its webview with it.
function Assert-ApplicationEnds($Process, [string] $What) {
    try {
        Assert-True ($Process.WaitForExit(15000)) $What
        Assert-Equal 0 $Process.ExitCode 'the exit code of the application'
    }
    finally {
        if (-not $Process.HasExited) { $Process.Kill() }
    }
    Wait-WebViewEnd
}

# --- Scenarios ----------------------------------------------------------------------------------------------

$AllOn = New-Choices $ChoiceNames

# A first installation, the application, an upgrade while the application runs, the removal.
function Test-InstallUpgradeRemove([string] $Scope) {
    $folder = $Scopes[$Scope].InstallFolder

    Write-Step "$Scope`: a first installation"
    Remove-UserData
    Assert-NoProduct
    Assert-Equal 0 (Install-Package (Get-PackagePath $Scope $false) "$Scope-install") 'the exit code of the installation'
    $first = Assert-Installed $Scope $folder $AllOn $false

    Write-Step "$Scope`: the installed application starts and ends"
    Assert-ApplicationRuns $folder $false

    # The other build has the version of the installed one: Windows Installer compares three numbers, and
    # the package replaces a product of its own version, as a release replaces a preview of it.
    Write-Step "$Scope`: another build of this version is installed while the application runs"
    Write-UserData
    $other = New-OtherBuild (Get-PackagePath $Scope $false)
    $running = Start-Application $folder
    try {
        $exitCode = Install-Package $other "$Scope-upgrade-while-running"
        Assert-Equal 0 $exitCode 'the exit code of the upgrade: no restart of Windows is asked for'
    }
    catch {
        if (-not $running.HasExited) { $running.Kill() }
        throw
    }
    Remove-Item $other
    Assert-ApplicationEnds $running 'the running application was closed'
    Assert-True (@(Get-Process Bravo -ErrorAction SilentlyContinue).Count -eq 0) 'the application is not started again'
    $product = Assert-Installed $Scope $folder $AllOn $false
    Assert-True ($product.Code -ne $first.Code) 'the other build replaced the first one: it is another product'
    Assert-True (Test-UserData) 'the data of the user is as it was'

    Write-Step "$Scope`: the removal"
    Assert-Equal 0 (Uninstall-Product $product.Code "$Scope-remove") 'the exit code of the removal'
    Assert-Removed $Scope $folder
    Assert-True (-not (Test-Path $DataFolder)) 'the removal removes the data folder of who removes, as the removal of 1.0.14 does'
}

# The public properties of 1.0.x on the command line: the same line gives 1.0.14 and this version the same
# choices, shortcuts and folder.
function Test-PublicProperties {
    $folder = 'C:\Bravo Custom\'
    $properties = "INSTALLFOLDER=`"$($folder.TrimEnd('\'))`" DESKTOPSHORTCUTENABLED=0 PROGRAMMENUSHORTCUTENABLED=1 $NoTelemetry APPLICATIONTELEMETRYENABLED=no TELEMETRYUSERID=someone"
    $expected = New-Choices 'programMenuShortcutEnabled'

    Write-Step 'perMachine: the public properties, given to 1.0.14'
    $previous = Install-Previous 'perMachine' 'properties-1.0.14-install' $properties
    $previousChoices = Format-Choices (Get-Choices 'perMachine')
    $previousFolder = Get-RegistryValue 'perMachine' 'installFolder'
    $previousDesktop = $null -ne (Get-ShortcutTarget $Scopes.perMachine.Desktop 'perMachine')
    $previousProgramMenu = $null -ne (Get-ShortcutTarget $Scopes.perMachine.ProgramMenu 'perMachine')
    Write-Host "    1.0.14: folder='$previousFolder' $previousChoices desktop=$previousDesktop programMenu=$previousProgramMenu"
    Uninstall-Previous $previous 'properties-1.0.14-remove'
    Assert-NoProduct

    Write-Step 'perMachine: the same public properties, given to this version'
    Assert-Equal 0 (Install-Package (Get-PackagePath 'perMachine' $false) 'properties-install' $properties) 'the exit code of the installation'
    $product = Assert-Installed 'perMachine' $folder $expected $false
    Assert-Equal $previousChoices (Format-Choices (Get-Choices 'perMachine')) 'the choices, as 1.0.14 wrote them'
    Assert-Equal $previousFolder (Get-RegistryValue 'perMachine' 'installFolder') 'the folder, as 1.0.14 wrote it'
    Assert-Equal $previousDesktop ($null -ne (Get-ShortcutTarget $Scopes.perMachine.Desktop 'perMachine')) 'a desktop shortcut, as with 1.0.14'
    Assert-Equal $previousProgramMenu ($null -ne (Get-ShortcutTarget $Scopes.perMachine.ProgramMenu 'perMachine')) 'a Start menu shortcut, as with 1.0.14'

    Assert-Equal 0 (Uninstall-Product $product.Code 'properties-remove') 'the exit code of the removal'
    Assert-Removed 'perMachine' $folder
}

# 1.0.14 installed with some choices turned off, the data of a user with the version hidden in the title, then
# this version over it.
function Test-UpgradeFromPrevious([string] $Scope, [string] $Properties, $ExpectedChoices) {
    $folder = $Scopes[$Scope].InstallFolder

    Write-Step "$Scope`: 1.0.14 is installed, with $Properties"
    Remove-UserData
    $previous = Install-Previous $Scope "$Scope-1.0.14-install" $Properties
    Assert-Equal '1.0.9805.22885' $previous.Version 'the version of the product of 1.0.14'
    Assert-Equal $folder (Get-RegistryValue $Scope 'installFolder') 'the folder of 1.0.14'
    $previousChoices = Format-Choices (Get-Choices $Scope)
    Write-Host "    1.0.14: $previousChoices"
    Write-UserData
    Set-TitleVersionHidden

    Write-Step "$Scope`: this version is installed over 1.0.14"
    Assert-Equal 0 (Install-Package (Get-PackagePath $Scope $false) "$Scope-upgrade-from-1.0.14") 'the exit code of the upgrade'
    $product = Assert-Installed $Scope $folder $ExpectedChoices $false
    Assert-True ($product.Code -ne $previous.Code) '1.0.14 is no longer installed'
    Assert-Equal $previousChoices (Format-Choices (Get-Choices $Scope)) 'the choices, as 1.0.14 wrote them'
    Assert-True (Test-UserData) 'the data of the user is as it was'

    Write-Step "$Scope`: the upgraded application starts and ends, with the version hidden in its title"
    Assert-ApplicationRuns $folder $false 'Bravo for Power BI'
    Assert-True (Test-UserData) 'the data of the user is as it was'
    Remove-TitleVersionHidden

    Assert-Equal 0 (Uninstall-Product $product.Code "$Scope-remove-after-upgrade") 'the exit code of the removal'
    Assert-Removed $Scope $folder
    Remove-UserData
}

# What the removal of 1.0.14 does to the data of the user: what a user of 1.0.x sees today.
function Show-PreviousRemoval([string] $Scope) {
    Write-Step "$Scope`: what the removal of 1.0.14 leaves of the data of the user"
    Remove-UserData
    $previous = Install-Previous $Scope "$Scope-1.0.14-install-for-removal"
    Write-UserData
    Uninstall-Previous $previous "$Scope-1.0.14-remove"
    Assert-NoProduct
    Write-Host "    OBSERVED: after the removal of 1.0.14 $Scope, the data folder exists: $(Test-Path $DataFolder); the data is as it was: $(Test-UserData)"
    Remove-UserData
}

# A package never upgrades an installation of the other context: both stay installed.
function Test-OtherContext([string] $InstalledScope, [string] $NewScope) {
    Write-Step "1.0.14 $InstalledScope is installed, then this version $NewScope"
    $previous = Install-Previous $InstalledScope "context-1.0.14-$InstalledScope-install"
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
    $product = Assert-Installed $Scope $folder $AllOn $true
    Assert-ApplicationRuns $folder $true
    Assert-Equal 0 (Uninstall-Product $product.Code "$Scope-selfcontained-remove") 'the exit code of the removal'
    Assert-Removed $Scope $folder
    Remove-UserData
}

# What a removal cannot take back is done when the removal has succeeded, and all the same where Windows
# Installer takes nothing back.
function Test-RemovalThatFails([string] $Scope) {
    $folder = $Scopes[$Scope].InstallFolder

    Write-Step "$Scope`: a removal that fails after the files are removed"
    Remove-UserData
    $package = New-PackageWithFailingRemoval (Get-PackagePath $Scope $false)
    Assert-Equal 0 (Install-Package $package "$Scope-install-for-failing-removal") 'the exit code of the installation'
    $product = Assert-Installed $Scope $folder $AllOn $false
    Write-UserData
    $exitCode = Uninstall-Product $product.Code "$Scope-remove-and-fail" $FailRemoval $FailingAction
    Assert-Equal 1603 $exitCode 'the exit code of the removal that fails'
    Assert-Installed $Scope $folder $AllOn $false | Out-Null
    Assert-True (Test-UserData) 'the data of the user is as it was'

    Write-Step "$Scope`: a removal with rollback disabled"
    Assert-Equal 0 (Uninstall-Product $product.Code "$Scope-remove-without-rollback" 'DISABLEROLLBACK=1') 'the exit code of the removal'
    Assert-Removed $Scope $folder
    Assert-True (-not (Test-Path $DataFolder)) 'the removal removes the data folder of who removes'
    Remove-Item $package
}

# --- Run ----------------------------------------------------------------------------------------------------

$watch = [Diagnostics.Stopwatch]::StartNew()

Test-InstallUpgradeRemove 'perMachine'
Test-InstallUpgradeRemove 'perUser'
Test-PublicProperties
Test-UpgradeFromPrevious 'perMachine' 'DESKTOPSHORTCUTENABLED=0 APPLICATIONTELEMETRYENABLED=0' (New-Choices 'programMenuShortcutEnabled')
Test-UpgradeFromPrevious 'perUser' 'PROGRAMMENUSHORTCUTENABLED=0' (New-Choices 'desktopShortcutEnabled', 'applicationTelemetryEnabled')
Show-PreviousRemoval 'perMachine'
Show-PreviousRemoval 'perUser'
Test-OtherContext 'perUser' 'perMachine'
Test-OtherContext 'perMachine' 'perUser'
Test-SelfContained 'perMachine'
Test-SelfContained 'perUser'
Test-RemovalThatFails 'perMachine'
Test-RemovalThatFails 'perUser'

Write-Step 'Nothing is left'
Assert-NoProduct
Assert-True (-not (Test-Path $Scopes.perMachine.InstallFolder)) 'no folder of an installation per machine'
Assert-True (-not (Test-Path $Scopes.perUser.InstallFolder)) 'no folder of an installation per user'
Assert-True (-not (Test-Path $ExternalToolFile)) 'no external tool registration'
Assert-True (@(Get-Process Bravo -ErrorAction SilentlyContinue).Count -eq 0) 'no process of the application'

Write-Host ''
Write-Host ("{0} checks passed in {1:N0} s." -f $script:Checks, $watch.Elapsed.TotalSeconds)
