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
if (-not (Test-Path (Join-Path $projFolder 'web.config'))) {

	# Add needed deployment files for az
	az bot prepare-deploy --code-dir $projFolder --lang Typescript
}

# Check for existing deployment configuration
if (-not (Test-Path (Join-Path $projFolder '.deployment'))) {

	# Add needed deployment configuration
	Add-Content -Path $(Join-Path $projFolder ".deployment") -Value @("[config]", "SCM_DO_BUILD_DURING_DEPLOYMENT=true")
}

# Delete src zip, if it exists
$zipPath = $(Join-Path $projFolder 'code.zip')
if (Test-Path $zipPath) {
	Remove-Item $zipPath -Force | Out-Null
}

# Compress source code
Get-ChildItem -Path "$($projFolder)" -Exclude @("node_modules", "test", "deployment") | Compress-Archive -DestinationPath "$($zipPath)" -Force | Out-Null

# Publish zip to Azure
Write-Host "> Publishing to Azure ..."
(az webapp deployment source config-zip `
	--resource-group $resourceGroup `
	--name $name `
	--src $zipPath) 2>> $logFile | Out-Null