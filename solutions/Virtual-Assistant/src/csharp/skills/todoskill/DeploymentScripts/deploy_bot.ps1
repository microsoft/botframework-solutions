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

$localeArr = $locales.Split(',')

Write-Host "Updating deployment scripts..."
foreach ($locale in $localeArr) 
{
	Invoke-Expression "$($PSScriptRoot)\generate_deployment_scripts.ps1 -locale $($locale)"
}

foreach ($locale in $localeArr)
{
    Write-Host "Deploying $($locale) resources..."
	$langCode = ($locale -split "-")[0]
	md -Force "$($PSScriptRoot)\..\LocaleConfigurations" 
	cd "$($PSScriptRoot)\..\LocaleConfigurations"
    msbot clone services -n "$($name)$($langCode)" -l $location --luisAuthoringKey $luisAuthoringKey --groupName $name --folder "$($PSScriptRoot)\$($langCode)" --force
}

if (!$languagesOnly)
{
	Write-Host "Changing to project directory ..."
	cd "$($PSScriptRoot)\..\"

	Write-Host "Deploying common resources..."
	msbot clone services -n $name -l $location --luisAuthoringKey $luisAuthoringKey --folder "$($PSScriptRoot)" --appId $appId --appSecret $appSecret --force
}