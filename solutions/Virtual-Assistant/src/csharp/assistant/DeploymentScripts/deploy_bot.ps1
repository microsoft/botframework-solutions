# all msbot clone parameters and locales
Param(
	[string] [Parameter(Mandatory=$true)]$name,
	[string] [Parameter(Mandatory=$true)]$location,
	[string] [Parameter(Mandatory=$true)]$luisAuthoringKey,
	[string] $locales = "en-us",
	[switch] $languagesOnly,
	[string] $luisAuthoringRegion,
	[string] $luisPublishRegion,
	[string] $subscriptionId,
	[string] $insightsRegion,
	[string] $groupName,
	[string] $sdkLanguage,
	[string] $sdkVersion,
	[string] $prefix,
	[string] $appId,        
	[string] $appSecret
)

if (!$languagesOnly)
{
	# Change to project directory for .bot file
	Write-Host "Changing to project directory ..."
	cd "$($PSScriptRoot)\..\"

	# Deploy the common resources (Azure Bot Service, App Insights, Azure Storage, Cosmos DB, etc)
	Write-Host "Deploying common resources..."
	msbot clone services -n $name -l $location --luisAuthoringKey $luisAuthoringKey --folder "$($PSScriptRoot)" --appId $appId --appSecret $appSecret --force --quiet
}

$localeArr = $locales.Split(',')

foreach ($locale in $localeArr)
{	
	# Update deployment scripts for the locale
	Invoke-Expression "$($PSScriptRoot)\generate_deployment_scripts.ps1 -locale $($locale)"

	# Get language code from locale (first two characters, i.e. "en")
	$langCode = ($locale -split "-")[0]

	# Create LocaleConfigurations folder and change directory
	md -Force "$($PSScriptRoot)\..\LocaleConfigurations" > $null
	cd "$($PSScriptRoot)\..\LocaleConfigurations" > $null

	# Deploy Dispatch, LUIS (calendar, email, todo, and general), and QnA Maker for the locale
    Write-Host "Deploying $($locale) resources..."
    msbot clone services -n "$($name)$($langCode)" -l $location --luisAuthoringKey $luisAuthoringKey --groupName $name --force --quiet --folder "$($PSScriptRoot)\$($langCode)" | Out-Null
}

Write-Host "Done."