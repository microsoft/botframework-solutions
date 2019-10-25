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

if (-not (Test-Path (Join-Path $projDir 'appsettings.json')))
{
	Write-Host "! Could not find an 'appsettings.json' file in the current directory." -ForegroundColor DarkRed
	Write-Host "+ Please re-run this script from your project directory." -ForegroundColor Magenta
	Break
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

$app = $null

if (-not $appId) {
	# Create app registration
	$appJson = (az ad app create `
		--display-name $name `
		--password `"$($appPassword)`" `
		--available-to-other-tenants `
		--reply-urls 'https://token.botframework.com/.auth/web/redirect' `
        --output json)

	if(-not $appJson) {
		Write-Host "! Could not provision Microsoft App Registration automatically. Review the log for more information." -ForegroundColor DarkRed
		Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed
		Write-Host "+ Provision an app manually in the Azure Portal, then try again providing the -appId and -appPassword arguments. See https://aka.ms/vamanualappcreation for more information." -ForegroundColor Magenta
		Break
	}

	$app = ($appJson | ConvertFrom-Json)
} else {
	# Get the app registration
	$appsJson = (az ad app list `
		--app-id $appId `
		--output json)

	$apps = ($appsJson | ConvertFrom-Json)

	if($apps.Length -eq 0) {
		Write-Host "! Could not find appId $($appId). Review the log for more information." -ForegroundColor DarkRed
		Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed
		Break
	}

	$app = $apps[0];
}

$appId = $app.appId

# Get timestamp
$timestamp = Get-Date -f MMddyyyyHHmmss

# Create resource group
Write-Host "> Creating resource group ..."
(az group create --name $resourceGroup --location $location --output json) 2>> $logFile | Out-Null

# Deploy Azure services (deploys LUIS, QnA Maker, Content Moderator, CosmosDB)
if ($parametersFile) {
	Write-Host "> Validating Azure deployment ..."
	$validation = az group deployment validate `
		--resource-group $resourcegroup `
		--template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'template.json')" `
		--parameters "@$($parametersFile)" `
		--parameters name=$name microsoftAppId=$appId microsoftAppPassword="`"$($appPassword)`"" `
        --output json

	if ($validation) {
		$validation >> $logFile
		$validation = $validation | ConvertFrom-Json
	
		if (-not $validation.error) {
			Write-Host "> Deploying Azure services (this could take a while)..." -ForegroundColor Yellow
			$deployment = az group deployment create `
				--name $timestamp `
				--resource-group $resourceGroup `
				--template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'template.json')" `
				--parameters "@$($parametersFile)" `
				--parameters name=$name microsoftAppId=$appId microsoftAppPassword="`"$($appPassword)`"" `
                --output json
		}
		else {
			Write-Host "! Template is not valid with provided parameters. Review the log for more information." -ForegroundColor DarkRed
			Write-Host "! Error: $($validation.error.message)"  -ForegroundColor DarkRed
			Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed
			Write-Host "+ To delete this resource group, run 'az group delete -g $($resourceGroup) --no-wait'" -ForegroundColor Magenta
			Break
		}
	}
}
else {
	Write-Host "> Validating Azure deployment ..."
	$validation = az group deployment validate `
		--resource-group $resourcegroup `
		--template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'template.json')" `
		--parameters name=$name microsoftAppId=$appId microsoftAppPassword="`"$($appPassword)`"" `
        --output json

	if ($validation) {
		$validation >> $logFile
		$validation = $validation | ConvertFrom-Json

		if (-not $validation.error) {
			Write-Host "> Deploying Azure services (this could take a while)..." -ForegroundColor Yellow
			$deployment = az group deployment create `
				--name $timestamp `
				--resource-group $resourceGroup `
				--template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'template.json')" `
				--parameters name=$name microsoftAppId=$appId microsoftAppPassword="`"$($appPassword)`"" `
                --output json
		}
		else {
			Write-Host "! Template is not valid with provided parameters. Review the log for more information." -ForegroundColor DarkRed
			Write-Host "! Error: $($validation.error.message)"  -ForegroundColor DarkRed
			Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed
			Write-Host "+ To delete this resource group, run 'az group delete -g $($resourceGroup) --no-wait'" -ForegroundColor Magenta
			Break
		}
	}
}

# Get deployment outputs
$outputs = (az group deployment show `
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

	# Update appsettings.json
	Write-Host "> Updating appsettings.json ..."
	if (Test-Path $(Join-Path $projDir appsettings.json)) {
		$settings = Get-Content -Encoding utf8 $(Join-Path $projDir appsettings.json) | ConvertFrom-Json
	}
	else {
		$settings = New-Object PSObject
	}

	$settings | Add-Member -Type NoteProperty -Force -Name 'microsoftAppId' -Value $appId
	$settings | Add-Member -Type NoteProperty -Force -Name 'microsoftAppPassword' -Value $appPassword
	foreach ($key in $outputMap.Keys) { $settings | Add-Member -Type NoteProperty -Force -Name $key -Value $outputMap[$key].value }
	$settings | ConvertTo-Json -depth 100 | Out-File -Encoding utf8 $(Join-Path $projDir appsettings.json)
	
	if ($outputs.qnaMaker.value.key) { $qnaSubscriptionKey = $outputs.qnaMaker.value.key }

	# Delay to let QnA Maker finish setting up
	Start-Sleep -s 30

	# Deploy cognitive models
	Invoke-Expression "& '$(Join-Path $PSScriptRoot 'deploy_cognitive_models.ps1')' -name $($name) -luisAuthoringRegion $($luisAuthoringRegion) -luisAuthoringKey $($luisAuthoringKey) -luisAccountName $($outputs.luis.value.accountName) -luisAccountRegion $($outputs.luis.value.region) -luisSubscriptionKey $($outputs.luis.value.key) -resourceGroup $($resourceGroup) -qnaSubscriptionKey '$($qnaSubscriptionKey)' -outFolder '$($projDir)' -languages '$($languages)'"

	# Update app registration homepage to point to skill
	az ad app update --id $app.objectId --homepage "https://$($outputs.botWebAppName.value).azurewebsites.net"

	# Summary 
	Write-Host "Summary of the deployed resources:" -ForegroundColor Yellow

	Write-Host "- Resource Group: $($resourceGroup)" -ForegroundColor Yellow
		
	Write-Host "- Bot Web App: $($outputs.botWebAppName.value)`n" -ForegroundColor Yellow

	# Publish bot
	Invoke-Expression "& '$(Join-Path $PSScriptRoot 'publish.ps1')' -name $($outputs.botWebAppName.value) -resourceGroup $($resourceGroup) -projFolder '$($projDir)'"

	Write-Host "> Done."
}
else
{
	# Check for failed deployments
	$operations = (az group deployment operation list -g $resourceGroup -n $timestamp --output json) 2>> $logFile | Out-Null 
	
	if ($operations) {
		$operations = $operations | ConvertFrom-Json
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
		}
	}
	else {
		Write-Host "! Deployment failed. Please refer to the log file for more information." -ForegroundColor DarkRed
		Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed
	}
	
	Write-Host "+ To delete this resource group, run 'az group delete -g $($resourceGroup) --no-wait'" -ForegroundColor Magenta
	Break
}