#Requires -Version 6

$configFile = '..\..\cognitivemodels.json'
$dispatchFolder = '..\Resources\Dispatch'

Write-Host "> Getting config file ..."
$languageMap = @{}
$config = Get-Content -Raw -Path $configFile | ConvertFrom-Json
$config.cognitiveModels.PSObject.Properties | Foreach-Object { $languageMap[$_.Name] = $_.Value }

$map = $languageMap | ConvertTo-Json
Write-Host $map

foreach ($langCode in $languageMap.Keys) {
    $models = $languageMap[$langCode]
    $dispatch = $models.dispatchModel
    
    if ($dispatch)
    {
        # Create a new dispatch file based on configuration
        Write-Host "> Creating new dispatch file ..."
        dispatch init -n $dispatch.name --luisAuthoringKey $dispatch.authoringkey --luisAuthoringRegion $dispatch.region --culture $langCode --dataFolder $(Join-Path $dispatchFolder $langCode)  
                 
        # Add appId from config
        $dispatchFile = $(Join-Path $dispatchFolder $langCode "$($dispatch.name).dispatch")
        Write-Host "> dispatch file $dispatchFile"
        $dispatchConfig = Get-Content -Raw -Path $dispatchFile | ConvertFrom-Json
        $dispatchConfig | Add-Member -Name "appId" -value $dispatch.appid -MemberType NoteProperty
        $dispatchConfig | ConvertTo-Json -depth 5 | Set-Content $dispatchFile

        # Update local LU files based on hosted models
        Write-Host $models.languageModels
        foreach ($luisApp in $models.languageModels)
        {
            # Add the LUIS application to the dispatch model. 
            # If the LUIS application id already exists within the model no action will be taken
            Write-Host "> Adding $($luisApp.id) app to dispatch model ... "
            dispatch add --type "luis" --name $luisApp.name --id $luisApp.appid  --region $luisApp.region --intentName "l_$($luisApp.id)" --dispatch $(Join-Path $dispatchFolder $langCode "$($dispatch.name).dispatch") --dataFolder $(Join-Path $dispatchFolder $langCode) --verbose               
        }

        # Update local LU files based on hosted QnA KBs
        foreach ($kb in $models.knowledgebases)
        {    
            # Add the knowledge base to the dispatch model. 
            # If the knowledge base id already exists within the model no action will be taken
            if ($dispatch) {
                Write-Host "> Adding $($kb.id) kb to dispatch model ..."   
                dispatch add --type "qna" --name $kb.name --id $kb.kbId  --key $kb.subscriptionKey --intentName "q_$($kb.id)" --dispatch $(Join-Path $dispatchFolder $langCode "$($dispatch.name).dispatch") --dataFolder $(Join-Path $dispatchFolder $langCode)      
            }
        }
    }
}


Write-Host "> Done."