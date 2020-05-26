#Requires -Version 6

Param(
    [switch] $RemoteToLocal,
    [switch] $useGov,
    [string] $configFile = $(Join-Path (Get-Location) 'cognitivemodels.json'),
	[string] $qnaEndpoint = "https://westus.api.cognitive.microsoft.com/qnamaker/v4.0",
    [string] $qnaFolder = $(Join-Path $PSScriptRoot '..' 'Resources' 'QnA'),
    [string] $logFile = $(Join-Path $PSScriptRoot .. "update_cognitive_models_log.txt"),
    [string[]] $excludedKbFromDispatch = @("Chitchat")
)

. $PSScriptRoot\qna_functions.ps1

# Reset log file
if (Test-Path $logFile) {
    Clear-Content $logFile -Force | Out-Null
}
else {
    New-Item -Path $logFile | Out-Null
}

# Check for AZ CLI and confirm version
if (Get-Command az -ErrorAction SilentlyContinue) {
    $azcliversionoutput = az -v
    [regex]$regex = '(\d{1,3}.\d{1,3}.\d{1,3})'
    [version]$azcliversion = $regex.Match($azcliversionoutput[0]).value
    [version]$minversion = '2.2.0'

    if ($azcliversion -ge $minversion) {
        $azclipassmessage = "AZ CLI passes minimum version. Current version is $azcliversion"
        Write-Debug $azclipassmessage
        $azclipassmessage | Out-File -Append -FilePath $logfile
    }
    else {
        $azcliwarnmessage = "You are using an older version of the AZ CLI, `
    please ensure you are using version $minversion or newer. `
    The most recent version can be found here: http://aka.ms/installazurecliwindows"
        Write-Warning $azcliwarnmessage
        $azcliwarnmessage | Out-File -Append -FilePath $logfile
    }
}
else {
    $azclierrormessage = 'AZ CLI not found. Please install latest version.'
    Write-Error $azclierrormessage
    $azclierrormessage | Out-File -Append -FilePath $logfile
}

if ($useGov) {
    $cloud = 'us'
}
else {
    $cloud = 'com'
}

Write-Host "> Getting config file ..." -NoNewline
$languageMap = @{ }
$config = Get-Content -Encoding utf8 -Raw -Path $configFile | ConvertFrom-Json
$config.cognitiveModels.PSObject.Properties | Foreach-Object { $languageMap[$_.Name] = $_.Value }
Write-Host "Done." -ForegroundColor Green

foreach ($langCode in $languageMap.Keys) {
    $models = $languageMap[$langCode]

        if ($RemoteToLocal) {
        # Update local LU files based on hosted QnA KBs
        foreach ($kb in $models.knowledgebases) {
            Write-Host "> Updating local $($langCode) $($kb.id).qna file ..." -NoNewline
            bf qnamaker:kb:export `
                --endpoint $qnaEndpoint `
                --environment Prod `
                --kbId $kb.knowledgebaseid `
                --subscriptionKey $kb.subscriptionKey > $(Join-Path $qnaFolder $langCode "$($kb.id).json")
                
            bf qnamaker:convert `
                --in $(Join-Path $qnaFolder $langCode "$($kb.id).json") `
                --out $(Join-Path $qnaFolder $langCode "$($kb.id).qna") `
                --force 2>> $logFile | Out-Null
            Write-Host "Done." -ForegroundColor Green
		
        }
    }
    else {
        # Update each knowledgebase based on local LU files
        foreach ($kb in $models.knowledgebases) {
            $lu = Get-Item -Path $(Join-Path $qnaFolder $langCode "$($kb.id).qna")
            UpdateKB `
                -luFile $lu `
                -kbId $kb.knowledgebaseid `
                -qnaSubscriptionKey $kb.subscriptionKey `
                -qnaEndpoint $qnaEndpoint `
                -language $langCode `
                -log $logFile
        }
    }

}

Write-Host "> Update complete." -ForegroundColor Green
