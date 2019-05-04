#Requires -Version 6

Param(
	[string] $botName,
    [string] $manifestUrl,
	[string] $luisFolder,
    [string] $dispatchFolder,
	[string] $dispatchName,
	[string] $language = "en-us",
	[string] $resourceGroup,
    [string] $outFolder = $(Get-Location),
    [string] $lgOutFolder = $(Join-Path $outFolder Services),
	[string] $appSettingsFile = $(Join-Path $outFolder 'appsettings.json'),
	[string] $skillsFile = $(Join-Path $outFolder 'skills.json'),
	[string] $cognitiveModelsFile = $(Join-Path $outFolder 'cognitivemodels.json'),
	[string] $logFile = $(Join-Path $PSScriptRoot .. "add_remote_skill_log.txt")
)

. $PSScriptRoot\skill_functions.ps1

# Reset log file
if (Test-Path $logFile) {
	Clear-Content $logFile -Force | Out-Null
}
else {
	New-Item -Path $logFile | Out-Null
}

# Set defaults and validate file paths
$langCode = ($language -split "-")[0]
if (-not $luisFolder) {
    $luisFolder = $(Join-Path $PSScriptRoot .. Resources Skills $langCode)
}

if (-not $botName) {
	$botName = Read-Host "? Virtual Assistant Name (used to configure skill authentication)"
}

if (-not $manifestUrl) {
	$manifestUrl = Read-Host "? Skill Manifest URL (i.e. https://calendarskill.azurewebsites.net/api/skill/manifest)"
}

if (-not $dispatchFolder) {
    $dispatchFolder = $(Join-Path $PSScriptRoot .. Resources Dispatch $langCode)
}

if (-not $dispatchName) {
	if (Test-Path $cognitiveModelsFile) {
		$cognitiveModels = Get-Content $cognitiveModelsFile | ConvertFrom-Json
		$models = $($cognitiveModels.cognitiveModels.PSObject.Properties | Where-Object { $_.Name -eq $langCode } | Select-Object -First 1).Value
		$dispatchName = $models.dispatchModel.name
	}
	else {
		Write-Host "! Could not find file: $($cognitiveModelsFile). Please provide a valid path, or the dispatchName and dispatchFolder parameters." -ForegroundColor DarkRed
		Break
	}
}

if (-not $resourceGroup) {
	$resourceGroup = $botName
}

if (-not $(Test-Path $appSettingsFile)) {
	Write-Host "! Could not find file: $($appSettingsFile)." -ForegroundColor DarkRed
	Break
}

$dispatchPath = $(Join-Path $dispatchFolder "$($dispatchName).dispatch")
if (-not $(Test-Path $dispatchPath)) {
	Write-Host "! Could not find file: $($dispatchPath). Please provide the dispatchName and dispatchFolder parameters." -ForegroundColor DarkRed
	Break
}

$dispatchJsonPath = $(Join-Path $dispatchFolder "$($dispatchName).json")
if (-not $(Test-Path $dispatchJsonPath)) {
	Write-Host "! Could not find file: $($dispatchPath). LuisGen will not be run." -ForegroundColor DarkRed
}

# Processing
Write-Host "> Loading skill manifest ..."
$manifest = $(Invoke-WebRequest -Uri $manifestUrl | ConvertFrom-Json) 2>> $logFile

if (-not $manifest) {
	Write-Host "! Could not load manifest from $($manifestUrl). Please check the url and try again." -ForegroundColor DarkRed
	Break
}

Write-Host "> Getting intents for dispatch ..." 
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

Write-Host "> Adding $($manifest.Id) to dispatch ..." 
try {
	foreach ($luisApp in $dictionary.Keys) {
		$intentName = $manifest.Id
		$intents = $dictionary[$luisApp] -Join ","
		$luFile = Get-ChildItem -Path $(Join-Path $luisFolder "$($luisApp).lu") ` 2>> $logFile

		if (-not $luFile) {
			$luFile = Get-ChildItem -Path $(Join-Path $luisFolder $langCode "$($luisApp).lu") ` 2>> $logFile

			if ($luFile) {
				$luisFolder = $(Join-Path $luisFolder $langCode)
			}
			else {
				Write-Host "! Could not find $($manifest.Name) LU file. Please provide the -luisFolder parameter." -ForegroundColor DarkRed
				Write-Host "! Checked the following locations:"  -ForegroundColor DarkRed
				Write-Host "	$(Join-Path $luisFolder "$($luisApp).lu")"  -ForegroundColor DarkRed
				Write-Host "	$(Join-Path $luisFolder $langCode "$($luisApp).lu")"  -ForegroundColor DarkRed
				Throw
			}
		}

		# Parse LU file
		ludown parse toluis `
			--in $luFile `
			--luis_culture $language `
			--out_folder $luisFolder `
			--out "$($luisApp).luis"

		$luisFile = Get-ChildItem `
			-Path $luisFolder `
			-Filter "$($luisApp).luis" `
			-Recurse `
			-Force 2>> $logFile

		if ($luisFile) {
			(dispatch add `
				--name $intentName `
				--type file `
				--filePath $luisFile `
				--intentName $intentName `
				--includedIntents $intents `
				--dataFolder $dispatchFolder `
				--dispatch $(Join-Path $dispatchFolder "$($dispatchName).dispatch")) 2>> $logFile | Out-Null
		}
		else {
			Write-Host "! Could not find LUIS file: $(Join-Path $luisFolder "$($luisApp).luis")" -ForegroundColor DarkRed
			Break
		}
	}
}
catch {
	Break
}

Write-Host "> Running dispatch refresh ..."
(dispatch refresh `
	--dispatch $(Join-Path $dispatchFolder "$($dispatchName).dispatch") `
	--dataFolder $dispatchFolder) 2>> $logFile | Out-Null

if (Test-Path $dispatchJsonPath) {
	Write-Host "> Running LuisGen ..." 
	luisgen $dispatchJsonPath -cs "DispatchLuis" -o $lgOutFolder 2>> $logFile | Out-Null
}

Write-Host "> Initializing skill.config ..."
if (Test-Path $skillsFile) {
   $skillConfig = Get-Content $skillsFile | ConvertFrom-Json

   if ($skillConfig) {
       if ($skillConfig.skills) {
           if ($skillConfig.skills.Id -eq $manifest.Id) {
               Write-Host "! $($manifest.Id) is already registered." -ForegroundColor DarkRed
				$alreadyRegistered = $true;
           }
           else {
               Write-Host "> Registering $($manifest.Id) ..."
               $skillConfig.skills += $manifest
           }
       }
       else {
           Write-Host "> Registering $($manifest.Id) ..."
           $skills = @($manifest)
           $skillConfig | Add-Member -Type NoteProperty -Force -Name "skills" -Value $skills
       }
   }
}

if (-not $alreadyRegistered) {

	if (-not $skillConfig) {
		$skillConfig = @{ skills = @($manifest) }
	}

	$skillConfig | ConvertTo-Json -depth 100 | Out-File $skillsFile

	# configuring bot auth settings
	Write-Host "> Checking for authentication settings ..."
	if ($manifest.authenticationConnections) {
		if ($manifest.authenticationConnections | Where-Object { $_.serviceProviderId -eq "Azure Active Directory v2" })
		{
			Write-Host "> Configuring Azure AD connection ..."
			$aadConfig = $manifest.authenticationConnections | Where-Object { $_.serviceProviderId -eq "Azure Active Directory v2" } | Select-Object -First 1
			$connectionName = $aadConfig.Id
			$newScopes = $aadConfig.scopes -Split ", "
			$scopes = $newScopes

			# check for existing aad connection
			$connections = az bot authsetting list `
				-n $botName `
				-g $resourceGroup `
				| ConvertFrom-Json

			if ($connections -and ($connections | Where-Object {$_.properties.serviceProviderDisplayName -eq "Azure Active Directory v2" })) {
				$aadConnection = $connections | Where-Object {$_.properties.serviceProviderDisplayName -eq "Azure Active Directory v2" } | Select-Object -First 1
				$settingName = $($aadConnection.name -Split "/")[1]

				# Get current aad auth setting
				$botAuthSetting = (az bot authsetting show `
					-n $botName	`
					-g $resourceGroup `
					-c $settingName	| ConvertFrom-Json) 2>> $logFile

				$existingScopes = $botAuthSetting.properties.scopes -Split " "
				$scopes += $existingScopes
				$connectionName = $settingName

				# delete current aad auth connection
				(az bot authsetting delete -n $botName -g $resourceGroup -c $settingName) 2>> $logFile | Out-Null
			}

			# update appsettings.json
			Write-Host "> Updating appsettings.json ..."
			$appSettings = Get-Content $appSettingsFile | ConvertFrom-Json

			# check for and remove existing aad connections
			if ($appSettings.oauthConnections) {
				$appSettings.oauthConnections = @($appSettings.oauthConnections | Where-Object -FilterScript { $_.provider -ne "Azure Active Directory v2" })
			}

			# set or add new oauth setting
			$oauthSetting = @{ "name" = $connectionName; "provider" = "Azure Active Directory v2" }
			if (-not $appSettings.oauthConnections) {
				$appSettings | Add-Member -Type NoteProperty -Force -Name 'oauthConnections' -Value @($oauthSetting)
			}
			else {
				$appSettings.oauthConnections += @($oauthSetting)
			}

			# update appsettings.json
			ConvertTo-Json $appSettings -depth 100 | Out-File $appSettingsFile

			# Remove duplicate scopes
			$scopes = $scopes | Select -unique
			$scopeManifest = $(CreateScopeManifest($scopes)).Replace("`"", "'")

			# Update MSA scopes
			Write-Host "> Configuring MSA app scopes ..."
			$errorResult = az ad app update `
				--id "$($appSettings.microsoftAppId)" `
				--required-resource-accesses "`"[$($scopeManifest)]`"" 2>&1

			#  Catch error: Updates to converged applications are not allowed in this version.
			if ($errorResult) {
				Write-Host "! Could not configure scopes automatically." -ForegroundColor Cyan
				$manualScopesRequired = $true
			}

			Write-Host "> Updating bot oauth settings ..."
			(az bot authsetting create `
				--name $botName `
				--resource-group $resourceGroup `
				--setting-name $connectionName `
				--client-id "`"$($appSettings.microsoftAppId)`"" `
				--client-secret "`"$($appSettings.microsoftAppPassword)`"" `
				--service Aadv2 `
				--parameters clientId="`"$($appSettings.microsoftAppId)`"" clientSecret="`"$($appSettings.microsoftAppPassword)`"" tenantId=common `
				--provider-scope-string "$($scopes)") 2>> $logFile | Out-Null	
		}
		else {
			Write-Host "! Could not configure authentication connection automatically." -ForegroundColor Cyan
			$manualAuthRequired = $true
		}
	}

	if ($manualScopesRequired) {
		Write-Host "+ Could not configure scopes automatically. You must configure the following scopes in the Azure Portal to use this skill: $($newScopes -Join ', ')" -ForegroundColor Magenta
	}

	if ($manualAuthRequired) {
		Write-Host "+ Could not configure authentication connection automatically. You must configure one of the following connection types manually in the Azure Portal: $($manifest.authenticationConnections.serviceProviderId -Join ', ')" -ForegroundColor Magenta
	}
}

Write-Host "> Done."