#Requires -Version 6

Param(
	[string] $name,
	[string] $luisAuthoringRegion,
	[string] $luisAuthoringKey,
	[string] $luisAccountName,
	[string] $luisAccountRegion,
	[string] $luisSubscriptionKey,
    [string] $qnaSubscriptionKey,
	[string] $resourceGroup,
	[switch] $useDispatch = $true,
    [string] $languages = "en-us",
    [string] $outFolder = $(Get-Location),
	[string] $logFile = $(Join-Path $PSScriptRoot .. "deploy_cognitive_models_log.txt")
)

. $PSScriptRoot\luis_functions.ps1
. $PSScriptRoot\qna_functions.ps1

# Reset log file
if (Test-Path $logFile) {
	Clear-Content $logFile -Force | Out-Null
}
else {
	New-Item -Path $logFile | Out-Null
}

# Get mandatory parameters
if (-not $name) {
    $name = Read-Host "? Base name for Cognitive Models"
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

if (-not $luisAccountName) {
    $luisAccountName = Read-Host "? LUIS Service Name (exising service in Azure required)"
}

if (-not $resourceGroup) {
	$resourceGroup = $name

	$rgExists = az group exists -n $resourceGroup --output json
	if ($rgExists -eq "false")
	{
	    $resourceGroup = Read-Host "? LUIS Service Resource Group (exising service in Azure required)"
	}
}

if (-not $luisSubscriptionKey) {
	$keys = az cognitiveservices account keys list --name $luisAccountName --resource-group $resourceGroup --output json | ConvertFrom-Json

	if ($keys) {
		$luisSubscriptionKey = $keys.key1
	}
	else {
		Write-Host "! Could not retrieve LUIS Subscription Key." -ForgroundColor DarkRed
		Write-Host "+ Verify the -luisAccountName and -resourceGroup parameters are correct." -ForegroundColor Magenta
		Break
	}
}

if (-not $luisAccountRegion) {
	$luisAccountRegion = Read-Host "? LUIS Service Location"
}
if (-not $qnaSubscriptionKey) {	
	$useQna = $false
}
else {
	$useQna = $true
}

$azAccount = az account show --output json | ConvertFrom-Json
$azAccessToken = $(Invoke-Expression "az account get-access-token --output json") | ConvertFrom-Json

# Get languages
$languageArr = $languages -split ","

# Initialize settings obj
$settings = @{ defaultLocale = $languageArr[0]; cognitiveModels = New-Object PSObject }

# Deploy localized resources
Write-Host "> Deploying cognitive models ..."
foreach ($language in $languageArr)
{
	$langCode = ($language -split "-")[0]
	$config = New-Object PSObject

	if ($useDispatch) {
		# Add dispatch to config
		$config | Add-Member -MemberType NoteProperty -Name dispatchModel -Value $(New-Object PSObject)

	    # Initialize Dispatch
		Write-Host "> Initializing dispatch model ..."
		$dispatchName = "$($name)$($langCode)_Dispatch"
		$dataFolder = Join-Path $PSScriptRoot .. resources Dispatch $langCode
		(dispatch init `
			--name $dispatchName `
			--luisAuthoringKey $luisAuthoringKey `
			--luisAuthoringRegion $luisAuthoringRegion `
			--dataFolder $dataFolder) 2>> $logFile | Out-Null
	}

    # Deploy LUIS apps
    $luisFiles = Get-ChildItem "$(Join-Path $PSScriptRoot .. 'resources' 'LU' $langCode)" | Where {$_.extension -eq ".lu"}
	if ($luisFiles) {
		$config | Add-Member -MemberType NoteProperty -Name languageModels -Value @()
		
		foreach ($lu in $luisFiles)
		{
			# Deploy LUIS model
			$luisApp = DeployLUIS `
				-name $name `
				-lu_file $lu `
				-region $luisAuthoringRegion `
				-luisAuthoringKey $luisAuthoringKey `
				-language $language `
				-log $logFile
			
			Write-Host "> Setting LUIS subscription key ..."
			if ($luisApp) {
				# Setting subscription key
				$addKeyResult = luis add appazureaccount `
					--appId $luisApp.id `
					--authoringKey $luisAuthoringKey `
					--region $luisAuthoringRegion `
					--accountName $luisAccountName `
					--azureSubscriptionId $azAccount.id `
					--resourceGroup $resourceGroup `
					--armToken "$($azAccessToken.accessToken)" 2>> $logFile

				if (-not $addKeyResult) {
					$luisKeySet = $false
					Write-Host "! Could not assign subscription key automatically. Review the log for more information. " -ForegroundColor DarkRed
					Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed
					Write-Host "+ Please assign your subscription key manually in the LUIS portal." -ForegroundColor Magenta
				}
			
				if ($useDispatch) {
					# Add luis app to dispatch
					Write-Host "> Adding $($lu.BaseName) app to dispatch model ..."
					(dispatch add `
						--type "luis" `
						--name $luisApp.name `
						--id $luisApp.id  `
						--region $luisApp.region `
						--intentName "l_$($lu.BaseName)" `
						--dataFolder $dataFolder `
						--dispatch "$(Join-Path $dataFolder "$($dispatchName).dispatch")") 2>> $logFile | Out-Null
				}

				# Add to config 
				$config.languageModels += @{
					id = $lu.BaseName
					name = $luisApp.name
					appId = $luisApp.id
					authoringKey = $luisAuthoringKey
					authoringRegion = $luisAuthoringRegion
					subscriptionKey = $luisSubscriptionKey
					version = $luisApp.activeVersion
					region = $luisAccountRegion
				}
			}
			else {
				Write-Host "! Could not create LUIS app. Skipping dispatch add." -ForegroundColor Cyan
			}
		}
	}

	if ($useQna) {
		if (Test-Path $(Join-Path $PSScriptRoot .. 'resources' 'QnA' $langCode)) {
			# Deploy QnA Maker KBs
			$qnaFiles = Get-ChildItem "$(Join-Path $PSScriptRoot .. 'resources' 'QnA' $langCode)" -Recurse | Where {$_.extension -eq ".lu"} 

			if ($qnaFiles) {
				$config | Add-Member -MemberType NoteProperty -Name knowledgeBases -Value @()

				foreach ($lu in $qnaFiles)
				{
					# Deploy QnA Knowledgebase
					$qnaKb = DeployKB -name $name -lu_file $lu -qnaSubscriptionKey $qnaSubscriptionKey -log $logFile

					if ($qnaKb) {
						if ($useDispatch) {
							Write-Host "> Adding $($lu.BaseName) kb to dispatch model ..."        
							(dispatch add `
								--type "qna" `
								--name $qnaKb.name `
								--id $qnaKb.id  `
								--key $qnaSubscriptionKey `
								--intentName "q_$($lu.BaseName)" `
								--dataFolder $dataFolder `
								--dispatch "$(Join-Path $dataFolder "$($dispatchName).dispatch")") 2>> $logFile | Out-Null
						}

						# Add to config
						$config.knowledgeBases += @{
							id = $lu.BaseName
							name = $qnaKb.name
							kbId = $qnaKb.kbId
							subscriptionKey = $qnaKb.subscriptionKey
							endpointKey = $qnaKb.endpointKey
							hostname = $qnaKb.hostname
						}
					}
					else {
						Write-Host "! Could not create knowledgebase. Skipping dispatch add." -ForegroundColor Cyan
					}        
				}
			}
		}
		else {
			Write-Host "! No knowledgeBases found. Skipping." -ForegroundColor Cyan
		}
	}
	else {
		Write-Host "! No QnA Maker Subscription Key provided. Skipping knowledgeBases." -ForegroundColor Cyan
	}

	if ($useDispatch) {
		# Create dispatch model
		Write-Host "> Creating dispatch model..."  
		$dispatch = (dispatch create `
			--dispatch "$(Join-Path $dataFolder "$($dispatchName).dispatch")" `
			--dataFolder  $dataFolder `
			--culture $language) 2>> $logFile

		if (-not $dispatch) {
			Write-Host "! Could not create Dispatch app. Review the log for more information." -ForegroundColor DarkRed
			Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed
			Break
		}
		else {
			$dispatchApp  = $dispatch | ConvertFrom-Json

			# Setting subscription key
			Write-Host "> Setting LUIS subscription key ..."
			$addKeyResult = luis add appazureaccount `
				--appId $dispatchApp.appId `
				--accountName $luisAccountName `
				--authoringKey $luisAuthoringKey `
				--region $luisAuthoringRegion `
				--azureSubscriptionId $azAccount.id `
				--resourceGroup $resourceGroup `
				--armToken $azAccessToken.accessToken 2>> $logFile

			if (-not $addKeyResult) {
				$luisKeySet = $false
				Write-Host "! Could not assign subscription key automatically. Review the log for more information. " -ForegroundColor DarkRed
				Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed
				Write-Host "+ Please assign your subscription key manually in the LUIS portal." -ForegroundColor Magenta
			}

			# Add to config
			$config.dispatchModel = @{
				type = "dispatch"
				name = $dispatchApp.name
				appId = $dispatchApp.appId
				authoringKey = $luisAuthoringKey
				authoringRegion = $luisAuthoringRegion
				subscriptionKey = $luisSubscriptionKey
				region = $luisAuthoringRegion
			}
		}
	}

    # Add config to cognitivemodels dictionary
    $settings.cognitiveModels | Add-Member -Type NoteProperty -Force -Name $langCode -Value $config
}

# Write out config to file
$settings | ConvertTo-Json -depth 100 | Out-File $(Join-Path $outFolder "cognitivemodels.json" )