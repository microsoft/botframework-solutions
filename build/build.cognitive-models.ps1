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

# Create a language block object with or without a dispatch model
if ($useDispatch){
    $languageBlock = [pscustomobject]@{
    dispatchModel = $dispatchModel
    languageModels = @()
    knowledgeBases = @()
    }
} else {
    $languageBlock = [pscustomobject]@{
        languageModels = @()
        knowledgeBases = @()
    }
}

# Add language models
for($i = 0; $i -lt $languageModels; $i++){
    $languageBlock.languageModels += $languageModel
}

# Add knowledge bases
for($i = 0; $i -lt $knowledgeBases; $i++){
    $languageBlock.knowledgeBases += $knowledgeBase
}

# Get languages
$languageArr = $languages -split ","

# Add block of models for each language
foreach ($language in $languageArr){
    Write-Host "Adding block for $language:"
    Write-Host $languageBlock
    $config.cognitiveModels | Add-Member -Force -Name $language -Value $languageBlock -MemberType NoteProperty
}

$config | ConvertTo-Json -depth 4 | Set-Content -Encoding utf8 $jsonFile