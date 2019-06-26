#Requires -Version 6

Param(
	[string] $name,
	[string] $luisAuthoringRegion,
    [string] $luisAuthoringKey,
    [string] $languages = "en-us",
    [string] $outFolder = $(Get-Location),
	[string] $logFile = $(Join-Path $PSScriptRoot .. "deploy_cognitive_models_log.txt")
)

. $PSScriptRoot\luis_functions.ps1


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
        languageModels = @()
    }

    # Deploy LUIS apps
    $luisFiles = Get-ChildItem "$(Join-Path $PSScriptRoot .. 'Resources' 'LU' $langCode)" | Where {$_.extension -eq ".lu"}
    foreach ($lu in $luisFiles)
    {
        # Deploy LUIS model
        $luisApp = DeployLUIS -name $name -lu_file $lu -region $luisAuthoringRegion -luisAuthoringKey $luisAuthoringKey -language $language -log $logFile
        
		if ($luisApp) {
			# Add to config 
			$config.languageModels += @{
				id = $lu.BaseName
				name = $luisApp.name
				appid = $luisApp.id
				authoringkey = $luisAuthoringKey
				subscriptionkey = $luisAuthoringKey
				version = $luisApp.activeVersion
				region = $luisAuthoringRegion
			}

			# Disable this due to https://github.com/Microsoft/botbuilder-tools/issues/1008
			#RunLuisGen $lu "$($lu.BaseName)" $(Join-Path $outFolder Services)
		}
		else {
			Write-Host "! Deployment failed for LUIS app: $($lu.BaseName)" -ForegroundColor Cyan
		}
    }

    # Add config to cognitivemodels dictionary
    $settings.cognitiveModels | Add-Member -Type NoteProperty -Force -Name $langCode -Value $config
}

# Write out config to file
$settings | ConvertTo-Json -depth 100 | Out-File $(Join-Path $outFolder "cognitivemodels.json" )