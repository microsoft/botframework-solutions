Param(
	[Parameter(Mandatory = $true)][string] $botName,
    [Parameter(Mandatory = $true)][string] $skillConfigFile,
    [Parameter(Mandatory = $true)][string] $manifestUrl,
    [Parameter(Mandatory = $true)][string] $dispatchName,
    [string] $language = "en-us",
    [string] $luisFolder,
    [string] $dispatchFolder,
    [string] $outFolder = $(Get-Location),
    [string] $lgOutFolder = $(Join-Path $outFolder Services),
	[string] $appSettingsFile = $(Join-Path $outFolder 'appsettings.json'),
	[string] $resourceGroup = $botName
)

. $PSScriptRoot\skill_functions.ps1

# Set folder defaults
$langCode = ($language -split "-")[0]
if (-not $luisFolder) {
    $luisFolder = $(Join-Path $PSScriptRoot .. Resources Skills $langCode)
}
if (-not $dispatchFolder) {
    $dispatchFolder = $(Join-Path $PSScriptRoot .. Resources Dispatch $langCode)
}

Write-Host "Loading skill manifest ..." -ForegroundColor Yellow
$manifest = Invoke-WebRequest -Uri $manifestUrl | ConvertFrom-Json

Write-Host "Initializing skill.config ..." -ForegroundColor Yellow
if (Test-Path $skillConfigFile) {
    $skillConfig = Get-Content $skillConfigFile | ConvertFrom-Json

    if ($skillConfig) {
        if ($skillConfig.skills) {
            if ($skillConfig.skills.Id -eq $manifest.Id) {
                Write-Host "$($manifest.Id) is already registered." -ForegroundColor Red
                Break
            }
            else {
                Write-Host "Registering $($manifest.Id) ..." -ForegroundColor Yellow
                $skillConfig.skills += $manifest
            }
        }
        else {
            Write-Host "Registering $($manifest.Id) ..." -ForegroundColor Yellow  
            $skills = @($manifest)
            $skillConfig | Add-Member -Type NoteProperty -Force -Name "skills" -Value $skills
        }
    }
}

if (-not $skillConfig) {
    $skillConfig = @{ skills = @($manifest) }
}

$skillConfig | ConvertTo-Json -depth 100 | Out-File $skillConfigFile

# configuring bot auth settings
Write-Host "Checking for authentication settings ..."
if ($manifest.authenticationConnections) {
	if ($manifest.authenticationConnections | Where-Object { $_.serviceProviderId -eq "Azure Active Directory v2" })
	{
		Write-Host "Configuring Azure AD authentication connection ..." -ForegroundColor Yellow
		$appSettings = Get-Content $appSettingsFile | ConvertFrom-Json
		$aad = $manifest.authenticationConnections `
			| Where-Object { $_.serviceProviderId -eq "Azure Active Directory v2" } `
			| Select-Object -First 1
		$connectionName = $aad.Id
		$newScopes = $aad.scopes -Split ", "
		$scopes = $newScopes

		# check for existing aad connection
		$connections = az bot authsetting list -n $botName -g $resourceGroup | ConvertFrom-Json
		if ($connections -and ($connections | Where-Object {$_.properties.serviceProviderDisplayName -eq "Azure Active Directory v2" })) {
			$connection = $connections `
				| Where-Object {$_.properties.serviceProviderDisplayName -eq "Azure Active Directory v2" } `
				| Select-Object -First 1

			$settingName = $($connection.name -Split "/")[1]
			$active = az bot authsetting show -n $botName -g $resourceGroup -c $settingName | ConvertFrom-Json
			$existingScopes = $active.properties.scopes -Split " "
			$scopes = $scopes + $existingScopes
			$connectionName = $settingName

			# delete current auth connection
			az bot authsetting delete -n $botName -g $resourceGroup -c $settingName | Out-Null
		}

		# update appsettings.json
		$oauthSetting = @{ "name" = $connectionName; "provider" = "Azure Active Directory v2" }
		$appSettings.oauthConnections = $appSettings.oauthConnections | ? { $_.provider -ne "Azure Active Directory v2" }
		if ($appSettings.oauthConnections) {
			$appSettings.oauthConnections = @($oauthSetting)
		}
		else {
			$appSettings.oauthConnections += @($oauthSetting)
		}

		ConvertTo-Json $appSettings -depth 100 | Out-File $appSettingsFile
		# Remove duplicate scopes
		$scopes = $scopes | Select -unique
		$scopeManifest = $(CreateScopeManifest($scopes)).Replace("`"", "'")

		Write-Host "Updating Microsoft App scopes ..."
		# Update MSA scopes
		az ad app update `
			--id "$($appSettings.microsoftAppId)" `
			--required-resource-accesses "`"[$($scopeManifest)]`""

		Write-Host "updating Bot OAuth settings ..."
		az bot authsetting create `
			--name $botName `
			--resource-group $resourceGroup `
			--setting-name $connectionName `
			--client-id "$($appSettings.microsoftAppId)" `
			--client-secret "$($appSettings.microsoftAppPassword)" `
			--service Aadv2 `
			--parameters clientId="$($appSettings.microsoftAppId)" clientSecret="$($appSettings.microsoftAppPassword)" tenantId=common `
			--provider-scope-string "$($scopes)" | Out-Null	
	}
	else {
		Write-Host "Could not configure authentication connection automatically." -ForegroundColor Yellow
		$manualAuthRequired = $true
	}
}

Write-Host "Getting intents for dispatch ..." -ForegroundColor Yellow 
$dictionary = @{ }
foreach ($action in $manifest.actions) {
   if ($action.definition.triggers.utteranceSources) {
       foreach ($source in $action.definition.triggers.utteranceSources) {
           foreach ($luisStr in $source.source) {
               $luis = $luisStr -Split '#'                
               if ($dictionary.ContainsKey($luis[0])) {
                   $intents = $dictionary[$luis[0]]
                   $intents += $luis[1]
                   $dictionary[$luis[0]] = $intents
               }
               else {
                   $dictionary.Add($luis[0], @($luis[1]))
               }
           }
       }
   }
}

Write-Host "Adding skill to Dispatch ..." -ForegroundColor Yellow 
$intentName = $manifest.Id
foreach ($luisApp in $dictionary.Keys) {
    $intents = $dictionary[$luisApp]
    $luFile = Get-ChildItem -Path $(Join-Path $luisFolder "$($luisApp).lu") `

    # Parse LU file
    Write-Host "Parsing $($luisApp) LU file ..." -ForegroundColor Yellow
    ludown parse toluis `
        --in $luFile `
        --luis_culture $language `
        --out_folder $luisFolder `
        --out "$($luisApp).luis"

    $luisFile = Get-ChildItem `
        -Path $luisFolder `
        -Filter "$($luisApp).luis" `
        -ErrorAction SilentlyContinue `
        -Recurse `
        -Force

	dispatch add `
        --type file `
        --filePath $luisFile `
        --intentName $intentName `
        --dataFolder $dispatchFolder `
        --dispatch $(Join-Path $dispatchFolder "$($dispatchName).dispatch") | Out-Null
}

Write-Host "Running dispatch refresh ..." -ForegroundColor Yellow 
dispatch refresh --dispatch $(Join-Path $dispatchFolder "$($dispatchName).dispatch") --dataFolder $dispatchFolder | Out-Null

Write-Host "Running LuisGen ..." -ForegroundColor Yellow 
luisgen $(Join-Path $dispatchFolder "$($dispatchName).json") -cs "DispatchLuis" -o $lgOutFolder | Out-Null

if ($manualAuthRequired) {
	Write-Host "Could not configure authentication connection automatically. You must configure one of the following connection types manually in the Azure Portal: $($manifest.authenticationConnections.serviceProviderId -Join ', ')" -ForegroundColor Magenta
}