#Requires -Version 6

Param(
	[string] $name,
	[string] $luisAuthoringRegion,
    [string] $luisAuthoringKey,
    [string] $qnaSubscriptionKey,
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
    $resourceGroup = $name
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

if (-not $qnaSubscriptionKey) {
    $qnaSubscriptionKey = Read-Host "? QnA Maker Subscription Key"
}

# Get languages
$languageArr = $languages -split ","

# Initialize settings obj
$settings = @{ defaultLocale = $languageArr[0]; cognitiveModels = New-Object PSObject }

# Deploy localized resources
Write-Host "> Deploying cognitive models ..."
foreach ($language in $languageArr)
{
    $langCode = ($language -split "-")[0]

    $config = @{
        dispatchModel = New-Object PSObject
        languageModels = @()
        knowledgebases = @()
    }

    # Initialize Dispatch
    Write-Host "> Initializing dispatch model ..."
    $dispatchName = "$($name)$($langCode)_Dispatch"
    $dataFolder = Join-Path $PSScriptRoot .. Resources Dispatch $langCode
    (dispatch init `
        --name $dispatchName `
        --luisAuthoringKey $luisAuthoringKey `
        --luisAuthoringRegion $luisAuthoringRegion `
        --dataFolder $dataFolder) 2>> $logFile | Out-Null

    # Deploy LUIS apps
    $luisFiles = Get-ChildItem "$(Join-Path $PSScriptRoot .. 'Resources' 'LU' $langCode)" | Where {$_.extension -eq ".lu"}
    foreach ($lu in $luisFiles)
    {
        # Deploy LUIS model
        $luisApp = DeployLUIS -name $name -lu_file $lu -region $luisAuthoringRegion -luisAuthoringKey $luisAuthoringKey -language $language -log $logFile
        
		if ($luisApp) {
			 # Add luis app to dispatch
			Write-Host "> Adding $($lu.BaseName) app to dispatch model ..."
			(dispatch add `
				--type "luis" `
				--name $luisApp.name `
				--id $luisApp.id  `
				--intentName "l_$($lu.BaseName)" `
				--dataFolder $dataFolder `
				--dispatch "$(Join-Path $dataFolder "$($dispatchName).dispatch")") 2>> $logFile | Out-Null
        
			# Add to config 
			$config.languageModels += @{
				id = $lu.BaseName
				name = $luisApp.name
				appid = $luisApp.id
				authoringkey = $luisauthoringkey
				subscriptionkey = $luisauthoringkey
				version = $luisApp.activeVersion
				region = $luisAuthoringRegion
			}
		}
		else {
			Write-Host "! Could not create LUIS app. Skipping dispatch add." -ForegroundColor Cyan
		}
    }

    # Deploy QnA Maker KBs
    $qnaFiles = Get-ChildItem "$(Join-Path $PSScriptRoot .. 'Resources' 'QnA' $langCode)" -Recurse | Where {$_.extension -eq ".lu"} 
    foreach ($lu in $qnaFiles)
    {
        # Deploy QnA Knowledgebase
        $qnaKb = DeployKB -name $name -lu_file $lu -qnaSubscriptionKey $qnaSubscriptionKey -log $logFile
       
		if ($qnaKb) {
			# Add luis app to dispatch
			Write-Host "> Adding $($lu.BaseName) kb to dispatch model ..."        
			(dispatch add `
				--type "qna" `
				--name $qnaKb.name `
				--id $qnaKb.id  `
				--key $qnaSubscriptionKey `
				--intentName "q_$($lu.BaseName)" `
				--dataFolder $dataFolder `
				--dispatch "$(Join-Path $dataFolder "$($dispatchName).dispatch")") 2>> $logFile | Out-Null
        
			# Add to config
			$config.knowledgebases += @{
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

	    # Add to config
		$config.dispatchModel = @{
			type = "dispatch"
			name = $dispatchApp.name
			appid = $dispatchApp.appId
			authoringkey = $luisauthoringkey
			subscriptionkey = $luisauthoringkey
			region = $luisAuthoringRegion
		}
	}

    # Add config to cognitivemodels dictionary
    $settings.cognitiveModels | Add-Member -Type NoteProperty -Force -Name $langCode -Value $config
}

# Write out config to file
$settings | ConvertTo-Json -depth 100 | Out-File $(Join-Path $outFolder "cognitivemodels.json" )