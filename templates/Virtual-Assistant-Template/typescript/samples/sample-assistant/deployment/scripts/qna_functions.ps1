function DeployKB ($name, $lu_file, $qnaSubscriptionKey, $log)
{
    $id = $lu_file.BaseName
    $outFile = "$($id).qna"
    $outFolder = $lu_file.DirectoryName

    # Parse LU file
    Write-Host "> Parsing $($id) LU file ..."
    ludown parse toqna `
        --in $lu_file `
        --out_folder $outFolder `
        --out $outFile
        
	# Create QnA Maker kb
    Write-Host "> Deploying $($id) QnA kb ..."

	# These values pretty much guarantee success. We can decrease them if the QnA backend gets faster
    $initialDelaySeconds = 30;
    $retryAttemptsRemaining = 3;
    $retryDelaySeconds = 15;
    $retryDelayIncrease = 30;

    while ($retryAttemptsRemaining -ge 0) {
		$qnaKb = (qnamaker create kb `
			--name $id `
			--subscriptionKey $qnaSubscriptionKey `
			--in $(Join-Path $outFolder $outFile) `
			--force `
			--wait `
			--msbot) 2>> $log

		if (-not $qnaKb) {
			$retryAttemptsRemaining = $retryAttemptsRemaining - 1
			Write-Host $retryAttemptsRemaining
			Start-Sleep -s $retryDelaySeconds
			$retryDelaySeconds += $retryDelayIncrease

			if ($retryAttemptsRemaining -lt 0) {
				Write-Host "! Unable to create QnA KB." -ForegroundColor Cyan
			}
			else {
				Write-Host "> Retrying ..."
				Continue
			}
		}
		else {
			Break
		}
    }

	if (-not $qnaKb) {
		Write-Host "! Could not deploy knowledgebase. Review the log for more information." -ForegroundColor DarkRed
		Write-Host "! Log: $($log)" -ForegroundColor DarkRed
		Return $null
	}
	else {
		$qnaKb = $qnaKb | ConvertFrom-Json

	    # Publish QnA Maker knowledgebase
		$(qnamaker publish kb --kbId $qnaKb.kbId --subscriptionKey $qnaSubscriptionKey) 2>> $log | Out-Null

		Return $qnaKb
	}
}

function UpdateKB ($lu_file, $kbId, $qnaSubscriptionKey)
{
    $id = $lu_file.BaseName
    $outFile = "$($id).qna"
    $outFolder = $lu_file.DirectoryName

    # Parse LU file
    Write-Host "> Parsing $($id) LU file ..."
    ludown parse toqna `
        --in $lu_file `
        --out_folder $outFolder `
        --out $outFile

    Write-Host "> Replacing $($id) QnA kb ..."
	qnamaker replace kb `
        --in $(Join-Path $outFolder $outFile) `
        --kbId $kbId `
        --subscriptionKey $qnaSubscriptionKey

    # Publish QnA Maker knowledgebase
	$(qnamaker publish kb --kbId $kbId --subscriptionKey $qnaSubscriptionKey) 2>&1 | Out-Null
}
