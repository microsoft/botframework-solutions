Param(
    [string] $name,
	[string] $location,
	[string] $appId,
	[string] $appPassword,
	[string] $environment,
	[string] $luisAuthoringKey,
	[string] $projDir = $(Get-Location),
	[string] $logFile = $(Join-Path $PSScriptRoot .. "create_log.txt")
)

if ($PSVersionTable.PSVersion.Major -lt 6){
	Write-Host "! Powershell 6 is required, current version is $($PSVersionTable.PSVersion.Major), please refer following documents for help."
	Write-Host "For Windows - https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-windows?view=powershell-6"
	Write-Host "For Mac - https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-macos?view=powershell-6"
	Break
}

# Reset log file
if (Test-Path $logFile) {
	Clear-Content $logFile -Force | Out-Null
}
else {
	New-Item -Path $logFile | Out-Null
}

if (-not (Test-Path (Join-Path $projDir 'appsettings.deployment.json')))
{
	Write-Host "! Could not find an 'appsettings.deployment.json' file in the current directory." -ForegroundColor DarkRed
	Write-Host "+ Please re-run this script from your project directory." -ForegroundColor Magenta
	Break
}

# Get mandatory parameters
if (-not $name) {
    $name = Read-Host "? Bot Name (used as default name for resource group and deployed resources)"
}

if (-not $environment)
{
	$environment = Read-Host "? Environment Name (single word, all lowercase)"
	$environment = $environment.ToLower().Split(" ") | Select-Object -First 1
}

if (-not $location) {
    $location = Read-Host "? Azure resource group region"
}

if (-not $appPassword) {
    $appPassword = Read-Host "? Password for MSA app registration (must be at least 16 characters long, contain at least 1 special character, and contain at least 1 numeric character)"
}

if (-not $appId) {
	# Create app registration
	$app = (az ad app create `
		--display-name $name `
		--password `"$($appPassword)`" `
		--available-to-other-tenants `
		--reply-urls 'https://token.botframework.com/.auth/web/redirect' `
        --output json)

	# Retrieve AppId
	if ($app) {
		$appId = ($app | ConvertFrom-Json) | Select-Object -ExpandProperty appId
	}

	if(-not $appId) {
		Write-Host "! Could not provision Microsoft App Registration automatically. Review the log for more information." -ForegroundColor DarkRed
		Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed
		Write-Host "+ Provision an app manually in the Azure Portal, then try again providing the -appId and -appPassword arguments. See https://aka.ms/vamanualappcreation for more information." -ForegroundColor Magenta
		Break
	}
}

$shouldCreateAuthoringResource = $true

# Use pre-exsisting luis authoring key
if ($luisAuthoringKey) {
	$shouldCreateAuthoringResource = $false
}

$resourceGroup = "$name-$environment"
$servicePlanName = "$name-$environment"

# Get timestamp
$timestamp = Get-Date -f MMddyyyyHHmmss

# Create resource group
Write-Host "> Creating resource group ..."
(az group create --name $resourceGroup --location $location --output json) 2>> $logFile | Out-Null

# Deploy Azure services
Write-Host "> Validating Azure deployment ..."
$validation = az group deployment validate `
	--resource-group $resourcegroup `
	--template-file "$(Join-Path $PSScriptRoot '..' 'DeploymentTemplates' 'template-with-preexisting-rg.json')" `
	--parameters appId=$appId appSecret="`"$($appPassword)`"" appServicePlanLocation=$location botId=$name shouldCreateAuthoringResource=$shouldCreateAuthoringResource luisAuthoringKey=$luisAuthoringKey `
	--output json

if ($validation) {
	$validation >> $logFile
	$validation = $validation | ConvertFrom-Json

	if (-not $validation.error) {
		Write-Host "> Deploying Azure services (this could take a while)..." -ForegroundColor Yellow
		$deployment = az group deployment create `
			--name $timestamp `
			--resource-group $resourceGroup `
			--template-file "$(Join-Path $PSScriptRoot '..' 'DeploymentTemplates' 'template-with-preexisting-rg.json')" `
			--parameters appId=$appId appSecret="`"$($appPassword)`"" appServicePlanLocation=$location botId=$name shouldCreateAuthoringResource=$shouldCreateAuthoringResource luisAuthoringKey=$luisAuthoringKey `
			--output json
	}
	else {
		Write-Host "! Template is not valid with provided parameters. Review the log for more information." -ForegroundColor DarkRed
		Write-Host "! Error: $($validation.error.message)"  -ForegroundColor DarkRed
		Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed
		Write-Host "+ To delete this resource group, run 'az group delete -g $($resourceGroup) --no-wait'" -ForegroundColor Magenta

		if ($validation.error.details -and $validation.error.details[0].code -eq "CanNotCreateMultipleFreeAccounts")
		{
			Write-Host "! The subscription is exceeding the maximum number of allowed LuisAuthoringAccounts. You already have a luis authoring resource created, please get your luis authoring key and retry with the following command:" -ForegroundColor DarkRed
			Write-Host "pwsh ./Scripts/create.ps1 -name $name -environment $environment -location $location -appPassword $appPassword -luisAuthoringKey [YourLuisAuthoringKey]" -ForegroundColor Green
		}

		Break
	}
}


# Get deployment outputs
$outputs = (az group deployment show `
	--name $timestamp `
	--resource-group $resourceGroup `
    --output json) 2>> $logFile

# If it succeeded then we perform the remainder of the steps
if ($outputs)
{
	# Log and convert to JSON
	$outputs >> $logFile
	$outputs = $outputs | ConvertFrom-Json
	if ($outputs.properties.error) {
		Write-Host "! Deployment failed. Review the log for more information." -ForegroundColor DarkRed
		Write-Host "! Error: $($outputs.error.message)"  -ForegroundColor DarkRed
		Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed
		Write-Host "+ To delete this resource group, run 'az group delete -g $($resourceGroup) --no-wait'" -ForegroundColor Magenta
		
		Break
	}

	$outputs = $outputs.properties.outputs
	$outputMap = @{}
	$outputs.PSObject.Properties | Foreach-Object { $outputMap[$_.Name] = $_.Value }

	# Update appsettings.deployment.json
	Write-Host "> Updating appsettings.deployment.json ..."
	if (Test-Path $(Join-Path $projDir appsettings.deployment.json)) {
		$settings = Get-Content $(Join-Path $projDir appsettings.deployment.json) | ConvertFrom-Json
	}
	else {
		$settings = New-Object PSObject
	}

	$settings | Add-Member -Type NoteProperty -Force -Name 'MicrosoftAppId' -Value $appId
	$settings | Add-Member -Type NoteProperty -Force -Name 'MicrosoftAppPassword' -Value $appPassword

	$settings | Add-Member -Type NoteProperty -Force -Name 'bot' -Value "ComposerDialogs"

	foreach ($key in $outputMap.Keys) { $settings | Add-Member -Type NoteProperty -Force -Name $key -Value $outputMap[$key].value }
	$settings | ConvertTo-Json -depth 100 | Out-File $(Join-Path $projDir appsettings.deployment.json)

	Write-Host "> Done."
	Write-Host "- App Id: $appId"
	Write-Host "- App Password: $appPassword"
	Write-Host "- Resource Group: $resourceGroup"
	Write-Host "- ServicePlan: $servicePlanName"
	Write-Host "- Bot Name: $name"
	Write-Host "- Web App Name : $name"
}
else
{
	# Check for failed deployments
	$operations = az group deployment operation list -g $resourceGroup -n $timestamp --output json 2>> $logFile | Out-Null 
	
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
