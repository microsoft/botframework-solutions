function DeployKB ($name, $lu_file, $qnaSubscriptionKey)
{
    $id = $lu_file.BaseName
    $outFile = "$($id).qna"
    $outFolder = $lu_file.DirectoryName

    # Parse LU file
    Write-Host "Parsing $($id) LU file ..."
    ludown parse toqna `
        --in $lu_file `
        --out_folder $outFolder `
        --out $outFile
        
    # Create QnA Maker kb
    Write-Host "Deploying $($id) QnA kb ..."
    $qnaKb = qnamaker create kb `
        --name $id `
        --subscriptionKey $qnaSubscriptionKey `
        --in $(Join-Path $outFolder $outFile) `
        --force `
        --wait `
        --msbot | ConvertFrom-Json

    # Publish QnA Maker knowledgebase
    $(qnamaker publish kb --kbId $qnaKb.kbId --subscriptionKey $qnaSubscriptionKey) 2>&1 | Out-Null

    Return $qnaKb
}

function UpdateKB ($lu_file, $kbId, $qnaSubscriptionKey)
{
    $id = $lu_file.BaseName
    $outFile = "$($id).qna"
    $outFolder = $lu_file.DirectoryName

    # Parse LU file
    Write-Host "Parsing $($id) LU file ..."
    ludown parse toqna `
        --in $lu_file `
        --out_folder $outFolder `
        --out $outFile

    Write-Host "Replacing $($id) QnA kb ..."
	qnamaker replace kb `
        --in $(Join-Path $outFolder $outFile) `
        --kbId $kbId `
        --subscriptionKey $qnaSubscriptionKey

    # Publish QnA Maker knowledgebase
	$(qnamaker publish kb --kbId $kbId --subscriptionKey $qnaSubscriptionKey) 2>&1 | Out-Null
}