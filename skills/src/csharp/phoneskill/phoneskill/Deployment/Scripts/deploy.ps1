#Requires -Version 6

Param(
    [string] $name,
	[string] $resourceGroup,
    [string] $location,
	[string] $appId,
    [string] $appPassword,
    [string] $luisAuthoringKey,
	[string] $luisAuthoringRegion,
    [string] $parametersFile,
	[string] $languages = "en-us",
	[string] $outFolder = $(Get-Location),
	[string] $logFile = $(Join-Path $PSScriptRoot .. "deploy_log.txt")
)

# Reset log file
if (Test-Path $logFile) {
	Clear-Content $logFile -Force | Out-Null
}
else {
	New-Item -Path $logFile | Out-Null
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

if (-not $appPassword) {
    $appPassword = Read-Host "? Password for MSA app registration (must be at least 16 characters long, contain at least 1 special character, and contain at least 1 numeric character)"
}

if (-not $luisAuthoringRegion) {
    $luisAuthoringRegion = Read-Host "? LUIS Authoring Region (westus, westeurope, or australiaeast)"
}

if (-not $luisAuthoringKey) {
	Switch ($luisAuthoringRegion) {
		"westus" { 
			$luisAuthoringKey = Read-Host "? LUIS Authoring Key (found at https://luis.ai/user/settings)"
			Break
		}
		"westeurope" {
		    $luisAuthoringKey = Read-Host "? LUIS Authoring Key (found at https://eu.luis.ai/user/settings)"
			Break
		}
		"australiaeast" {
			$luisAuthoringKey = Read-Host "? LUIS Authoring Key (found at https://au.luis.ai/user/settings)"
			Break
		}
		default {
			Write-Host "! $($luisAuthoringRegion) is not a valid LUIS authoring region." -ForegroundColor DarkRed
			Break
		}
	}

	if (-not $luisAuthoringKey) {
		Break
	}
}

if (-not $appId) {
	# Create app registration
	$appId = (az ad app create `
		--display-name $name `
		--password $appPassword `
		--available-to-other-tenants `
		--reply-urls https://token.botframework.com/.auth/web/redirect) 2>> $logFile `
	| ConvertFrom-Json `
	| Select-Object -ExpandProperty appId

	if(-not $appId) {
		Write-Host "! Could not provision Microsoft App Registration automatically. Review the log for more information." -ForegroundColor DarkRed
		Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed
		Write-Host "+ Provision an app manually in the Azure Portal, then try again providing the -appId and -appPassword arguments." -ForegroundColor Magenta
		Break
	}
}

# Get timestamp
$timestamp = Get-Date -f MMddyyyyHHmmss

# Create resource group
Write-Host "> Creating resource group ..."
(az group create --name $name --location $location) 2>> $logFile | Out-Null

# Deploy Azure services (deploys LUIS, QnA Maker, Content Moderator, CosmosDB)
Write-Host "> Deploying Azure services (this could take a while)..." -ForegroundColor Yellow
if ($parametersFile) {
    (az group deployment create `
        --name $timestamp `
        --resource-group $resourceGroup `
        --template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'template.json')" `
        --parameters "@$($parametersFile)" `
        --parameters microsoftAppId=$appId microsoftAppPassword="`"$($appPassword)`"") 2>> $logFile | Out-Null
}
else {
    (az group deployment create `
        --name $timestamp `
        --resource-group $resourceGroup `
        --template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'template.json')" `
        --parameters microsoftAppId=$appId microsoftAppPassword="`"$($appPassword)`"") 2>> $logFile | Out-Null
}

# Check for failed deployments
$operations = az group deployment operation list -g $resourceGroup -n $timestamp | ConvertFrom-Json
$failedOperations = $operations | Where { $_.properties.statusmessage.error -ne $null }
if ($failedOperations) {
	foreach ($operation in $failedOperations) {
		switch ($operation.properties.statusmessage.error.code) {
			"MissingRegistrationForLocation" {
				Write-Host "! Deployment failed for resource of type $($operation.properties.targetResource.resourceType). This resource is not avaliable in the location provided." -ForegroundColor DarkRed
				Write-Host "+ Update the .\Deployment\Resources\parameters.template.json file with a valid region for this resource and provide the file path in the -parametersFile parameter." -ForegroundColor Magenta
			}
			default {
				Write-Host "! Deployment failed for resource of type $($operation.properties.targetResource.resourceType)."
				Write-Host "! Code: $($operation.properties.statusMessage.error.code)."
				Write-Host "! Message: $($operation.properties.statusMessage.error.message)."
			}
		}
	}

	Write-Host "+ To delete this resource group, run 'az group delete -g $($resourceGroup) --no-wait'" -ForegroundColor Magenta
	Break
}

# Get deployment outputs
$outputs = (az group deployment show `
    --name $timestamp `
    --resource-group $resourceGroup `
    --query properties.outputs) 2>> $logFile | ConvertFrom-Json

# Update appsettings.json
Write-Host "> Updating appsettings.json ..."
if (Test-Path $(Join-Path $outFolder appsettings.json)) {
    $settings = Get-Content $(Join-Path $outFolder appsettings.json) | ConvertFrom-Json
}
else {
    $settings = New-Object PSObject
}

$settings | Add-Member -Type NoteProperty -Force -Name 'microsoftAppId' -Value $appId
$settings | Add-Member -Type NoteProperty -Force -Name 'microsoftAppPassword' -Value $appPassword
if ($outputs.appInsights) { $settings | Add-Member -Type NoteProperty -Force -Name 'appInsights' -Value $outputs.appInsights.value }
if ($outputs.storage) { $settings | Add-Member -Type NoteProperty -Force -Name 'blobStorage' -Value $outputs.storage.value }
if ($outputs.cosmosDb) { $settings | Add-Member -Type NoteProperty -Force -Name 'cosmosDb' -Value $outputs.cosmosDb.value }

$settings | ConvertTo-Json -depth 100 | Out-File $(Join-Path $outFolder appsettings.json)

# Deploy cognitive models
Invoke-Expression "$(Join-Path $PSScriptRoot 'deploy_cognitive_models.ps1') -name $($name) -luisAuthoringRegion $($luisAuthoringRegion) -luisAuthoringKey $($luisAuthoringKey) -outFolder $($outFolder) -languages `"$($languages)`""

Write-Host "> Done."