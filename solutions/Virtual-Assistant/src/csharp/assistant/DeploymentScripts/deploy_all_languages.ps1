# all msbot clone parameters and locales
Param(
	[string] [Parameter(Mandatory=$true)]$name,
    [string] [Parameter(Mandatory=$true)]$location,
	[string] [Parameter(Mandatory=$true)]$luisAuthoringKey,
    [string] $locales = "de-de,en-us,es-es,fr-fr,it-it,zh-cn",
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

Write-Host "Updating deployment scripts..."
Invoke-Expression "$($PSScriptRoot)\update_all_deployment_scripts.ps1"

Write-Host "Deploying common resources..."
msbot clone services -n $name -l $location --luisAuthoringKey $luisAuthoringKey --folder "$($PSScriptRoot)" --appId $appId --appSecret $appSecret --force

$localeArr = $locales.Split(',');

foreach ($locale in $localeArr)
{
    Write-Host "Deploying $($locale) resources..."
	$langCode = ($locale -split "-")[0]
	md -Force "$($PSScriptRoot)\..\LocaleConfigurations" 
	cd "$($PSScriptRoot)\..\LocaleConfigurations"
    msbot clone services -n "$($name)$($langCode)" -l $location --luisAuthoringKey $luisAuthoringKey --groupName $name --folder "$($PSScriptRoot)\$($langCode)" --force
}