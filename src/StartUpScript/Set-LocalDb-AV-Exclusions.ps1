$dataRoot = if ($env:LocalDBData) { $env:LocalDBData } else { "$env:Temp\LocalDb" }
$instanceRoot = "$env:LocalAppData\Microsoft\Microsoft SQL Server Local DB\Instances"

@($dataRoot, $instanceRoot) | % { Add-MpPreference -ExclusionPath $_ }
