Param(
    [Parameter(Mandatory=$true)][string] $config_files,
    [switch] $RemoteToLocal
)

. $PSScriptRoot\luis_functions.ps1
. $PSScriptRoot\qna_functions.ps1

foreach ($filePath in $config_files -split ",")
{
    $file = Get-Item -Path $filePath
    $fileName = $file | ForEach-Object { $_.BaseName } 
    $config = Get-Content -Raw -Path $file | ConvertFrom-Json
    $langCode = $fileName.Substring($fileName.Length -2, 2)

    if($RemoteToLocal)
    {
        # Update LUIS apps
        foreach ($app in $config.cognitiveModels | Where-Object { $_.type -eq "luis" })
        {
            luis export version `
                --appId $app.appid `
                --versionId $app.version `
                --authoringKey $app.authoringKey | ludown refresh `
                --stdin `
                -n "$($app.id).lu" `
                -o $(Join-Path $PSScriptRoot '..' 'Resources' 'LU' $langCode)
        }

        # Update QnA Maker KBs
        foreach ($kb in $config.cognitiveModels | Where-Object { $_.type -eq "qna" })
        {          
            qnamaker export kb `
                --environment Prod `
                --kbId $kb.kbId `
                --subscriptionKey $kb.subscriptionKey | ludown refresh `
                --stdin `
                -n "$($kb.id).lu" `
                -o $(Join-Path $PSScriptRoot '..' 'Resources' 'QnA' $langCode)
        }
    }
    else
    {
        # Update LUIS apps
        foreach ($app in $config.cognitiveModels | Where-Object { $_.type -eq "luis" })
        {
            $lu = Get-Item -Path $(Join-Path $PSScriptRoot '..' 'Resources' 'LU' $langCode "$($app.id).lu")
            UpdateLUIS -lu_file $lu -appId $app.appid -version $app.version -authoringKey $app.authoringKey -subscriptionKey $app.subscriptionKey
        }

        # Update QnA Maker KBs
        foreach ($kb in $config.cognitiveModels | Where-Object { $_.type -eq "qna" })
        {
            $lu = Get-Item -Path $(Join-Path $PSScriptRoot '..' 'Resources' 'QnA' $langCode "$($kb.id).lu")
            UpdateKB -lu_file $lu -kbId $kb.kbId -qnaSubscriptionKey $kb.subscriptionKey
        }
    }

    foreach ($dispatch in $config.cognitiveModels | Where-Object {$_.type -eq "dispatch"})
    {
        $dataFolder = $(Join-Path $PSScriptRoot Resources Dispatch $langCode)
        Write-Host "dispatch refresh --dispatch $(Join-Path $dataFolder $dispatch.name) --dataFolder $dataFolder"
        dispatch refresh --dispatch $(Join-Path $dataFolder $dispatch.name) --dataFolder $dataFolder
    }
}