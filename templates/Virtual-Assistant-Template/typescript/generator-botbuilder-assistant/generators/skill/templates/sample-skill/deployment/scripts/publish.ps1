#Requires -Version 6

Param(
	[string] $name,
	[string] $resourceGroup,
    [string] $projFolder = $(Get-Location),
	[string] $logFile = $(Join-Path $PSScriptRoot .. "publish.txt")
)

# Get mandatory parameters
if (-not $name) {
    $name = Read-Host "? Bot Web App Name"
}

if (-not $resourceGroup) {
    $resourceGroup = Read-Host "? Bot Resource Group"
}


# Reset log file
if (Test-Path $logFile) {
	Clear-Content $logFile -Force | Out-Null
}
else {
	New-Item -Path $logFile | Out-Null
}

# Check for existing deployment files
if (-not (Test-Path (Join-Path $projFolder '.deployment'))) {

	# Add needed deployment files for az
	az bot prepare-deploy --code-dir $projFolder --lang Typescript --output json | Out-Null
}

# Delete src zip, if it exists
$zipPath = $(Join-Path $projFolder 'code.zip')
if (Test-Path $zipPath) {
	Remove-Item $zipPath -Force | Out-Null
}

if($?) 
{     
	# Compress source code
	Get-ChildItem -Path "$($projFolder)" -Exclude @("node_modules", "test", "deployment") | Compress-Archive -DestinationPath "$($zipPath)" -Force | Out-Null

	# Publish zip to Azure
	Write-Host "> Publishing to Azure ..." -ForegroundColor Green
	(az webapp deployment source config-zip `
		--resource-group $resourceGroup `
		--name $name `
		--src $zipPath `
        --output json) 2>> $logFile | Out-Null
}
 else
{
	Write-Host "! Could not deploy automatically to Azure. Review the log for more information." -ForegroundColor DarkRed
	Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed    
}       