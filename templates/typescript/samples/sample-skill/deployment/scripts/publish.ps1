#Requires -Version 6

Param(
	[string] $name,
	[string] $resourceGroup,
    [string] $projFolder = $(Get-Location),
	[string] $logFile = $(Join-Path $PSScriptRoot .. "publish_log.txt")
)

# Reset log file
if (Test-Path $logFile) {
	Clear-Content $logFile -Force | Out-Null
}
else {
	New-Item -Path $logFile | Out-Null
}

# Check for AZ CLI and confirm version
if (Get-Command az -ErrorAction SilentlyContinue) {
    $azcliversionoutput = az -v
    [regex]$regex = '(\d{1,3}.\d{1,3}.\d{1,3})'
    [version]$azcliversion = $regex.Match($azcliversionoutput[0]).value
    [version]$minversion = '2.2.0'

    if ($azcliversion -ge $minversion) {
        $azclipassmessage = "AZ CLI passes minimum version. Current version is $azcliversion"
        Write-Debug $azclipassmessage
        $azclipassmessage | Out-File -Append -FilePath $logfile
    }
    else {
        $azcliwarnmessage = "You are using an older version of the AZ CLI, `
    please ensure you are using version $minversion or newer. `
    The most recent version can be found here: http://aka.ms/installazurecliwindows"
        Write-Warning $azcliwarnmessage
        $azcliwarnmessage | Out-File -Append -FilePath $logfile
    }
}
else {
    $azclierrormessage = 'AZ CLI not found. Please install latest version.'
    Write-Error $azclierrormessage
    $azclierrormessage | Out-File -Append -FilePath $logfile
}

# Get mandatory parameters
if (-not $name) {
    $name = Read-Host "? Bot Web App Name"
}

if (-not $resourceGroup) {
    $resourceGroup = Read-Host "? Bot Resource Group"
}

# Check for existing deployment files
if (-not (Test-Path (Join-Path $projFolder 'web.config'))) {

	# Add needed deployment files for az
	az bot prepare-deploy --code-dir $projFolder --lang Typescript --output json | Out-Null
}

# Check for existing deployment configuration
if (-not (Test-Path (Join-Path $projFolder '.deployment'))) {

	# Add needed deployment configuration
	Add-Content -Path $(Join-Path $projFolder ".deployment") -Value @("[config]", "SCM_DO_BUILD_DURING_DEPLOYMENT=true") -Encoding utf8
}

# Delete src zip, if it exists
$zipPath = $(Join-Path $projFolder 'code.zip')
if (Test-Path $zipPath) {
	Remove-Item $zipPath -Force | Out-Null
}

if($?)
{
	# Install dependencies locally
	Invoke-Expression "npm install"

	# Build the project
	Invoke-Expression "npm run build"

	# Compress source code
	Get-ChildItem -Path "$($projFolder)" -Exclude @("node_modules", "test", "deployment") | Compress-Archive -DestinationPath "$($zipPath)" -Force | Out-Null

    # Publish zip to Azure
    Write-Host "> Publishing to Azure ..." -NoNewline
    Invoke-Expression "az webapp deployment source config-zip --resource-group $($resourceGroup) --name $($name) --src '$($zipPath)' --output json" -ErrorVariable publishError -OutVariable publishOutput 2>&1 | Out-Null
    Add-Content $logFile $publishOutput | Out-Null
    Add-Content $logFile $publishError | Out-Null

    $err = $publishError | Where { $_.Exception.ErrorRecord -like "*ERROR*" }

    if ($err)
    {
        Write-Host "! Could not deploy automatically to Azure. Review the log for more information." -ForegroundColor DarkRed
        Write-Host "! Error: $($err.Exception.ErrorRecord)" -ForegroundColor DarkRed
	    Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed
    }
    else {
        Write-Host "Done." -ForegroundColor Green
    }
}
else {
	Write-Host "! Could not deploy automatically to Azure. Review the log for more information." -ForegroundColor DarkRed
	Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed
}