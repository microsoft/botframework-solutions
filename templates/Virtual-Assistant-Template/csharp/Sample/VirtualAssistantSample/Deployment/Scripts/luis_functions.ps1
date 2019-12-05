function DeployLUIS ($name, $lu_file, $region, $luisAuthoringKey, $language, $log)
{
    $id = $lu_file.BaseName
    $outFile = "$($id).luis"
    $outFolder = $lu_file.DirectoryName
    $appName = "$($name)$($langCode)_$($id)"
    
    # Parse LU file
    Write-Host "> Parsing $($id) LU file ..." -NoNewline
	bf luis:convert `
        --in $lu_file `
        --culture $language `
        --out $(Join-Path $outFolder $outFile) `
        --force 2>> $log | Out-Null
    Write-Host "Done." -ForegroundColor Green
		
    # Create LUIS app
    Write-Host "> Deploying $($id) LUIS app ..." -NoNewline
    $luisApp = (luis import application `
        --appName $appName `
        --authoringKey $luisAuthoringKey `
        --subscriptionKey $luisAuthoringKey `
        --region $region `
        --in $(Join-Path $outFolder $outFile) `
        --wait) 2>> $log | ConvertFrom-Json

	if (-not $luisApp) {
		Write-Host "! Could not deploy LUIS model. Review the log for more information." -ForegroundColor DarkRed
		Write-Host "! Log: $($log)" -ForegroundColor DarkRed
		Return $null
	}
	else {
        Write-Host "Done." -ForegroundColor Green
        Write-Host "> Publishing and training app ..." -NoNewline
	    # train and publish luis app
		$(luis train version `
            --appId $luisApp.id `
            --region $region `
            --authoringKey $luisAuthoringKey `
            --versionId $luisApp.activeVersion `
            --wait 
        & luis publish version `
            --appId $luisApp.id `
            --region $region `
            --authoringKey $luisAuthoringKey `
            --versionId $luisApp.activeVersion `
            --wait) 2>> $log | Out-Null
        Write-Host "Done." -ForegroundColor Green

		Return $luisApp
	}
}

function UpdateLUIS ($lu_file, $appId, $version, $region, $authoringKey, $subscriptionKey, $log)
{
    $id = $lu_file.BaseName
    $outFile = "$($id).luis"
    $outFolder = $lu_file.DirectoryName

    Write-Host "> Getting hosted $($id) LUIS model settings..." -NoNewline
    $luisApp = luis get application `
        --appId $appId `
        --region $region `
        --authoringKey $authoringKey | ConvertFrom-Json
    Write-Host "Done." -ForegroundColor Green

    # Parse LU file
    Write-Host "> Parsing $($id) LU file ..." -NoNewline
	bf luis:convert `
        --in $lu_file `
        --culture $luisApp.culture `
        --out $(Join-Path $outFolder $outFile) `
        --force 2>> $log | Out-Null
    Write-Host "Done." -ForegroundColor Green
     
    Write-Host "> Getting current versions ..." -NoNewline
    # Get list of current versions
	$versions = luis list versions `
        --appId $appId `
        --region $region `
        --authoringKey $authoringKey | ConvertFrom-Json
    Write-Host "Done." -ForegroundColor Green
    
    # If the current version exists
    if ($versions | Where { $_.version -eq $version })
    {
        # delete any old backups
        if ($versions | Where { $_.version -eq "backup" })
        {
            Write-Host "> Deleting old backup version ..." -NoNewline
            luis delete version `
                --appId $appId `
                --versionId "backup" `
                --region $region `
                --authoringKey $authoringKey `
                --force --wait 2>> $log | Out-Null
            Write-Host "Done." -ForegroundColor Green
        }
        
        # rename the active version to backup
        Write-Host "> Saving current version as backup ..." -NoNewline
	    luis rename version `
            --appId $appId `
            --versionId $version `
            --region $region `
            --newVersionId backup `
            --authoringKey $authoringKey `
            --subscriptionKey $subscriptionKey `
            --wait 2>> $log | Out-Null
        Write-Host "Done." -ForegroundColor Green
    }
    
    # import the new 0.1 version from the .luis file
    Write-Host "> Importing new version ..." -NoNewline
    luis import version `
        --appId $appId `
        --versionId $version `
        --region $region `
        --authoringKey $authoringKey `
        --subscriptionKey $subscriptionKey `
        --in $(Join-Path $outFolder $outFile) `
        --wait 2>> $log | Out-Null
    Write-Host "Done." -ForegroundColor Green
    
    # train and publish luis app
    Write-Host "> Publishing and training app ..." -NoNewline
    $(luis train version `
        --appId $appId `
        --region $region `
        --authoringKey $authoringKey `
        --versionId $version `
        --wait 
    & luis publish version `
        --appId $appId `
        --region $region `
        --authoringKey $authoringKey `
        --versionId $version `
        --wait) 2>> $log | Out-Null
    Write-Host "Done." -ForegroundColor Green
}

function RunLuisGen($lu_file, $outName, $outFolder) {
    $id = $lu_file.BaseName
	$luisFolder = $lu_file.DirectoryName
	$luisFile = Join-Path $luisFolder "$($id).luis"

	bf luis:generate:cs `
        --in $luisFile `
        --className "$($outName)Luis" `
        --out $outFolder `
        --force 2>&1 | Out-Null
}