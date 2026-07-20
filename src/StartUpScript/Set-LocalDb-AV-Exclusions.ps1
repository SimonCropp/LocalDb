# Resolved before elevating. If elevation switches to a different account, the
# environment variables below would otherwise resolve against that account's profile
param(
    $dataRoot = $(if ($env:LocalDBData) { $env:LocalDBData } else { "$env:Temp\LocalDb" }),
    $instanceRoot = "$env:LocalAppData\Microsoft\Microsoft SQL Server Local DB\Instances")

# %Temp% can be an 8.3 short path, which would not match the paths being scanned
$dataRoot = [IO.DirectoryInfo]::new($dataRoot).FullName
$instanceRoot = [IO.DirectoryInfo]::new($instanceRoot).FullName

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$isAdmin = ([Security.Principal.WindowsPrincipal]$identity).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin)
{
    if (-not $PSCommandPath)
    {
        throw 'Adding exclusions needs elevation, and this was not run as a file so it cannot relaunch itself. Run the file, or paste this into an elevated shell.'
    }

    # relaunch the same host elevated, which raises the UAC prompt
    Start-Process -FilePath (Get-Process -Id $PID).Path -Verb RunAs -ArgumentList @(
        '-File', "`"$PSCommandPath`""
        '-dataRoot', "`"$dataRoot`""
        '-instanceRoot', "`"$instanceRoot`"")
    return
}

@($dataRoot, $instanceRoot) | % { Add-MpPreference -ExclusionPath $_ }
