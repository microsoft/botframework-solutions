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
	[string] $groupName = $name,
	[string] $sdkLanguage,
	[string] $sdkVersion,
	[string] $prefix,
	[string] $appId,        
    [string] $appSecret,
    [switch] $showLogs
)

if (!$languagesOnly)
{
	# Change to project directory for .bot file
    Write-Host "Changing to project directory ..."
    Set-Location "$($PSScriptRoot)\..\"

    # Deploy the common resources (Azure Bot Service, App Insights, Azure Storage, Cosmos DB, etc)
    Write-Host "Deploying common resources..."
    $msbotScript = "msbot clone services -n " + $name + " -l " + $location + " --luisAuthoringKey " + $luisAuthoringKey + " --groupName " + $groupName + " --folder $($PSScriptRoot) --appId " + $appId + " --appSecret " + $appSecret + " --force  " 
    if($showLogs)
    {
        $msbotScript += "--verbose"
    }
    else
    {
        $msbotScript += "--quiet"
    }
	Invoke-Expression $msbotScript
}

$localeArr = $locales.Split(',')

foreach ($locale in $localeArr)
{	
	# Update deployment scripts for the locale
	Invoke-Expression "$($PSScriptRoot)\generate_deployment_scripts.ps1 -locale $($locale)"

	# Get language code from locale (first two characters, i.e. "en")
	$langCode = ($locale -split "-")[0]

	# Create LocaleConfigurations folder and change directory
	mkdir -Force "$($PSScriptRoot)\..\LocaleConfigurations" > $null
    Set-Location "$($PSScriptRoot)\..\LocaleConfigurations" > $null

	# Deploy Dispatch, LUIS (calendar, email, todo, and general), and QnA Maker for the locale
    Write-Host "Deploying $($locale) resources..."
    msbot clone services -n "$($name)$($langCode)" -l $location --luisAuthoringKey $luisAuthoringKey --groupName $groupName --force --quiet --folder "$($PSScriptRoot)\$($langCode)" | Out-Null
}

Write-Host "Done."