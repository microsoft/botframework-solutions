Param(
    [string] $jsonFile,
    [switch] $useDispatch = $false,
    [int] $languageModels = 1,
    [int] $knowledgeBases = 1,
    [string] $languages = "en-us"
)

$config = Get-Content -Raw -Path $jsonFile | ConvertFrom-Json

# Create reused variables
$dispatchModel = [pscustomobject]@{
    authoringKey = ""
    authoringRegion = ""
    appId = ""
    name = ""
    region = ""
    subscriptionKey = ""
    type = "dispatch"
}

$languageModel = [pscustomobject]@{
    appId = ""
    authoringKey = ""
    authoringRegion = ""
    id = ""
    name = ""
    region = ""
    subscriptionKey = ""
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

# Create a language block object with or without a dispatch model
if ($useDispatch){
    $languageBlock = [pscustomobject]@{
    dispatchModel = $dispatchModel
    languageModels = @()
    knowledgebases = @()
    }
} else {
    $languageBlock = [pscustomobject]@{
        languageModels = @()
        knowledgebases = @()
    }
}

# Add language models
for($i = 0; $i -lt $languageModels; $i++){
    $languageBlock.languageModels += $languageModel
}

# Add knowledge bases
for($i = 0; $i -lt $knowledgeBases; $i++){
    $languageBlock.knowledgebases += $knowledgeBase
}

# Get languages
$languageArr = $languages -split ","

# Add block of models for each language
foreach ($language in $languageArr){
    Write-Host $language "Adding ${language}: ${languageBlock}"
    $config.cognitiveModels | Add-Member -Force -Name $language -Value $languageBlock -MemberType NoteProperty
}

$config | ConvertTo-Json -depth 4 | Set-Content $jsonFile
