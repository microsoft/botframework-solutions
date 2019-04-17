Param(
    [string] $name,
    [string] $location,
    [string] $appPassword,
    [string] $luisAuthoringKey,
    [string] $resourceGroup = $name,
    [string] $outFolder = $(Get-Location),
    [string] $parametersFile
)

# Get mandatory parameters
if (-not $name) {
    $name = Read-Host "Azure resource group name"
    $resourceGroup = $name
}

if (-not $location) {
    $location = Read-Host "Azure resource group region"
}

if (-not $appPassword) {
    $appPassword = Read-Host "Password for MSA app registration (must be at least 16 characters long, contain at least 1 special character, and contain at least 1 numeric character)"
}

if (-not $luisAuthoringKey) {
    $luisAuthoringKey = Read-Host "LUIS Authoring Key (found at https://www.luis.ai/user/settings or https://eu.luis.ai/user/settings)"
}

# Get timestamp
$timestamp = Get-Date -f MMddyyyyHHmmss

# Create resource group
Write-Host "Creating resource group ..."
az group create --name $name --location $location | Out-Null

# Create bot registration
$appId = az ad app create `
    --display-name $name `
    --password $appPassword `
	--reply-urls https://token.botframework.com/.auth/web/redirect `
| ConvertFrom-Json `
| Select-Object -ExpandProperty appId

# Deploy Azure services (deploys LUIS, QnA Maker, Content Moderator, CosmosDB)
Write-Host "Deploying Azure services ..."
if ($parametersFile) {
	$validation = az group deployment validate `
		--resource-group $resourceGroup `
		--template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'template.json')" `
        --parameters "@$($parametersFile)" `
        --parameters microsoftAppId=$appId microsoftAppPassword=$appPassword

	if (-not $validation.error) {
		az group deployment create `
			--name $timestamp `
			--resource-group $resourceGroup `
			--template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'template.json')" `
			--parameters "@$($parametersFile)" `
			--parameters microsoftAppId=$appId microsoftAppPassword=$appPassword | Out-Null
	}
	else {
		Write-Error $result.error
		Break
	}
}
else {
	$validation = az group deployment validate `
		--resource-group $resourceGroup `
		--template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'template.json')" `
        --parameters microsoftAppId=$appId microsoftAppPassword=$appPassword

	if (-not $validation.error) {
		az group deployment create `
			--name $timestamp `
			--resource-group $resourceGroup `
			--template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'template.json')" `
			--parameters microsoftAppId=$appId microsoftAppPassword=$appPassword | Out-Null
	}
	else {
		Write-Error $result.error
		Break
	}
}

# Get deployment outputs
$outputs = az group deployment show `
    --name $timestamp `
    --resource-group $resourceGroup `
    --query properties.outputs | ConvertFrom-Json

# Update appsettings.json
Write-Host "Updating appsettings.json ..."
if (Test-Path $(Join-Path $outFolder appsettings.json)) {
    $settings = Get-Content $(Join-Path $outFolder appsettings.json) | ConvertFrom-Json
}
else {
    $settings = New-Object PSObject
}
$settings | Add-Member -Type NoteProperty -Force -Name 'microsoftAppId' -Value $appId
$settings | Add-Member -Type NoteProperty -Force -Name 'microsoftAppPassword' -Value $appPassword
$settings | Add-Member -Type NoteProperty -Force -Name 'appInsights' -Value $outputs.appInsights.value
$settings | Add-Member -Type NoteProperty -Force -Name 'blobStorage' -Value $outputs.storage.value
$settings | Add-Member -Type NoteProperty -Force -Name 'cosmosDb' -Value $outputs.cosmosDb.value
$settings | Add-Member -Type NoteProperty -Force -Name 'contentModerator' -Value $outputs.contentModerator.value
$settings | ConvertTo-Json -depth 100 | Out-File $(Join-Path $outFolder appsettings.json)

# Deploy cognitive models
Invoke-Expression "$(Join-Path $PSScriptRoot 'deploy_cognitive_models.ps1') -name $($name) -location $($location) -luisAuthoringKey $luisAuthoringKey -qnaSubscriptionKey $($outputs.qnaMaker.value.key) -outFolder $($outFolder)"