#Requires -Version 6

Param(
	[string] $name,
	[string] $resourceGroup,
    [string] $projFolder = $(Get-Location),
	[string] $logFile = $(Join-Path $PSScriptRoot .. "publish.txt")
)

# Reset log file
if (Test-Path $logFile) {
	Clear-Content $logFile -Force | Out-Null
}
else {
	New-Item -Path $logFile | Out-Null
}

# Check for existing deployment files
if (-not (Test-Path (Join-Path $projFolder '.deployment'))) {

	# Get path to csproj file
	$projFile = Get-ChildItem $prjFolder `
		| Where-Object {$_.extension -eq ".csproj" } `
		| Select-Object -First 1

	# Add needed deployment files for az
	az bot prepare-deploy --lang Csharp --code-dir $projFolder --proj-file-path $projFile.name
}

# Delete src zip, if it exists
$zipPath = $(Join-Path $projFolder 'code.zip')
if (Test-Path $zipPath) {
	Remove-Item $zipPath -Force | Out-Null
}

# Compress source code
Get-ChildItem -Path "$($projFolder)" | Compress-Archive -DestinationPath "$($zipPath)" -Force | Out-Null

# Publish zip to Azure
Write-Host "> Publishing to Azure ..."
(az webapp deployment source config-zip `
	--resource-group $resourceGroup `
	--name $name `
	--src $zipPath) 2>> $logFile | Out-Null