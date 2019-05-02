function DeployLUIS ($name, $lu_file, $region, $luisAuthoringKey, $language)
{
    $id = $lu_file.BaseName
    $outFile = "$($id).luis"
    $outFolder = $lu_file.DirectoryName
    $appName = "$($name)$($langCode)_$($id)"
    
    # Parse LU file
    Write-Host "Parsing $($id) LU file ..."
    ludown parse toluis `
        --in $lu_file `
        --luis_culture $language `
        --out_folder $outFolder `
        --out $outFile        
        
    # Create LUIS app
    Write-Host "Deploying $($id) LUIS app ..."
    $luisApp = luis import application `
        --appName $appName `
        --authoringKey $luisAuthoringKey `
        --subscriptionKey $luisAuthoringKey `
        --region $region `
        --in "$(Join-Path $outFolder $outFile)" `
        --wait | ConvertFrom-Json

    # train and publish luis app
    $(luis train version --appId $luisApp.id --authoringKey $luisAuthoringKey --versionId $luisApp.activeVersion --wait 
    & luis publish version --appId $luisApp.id --authoringKey $luisAuthoringKey --versionId $luisApp.activeVersion --wait) 2>&1 | Out-Null

    Return $luisApp
}

function UpdateLUIS ($lu_file, $appId, $version, $authoringKey, $subscriptionKey)
{
    $id = $lu_file.BaseName
    $outFile = "$($id).luis"
    $outFolder = $lu_file.DirectoryName

    $luisApp = luis get application --appId $appId --authoringKey $authoringKey | ConvertFrom-Json

    # Parse LU file
    Write-Host "Parsing $($id) LU file ..."
    ludown parse toluis `
        --in $lu_file `
        --luis_culture $luisApp.culture `
        --out_folder $outFolder `
        --out $outFile
    
    Write-Host "Getting current versions ..."
    # Get list of current versions
	$versions = luis list versions `
        --appId $appId `
        --authoringKey $authoringKey | ConvertFrom-Json
    
    # If the current version exists
    if ($versions | Where { $_.version -eq $version })
    {
        # delete any old backups
        if ($versions | Where { $_.version -eq "backup" })
        {
            Write-Host "Deleting old backup version ..."
            luis delete version `
                --appId $appId `
                --versionId "backup" `
                --authoringKey $authoringKey `
                --force --wait | Out-Null
        }
        
        # rename the active version to backup
        Write-Host "Saving current version as backup ..."
	    luis rename version `
            --appId $appId `
            --versionId $version `
            --newVersionId backup `
            --authoringKey $authoringKey `
            --subscriptionKey $subscriptionKey `
            --wait | Out-Null
    }
    
    # import the new 0.1 version from the .luis file
    Write-Host "Importing new version ..."
    luis import version `
        --appId $appId `
        --versionId $version `
        --authoringKey $authoringKey `
        --subscriptionKey $subscriptionKey `
        --in "$(Join-Path $outFolder $outFile)" `
        --wait | ConvertFrom-Json
    
    # train and publish luis app
    $(luis train version --appId $appId --authoringKey $authoringKey --versionId $version --wait 
    & luis publish version --appId $appId --authoringKey $authoringKey --versionId $version --wait) 2>&1 | Out-Null
}