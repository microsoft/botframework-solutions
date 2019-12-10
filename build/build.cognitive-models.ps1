$jsonfile = '.\templates\Virtual-Assistant-Template\csharp\Sample\VirtualAssistantSample\CognitiveModels.json'
$config = Get-Content -Raw -Path $jsonfile | ConvertFrom-Json
$languageArr = "en-us", "de-de", "fr-fr", "it-it", "zh-cn"

$dispatchModel = [pscustomobject]@{
    authoringkey = ""
    authoringRegion = ""
    appid = ""
    name = ""
    region = ""
    subscriptionkey = ""
    type = "dispatch"
}

$languageModel = [pscustomobject]@{
    appid = ""
    authoringkey = ""
    authoringRegion = ""
    id = ""
    name = ""
    region = ""
    subscriptionkey = ""
    version = ""
}

$knowledgeBase = [pscustomobject]@{
    endpointKey = ""
    id = ""
    hostname = ""
    kbId = ""
    name = ""
    subscriptionKey = ""
}

$languageBlock = [pscustomobject]@{
    dispatchModel = $dispatchModel
    languageModels = @($languageModel)
    knowledgeBases = @(
        $knowledgeBase
        $knowledgeBase
    )
}

foreach ($language in $languageArr){
    # If english, add additional knowledge base
    # If other, add all
    if ($language -eq "en-us"){
        $config.cognitiveModels.$language.knowledgebases += $knowledgeBase
    }
    else {
        $config.cognitiveModels | Add-Member -Name $language -Value $languageBlock -MemberType NoteProperty
    }
}

$config | ConvertTo-Json -depth 5 | Set-Content $jsonfile