Param(
	[Parameter(Mandatory=$true)][string] $name,
    [Parameter(Mandatory=$true)][string] $location,
    [Parameter(Mandatory=$true)][string] $luisAuthoringKey,
    [Parameter(Mandatory=$true)][string] $qnaSubscriptionKey,
    [string] $languages = "en-us",
    [string] $outFolder = $(Get-Location)
)

. $PSScriptRoot\luis_functions.ps1
. $PSScriptRoot\qna_functions.ps1
$qnamaker = "qnamaker"; 
# Initialize settings obj
$settings = @{ cognitiveModels = New-Object PSObject }

# Deploy localized resources
Write-Host "Deploying cognitive models ..."
foreach ($language in $languages -split ",")
{
    $langCode = ($language -split "-")[0]

    $config = @{
        dispatchModel = New-Object PSObject
        languageModels = @()
        knowledgeBases = @()
    }

    # Initialize Dispatch
    Write-Host "Initializing dispatch model ..."
    $dispatchName = "$($name)$($langCode)_Dispatch"
    $dataFolder = Join-Path $PSScriptRoot .. Resources Dispatch $langCode
    $dispatch = dispatch init `
        --name $dispatchName `
        --luisAuthoringKey $luisAuthoringKey `
        --luisAuthoringRegion $location `
        --dataFolder $dataFolder

    # Deploy LUIS apps
    $luisFiles = Get-ChildItem "$(Join-Path $PSScriptRoot .. 'Resources' 'LU' $langCode)" | Where {$_.extension -eq ".lu"}
    foreach ($lu in $luisFiles)
    {
        # Deploy LUIS model
        $luisApp = DeployLUIS -name $name -lu_file $lu -region $location -luisAuthoringKey $luisAuthoringKey -language $language
        
        # Add luis app to dispatch
        Write-Host "Adding $($lu.BaseName) app to dispatch model ..."
        dispatch add `
            --type "luis" `
            --name $luisApp.name `
            --id $luisApp.id  `
            --intentName "l_$($lu.BaseName)" `
            --dataFolder $dataFolder `
            --dispatch "$(Join-Path $dataFolder "$($dispatchName).dispatch")" | Out-Null
        
        # Add to config 
        $config.languageModels += @{
            id = $lu.BaseName
            name = $luisApp.name
            appId = $luisApp.id
            authoringKey = $luisauthoringkey
            subscriptionKey = $luisauthoringkey
            version = $luisApp.activeVersion
            region = $location
        }
    }

    # Deploy QnA Maker KBs
    $qnaFiles = Get-ChildItem "$(Join-Path $PSScriptRoot .. 'Resources' 'QnA' $langCode)" -Recurse | Where {$_.extension -eq ".lu"} 
    foreach ($lu in $qnaFiles)
    {
        # Deploy QnA Knowledgebase
        $qnaKb = DeployKB -name $name -lu_file $lu -qnaSubscriptionKey $qnaSubscriptionKey
       
        # Add luis app to dispatch
        Write-Host "Adding $($lu.BaseName) kb to dispatch model ..."        
        dispatch add `
            --type "qna" `
            --name $qnaKb.name `
            --id $qnaKb.id  `
            --key $qnaSubscriptionKey `
            --intentName "q_$($lu.BaseName)" `
            --dataFolder $dataFolder `
            --dispatch "$(Join-Path $dataFolder "$($dispatchName).dispatch")" | Out-Null
        
        # Add to config
        $config.knowledgeBases += @{
            id = $lu.BaseName
            name = $qnaKb.name
            kbId = $qnaKb.kbId
            subscriptionKey = $qnaKb.subscriptionKey
            endpointKey = $qnaKb.endpointKey
            hostname = "$($qnaKb.hostname)/$($qnamaker)" 
        }
    }

    # Create dispatch model
    Write-Host "Creating dispatch model..."  
    $dispatch = dispatch create `
        --dispatch "$(Join-Path $dataFolder "$($dispatchName).dispatch")" `
        --dataFolder  $dataFolder `
        --culture $language | ConvertFrom-Json

    # Add to config
    $config.dispatchModel = @{
        type = "dispatch"
        name = $dispatch.name
        appId = $dispatch.appId
        authoringKey = $luisauthoringkey
        subscriptionKey = $luisauthoringkey
        region = $location   
    }

    # Add config to cognitivemodels dictionary
    $settings.cognitiveModels | Add-Member -Type NoteProperty -Force -Name $langCode -Value $config
}
$settings | Add-Member -Type NoteProperty -Force -Name "defaultLocale" -Value "en"

# Write out config to file
$settings | ConvertTo-Json -depth 100 | Out-File $(Join-Path $outFolder "cognitivemodels.json" )