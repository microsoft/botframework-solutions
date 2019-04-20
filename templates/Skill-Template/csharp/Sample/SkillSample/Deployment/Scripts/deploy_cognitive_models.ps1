Param(
	[Parameter(Mandatory=$true)][string] $name,
    [Parameter(Mandatory=$true)][string] $location,
    [Parameter(Mandatory=$true)][string] $luisAuthoringKey,
    [string] $languages = "en-us",
    [string] $outFolder = $(Get-Location)
)

. $PSScriptRoot\luis_functions.ps1

# Initialize settings obj
$settings = @{ cognitiveModels = New-Object PSObject }

# Deploy localized resources
Write-Host "Deploying cognitive models ..."
foreach ($language in $languages -split ",")
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
        $luisApp = DeployLUIS -name $name -lu_file $lu -region $location -luisAuthoringKey $luisAuthoringKey -language $language
        
        # Add to config 
        $config.languageModels += @{
            id = $lu.BaseName
            name = $luisApp.name
            appid = $luisApp.id
            authoringkey = $luisauthoringkey
            subscriptionkey = $luisauthoringkey
            version = $luisApp.activeVersion
            region = $location
        }

		RunLuisGen $lu "$($lu.BaseName)" $(Join-Path $outFolder Services)
    }

    # Add config to cognitivemodels dictionary
    $settings.cognitiveModels | Add-Member -Type NoteProperty -Force -Name $langCode -Value $config
}

# Write out config to file
$settings | ConvertTo-Json -depth 100 | Out-File $(Join-Path $outFolder "cognitivemodels.json" )