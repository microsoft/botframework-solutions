#Requires -Version 6

Param(
    [string] $name,
	[string] $resourceGroup,
    [string] $location,
    [string] $parametersFile,
    [switch] $useGov,
	[string] $languages = "en-us",
    [string] $qnaEndpoint = "https://westus.api.cognitive.microsoft.com/qnamaker/v4.0",
	[string] $projDir = $(Get-Location),
	[string] $logFile = $(Join-Path $PSScriptRoot .. "deploy_log.txt")
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
    $name = Read-Host "? Bot Name (used as default name for resource group and deployed resources)"
}

if (-not $resourceGroup) {
	$resourceGroup = $name
}

if (-not $location) {
    $location = Read-Host "? Azure resource group region"
}

# Get timestamp
$timestamp = Get-Date -f MMddyyyyHHmmss

# Deploy Azure services (deploys LUIS, QnA Maker, Content Moderator, CosmosDB)
if ($parametersFile) {
	Write-Host "> Validating Azure deployment ..." -NoNewline
	$validation = az deployment group validate `
		--resource-group $resourcegroup `
		--template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'template.json')" `
        --output json

	if ($validation) {    
		$validation >> $logFile
		$validation = $validation | ConvertFrom-Json
	
		if (-not $validation.error) {
            Write-Host "Done." -ForegroundColor Green
			Write-Host "> Deploying Azure services (this could take a while)..." -ForegroundColor Yellow -NoNewline
			$deployment = az deployment group create `
				--name $timestamp `
				--resource-group $resourceGroup `
				--template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'template.json')" `
                --output json 2>> $logFile | Out-Null

            Write-Host "Done." -ForegroundColor Green
		}
		else {
			Write-Host "! Template is not valid with provided parameters. Review the log for more information." -ForegroundColor Red
			Write-Host "! Error: $($validation.error.message)"  -ForegroundColor Red
			Write-Host "! Log: $($logFile)" -ForegroundColor Red
			Write-Host "+ To delete this resource group, run 'az group delete -g $($resourceGroup) --no-wait'" -ForegroundColor Magenta
			Break
		}
	}
}
else {
	Write-Host "> Validating Azure deployment ..." -NoNewline
	$validation = az deployment group validate `
		--resource-group $resourcegroup `
		--template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'template.json')" `
        --output json

	if ($validation) {
		$validation >> $logFile
		$validation = $validation | ConvertFrom-Json

		if (-not $validation.error) {
            Write-Host "Done." -ForegroundColor Green
			Write-Host "> Deploying Azure services (this could take a while)..." -ForegroundColor Yellow -NoNewline
			$deployment = az deployment group create `
				--name $timestamp `
				--resource-group $resourceGroup `
				--template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'template.json')" `
                --output json 2>> $logFile | Out-Null

            Write-Host "Done." -ForegroundColor Green
		}
		else {
			Write-Host "! Template is not valid with provided parameters. Review the log for more information." -ForegroundColor Red
			Write-Host "! Error: $($validation.error.message)"  -ForegroundColor Red
			Write-Host "! Log: $($logFile)" -ForegroundColor Red
			Write-Host "+ To delete this resource group, run 'az group delete -g $($resourceGroup) --no-wait'" -ForegroundColor Magenta
			Break
		}
	}
}

# Get deployment outputs
$outputs = (az deployment group show `
	--name $timestamp `
	--resource-group $resourceGroup `
	--query properties.outputs `
    --output json) 2>> $logFile

# If it succeeded then we perform the remainder of the steps
if ($outputs)
{
	# Log and convert to JSON
	$outputs >> $logFile
	$outputs = $outputs | ConvertFrom-Json
	$outputMap = @{}
	$outputs.PSObject.Properties | Foreach-Object { $outputMap[$_.Name] = $_.Value }

	if ($outputs.qnaMaker.value.key) { $qnaSubscriptionKey = $outputs.qnaMaker.value.key }

	# Delay to let QnA Maker finish setting up
	Start-Sleep -s 30

	# Deploy cognitive models
    if ($useGov) {
        Invoke-Expression "& '$(Join-Path $PSScriptRoot 'deploy_cognitive_models.ps1')' -name $($name) -resourceGroup $($resourceGroup) -outFolder '$($projDir)' -languages '$($languages)' -qnaSubscriptionKey '$($qnaSubscriptionKey)' -qnaEndpoint '$($qnaEndpoint)' -useGov"
    }
    else {
        Invoke-Expression "& '$(Join-Path $PSScriptRoot 'deploy_cognitive_models.ps1')' -name $($name) -resourceGroup $($resourceGroup) -outFolder '$($projDir)' -languages '$($languages)' -qnaSubscriptionKey '$($qnaSubscriptionKey)' -qnaEndpoint '$($qnaEndpoint)'"
    }

}