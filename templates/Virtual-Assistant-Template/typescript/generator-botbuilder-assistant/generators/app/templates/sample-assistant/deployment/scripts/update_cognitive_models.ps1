#Requires -Version 6

Param(
    [switch] $RemoteToLocal,
    [switch] $useLuisGen = $true,
    [string] $configFile = $(Join-Path (Get-Location) 'src' 'cognitivemodels.json'),
    [string] $dispatchFolder = $(Join-Path $PSScriptRoot '..' 'resources' 'Dispatch'),
	[string] $luisFolder = $(Join-Path $PSScriptRoot '..' 'resources' 'LU'),
    [string] $qnaFolder = $(Join-Path $PSScriptRoot '..' 'resources' 'QnA'),
    [string] $lgOutFolder = $(Join-Path (Get-Location) 'src' 'services'),
    [string] $logFile = $(Join-Path $PSScriptRoot .. "update_cognitive_models_log.txt")
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

Write-Host "> Getting config file ..."
$languageMap = @{}
$config = Get-Content -Raw -Path $configFile | ConvertFrom-Json
$config.cognitiveModels.PSObject.Properties | Foreach-Object { $languageMap[$_.Name] = $_.Value }

foreach ($langCode in $languageMap.Keys) {
    $models = $languageMap[$langCode]
    $dispatch = $models.dispatchModel

    if($RemoteToLocal)
    {
        # Update local LU files based on hosted models
        foreach ($luisApp in $models.languageModels) {
            $culture = (luis get application `
                    --appId $luisApp.appId `
                    --authoringKey $luisApp.authoringKey `
                    --subscriptionKey $luisApp.subscriptionKey `
                    --region $luisApp.authoringRegion | ConvertFrom-Json).culture

            Write-Host "> Updating local $($luisApp.id).lu file ..."
            luis export version `
                --appId $luisApp.appId `
                --versionId $luisApp.version `
                --region $luisApp.authoringRegion `
                --authoringKey $luisApp.authoringKey | ludown refresh `
                --stdin `
                -n "$($luisApp.id).lu" `
                -o $(Join-Path $luisFolder $langCode)

            # Parse LU file
			$id = $luisApp.id
			$outFile = "$($id).luis"
			$outFolder = $(Join-Path $luisFolder $langCode)
			$appName = "$($name)$($langCode)_$($id)"

			Write-Host "> Parsing $($luisApp.id) LU file ..."
			ludown parse toluis `
				--in $(Join-Path $outFolder "$($luisApp.id).lu") `
				--luis_culture $culture `
				--out_folder $(Join-Path $luisFolder $langCode) `
				--out "$($luisApp.id).luis"
            if ($useLuisGen) {
                Write-Host "> Running LuisGen for $($luisApp.id) app ..."
                $luPath = $(Join-Path $luisFolder $langCode "$($luisApp.id).lu")
                RunLuisGen -lu_file $(Get-Item $luPath) -outName "$($luisApp.id)" -outFolder $lgOutFolder
            }

            # Add the LUIS application to the dispatch model. 
            # If the LUIS application id already exists within the model no action will be taken
            if ($dispatch) {
                Write-Host "> Adding $($luisApp.id) app to dispatch model ... "
                (dispatch add `
                    --type "luis" `
                    --name $luisApp.name `
                    --id $luisApp.appid  `
                    --region $luisApp.authoringRegion `
                    --intentName "l_$($luisApp.id)" `
                    --dispatch $(Join-Path $dispatchFolder $langCode "$($dispatch.name).dispatch") `
                    --dataFolder $(Join-Path $dispatchFolder $langCode))  2>> $logFile | Out-Null
        
            }
        }

        # Update local LU files based on hosted QnA KBs
        foreach ($kb in $models.knowledgeBases)
        {
            Write-Host "> Updating local $($kb.id).lu file ..."
            qnamaker export kb `
                --environment Prod `
                --kbId $kb.kbId `
                --subscriptionKey $kb.subscriptionKey | ludown refresh `
                --stdin `
                -n "$($kb.id).lu" `
                -o $(Join-Path $qnaFolder $langCode)

            # Add the knowledge base to the dispatch model. 
            # If the knowledge base id already exists within the model no action will be taken
            if ($dispatch) {
                Write-Host "> Adding $($kb.id) kb to dispatch model ..."   
                (dispatch add `
                    --type "qna" `
                    --name $kb.name `
                    --id $kb.kbId  `
                    --key $kb.subscriptionKey  `
                    --intentName "q_$($kb.id)" `
                    --dispatch $(Join-Path $dispatchFolder $langCode "$($dispatch.name).dispatch") `
                    --dataFolder $(Join-Path $dispatchFolder $langCode))  2>> $logFile | Out-Null
            }
        }
    }
    else{
        # Update each luis model based on local LU files
		foreach ($luisApp in $models.languageModels) {
            Write-Host "> Updating hosted $($luisApp.id) app..."
			$lu = Get-Item -Path $(Join-Path $luisFolder $langCode "$($luisApp.id).lu")
			UpdateLUIS `
				-lu_file $lu `
				-appId $luisApp.appId `
                -version $luisApp.version `
                -region $luisApp.region `
				-authoringKey $luisApp.authoringKey `
				-subscriptionKey $luisApp.subscriptionKey
		}

        # Update each knowledgebase based on local LU files
		foreach ($kb in $models.knowledgeBases) {
            Write-Host "> Updating hosted $($kb.id) kb..."
			$lu = Get-Item -Path $(Join-Path $qnaFolder $langCode "$($kb.id).lu")
			UpdateKB `
				-lu_file $lu `
				-kbId $kb.kbId `
				-qnaSubscriptionKey $kb.subscriptionKey
        }
	}
    if ($dispatch) {
        # Update dispatch model
        Write-Host "> Updating dispatch model ..."
        dispatch refresh `
            --dispatch $(Join-Path $dispatchFolder $langCode "$($dispatch.name).dispatch") `
            --dataFolder $(Join-Path $dispatchFolder $langCode)  2>> $logFile | Out-Null
        if ($useLuisGen) {
        # Update dispatch.cs file
            Write-Host "> Running LuisGen ..."
            luisgen $(Join-Path $dispatchFolder $langCode "$($dispatch.name).json") -ts "DispatchLuis" -o $lgOutFolder 2>> $logFile | Out-Null
        }
    }
}

Write-Host "> Done."