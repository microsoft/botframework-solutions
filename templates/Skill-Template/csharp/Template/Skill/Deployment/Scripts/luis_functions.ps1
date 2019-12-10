function DeployLUIS ($name, $lu_file, $region, $authoringKey, $language, $gov, $log)
{
    $id = $lu_file.BaseName
    $outFile = Join-Path $lu_file.DirectoryName "$($id).json"
    $appName = "$($name)$($langCode)_$($id)"
    
    if ($gov)
    {
        $cloud = 'us'
    }
    else 
    {
        $cloud = 'com'
    }
    
    Write-Host "> Parsing $($id) LU file ..." -NoNewline
	bf luis:convert `
        --name $appName `
        --in $lu_file `
        --culture $language `
        --out $outFile `
        --force 2>> $log | Out-Null
    Write-Host "Done." -ForegroundColor Green
		
    Write-Host "> Deploying $($id) LUIS app ..." -NoNewline
    $luisApp = (luis import application `
        --appName $appName `
        --authoringKey $authoringKey `
        --subscriptionKey $authoringKey `
        --region $region `
        --in $outFile `
        --cloud $cloud `
        --wait) 2>> $log | ConvertFrom-Json

	if (-not $luisApp)
    {
		Write-Host "! Could not deploy LUIS model. Review the log for more information." -ForegroundColor DarkRed
		Write-Host "! Log: $($log)" -ForegroundColor DarkRed
		Return $null
	}
	else
    {
        Write-Host "Done." -ForegroundColor Green
        Write-Host "> Training and publishing LUIS app ..." -NoNewline
		$(luis train version `
            --appId $luisApp.id `
            --region $region `
            --authoringKey $authoringKey `
            --versionId $luisApp.activeVersion `
            --cloud $cloud `
            --wait
        & luis publish version `
            --appId $luisApp.id `
            --region $region `
            --authoringKey $authoringKey `
            --versionId $luisApp.activeVersion `
            --cloud $cloud `
            --wait) 2>> $log | Out-Null
        Write-Host "Done." -ForegroundColor Green

		Return $luisApp
	}
}

function UpdateLUIS ($lu_file, $appId, $version, $region, $authoringKey, $subscriptionKey, $gov, $log)
{
    $id = $lu_file.BaseName
    $outFile = Join-Path $lu_file.DirectoryName "$($id).json"
    
    if ($gov)
    {
        $cloud = 'us'
    }
    else 
    {
        $cloud = 'com'
    }

    Write-Host "> Getting hosted $($id) LUIS model settings..." -NoNewline
    $luisApp = (luis get application `
        --appId $appId `
        --region $region `
        --authoringKey $authoringKey `
        --cloud $cloud) 2>> $log | ConvertFrom-Json
    Write-Host "Done." -ForegroundColor Green
     
    Write-Host "> Getting current versions ..." -NoNewline
	$versions = (luis list versions `
        --appId $appId `
        --region $region `
        --authoringKey $authoringKey `
        --cloud $cloud) 2>> $log | ConvertFrom-Json
    Write-Host "Done." -ForegroundColor Green

    if ($versions | Where { $_.version -eq $version })
    {
        if ($versions | Where { $_.version -eq "backup" })
        {
            Write-Host "> Deleting old backup version ..." -NoNewline
            luis delete version `
                --appId $appId `
                --versionId backup `
                --region $region `
                --authoringKey $authoringKey `
                --cloud $cloud `
                --force `
                --wait 2>> $log | Out-Null
            Write-Host "Done." -ForegroundColor Green
        }
        
        Write-Host "> Saving current version as backup ..." -NoNewline
	    luis rename version `
            --appId $appId `
            --versionId $version `
            --region $region `
            --newVersionId backup `
            --authoringKey $authoringKey `
            --subscriptionKey $subscriptionKey `
            --cloud $cloud `
            --wait 2>> $log | Out-Null
        Write-Host "Done." -ForegroundColor Green
    }   
    
    Write-Host "> Parsing $($id) LU file ..." -NoNewline
	bf luis:convert `
        --name $luisApp.name `
        --in $lu_file `
        --culture $luisApp.culture `
        --out $outFile `
        --force 2>> $log | Out-Null
    Write-Host "Done." -ForegroundColor Green

    Write-Host "> Importing new version ..." -NoNewline
    luis import version `
        --appId $appId `
        --versionId $version `
        --region $region `
        --authoringKey $authoringKey `
        --subscriptionKey $subscriptionKey `
        --in $outFile `
        --cloud $cloud `
        --wait 2>> $log | Out-Null
    Write-Host "Done." -ForegroundColor Green

    Write-Host "> Training and publishing LUIS app ..." -NoNewline
	$(luis train version `
        --appId $luisApp.id `
        --region $region `
        --authoringKey $authoringKey `
        --versionId $luisApp.activeVersion `
        --cloud $cloud `
        --wait
    & luis publish version `
        --appId $luisApp.id `
        --region $region `
        --authoringKey $authoringKey `
        --versionId $luisApp.activeVersion `
        --cloud $cloud `
        --wait) 2>> $log | Out-Null
    Write-Host "Done." -ForegroundColor Green
}

function RunLuisGen($lu_file, $outName, $outFolder, $log)
{
    $id = $lu_file.BaseName
	$luisFolder = $lu_file.DirectoryName
	$luisFile = Join-Path $luisFolder "$($id).json"

	bf luis:generate:cs `
        --in $luisFile `
        --className "$($outName)Luis" `
        --out $outFolder `
        --force 2>> $log | Out-Null
}