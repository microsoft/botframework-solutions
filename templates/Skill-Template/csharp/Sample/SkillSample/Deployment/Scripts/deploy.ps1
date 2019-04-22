Param(
    [string] $name,
	[string] $resourceGroup = $name,
    [string] $location,
	[string] $appId,
    [string] $appPassword,
    [string] $luisAuthoringKey,
    [string] $parametersFile,
	[string] $outFolder = $(Get-Location)
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
    $luisAuthoringKey = Read-Host "LUIS Authoring Key (found at https://www.luis.ai/user/settings)"
}

if (-not $appId) {
	# Create app registration
	$appId = az ad app create `
		--display-name $name `
		--password $appPassword `
		--available-to-other-tenants `
		--reply-urls https://token.botframework.com/.auth/web/redirect `
	| ConvertFrom-Json `
	| Select-Object -ExpandProperty appId

	if(-not $appId) {
		Write-Host "Could not provision Microsoft App Registration automatically. Please provide the -appId and -appPassword arguments for an existing app and try again." -ForegroundColor Cyan
		Break
	}
}

# Get timestamp
$timestamp = Get-Date -f MMddyyyyHHmmss

# Create resource group
Write-Host "Creating resource group ..."
az group create --name $name --location $location | Out-Null

# Deploy Azure services (deploys LUIS, QnA Maker, Content Moderator, CosmosDB)
Write-Host "Deploying Azure services ..."
if ($parametersFile) {
    az group deployment create `
        --name $timestamp `
        --resource-group $resourceGroup `
        --template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'template.json')" `
        --parameters "@$($parametersFile)" `
        --parameters microsoftAppId=$appId microsoftAppPassword=$appPassword | Out-Null
}
else {
    az group deployment create `
        --name $timestamp `
        --resource-group $resourceGroup `
        --template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'template.json')" `
        --parameters microsoftAppId=$appId microsoftAppPassword=$appPassword | Out-Null
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
$settings | ConvertTo-Json -depth 100 | Out-File $(Join-Path $outFolder appsettings.json)

# Deploy cognitive models
Invoke-Expression "$(Join-Path $PSScriptRoot 'deploy_cognitive_models.ps1') -name $($name) -location $($location) -luisAuthoringKey $luisAuthoringKey -outFolder $($outFolder)"