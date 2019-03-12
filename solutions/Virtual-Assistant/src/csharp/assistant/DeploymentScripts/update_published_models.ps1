#Requires -Version 6

param (
	[string] $locales = "de-de,en-us,es-es,fr-fr,it-it,zh-cn",
	[string] $serviceIds
)

$basePath = Join-Path $PSScriptRoot ".." "LocaleConfigurations"
$botFiles = get-childitem $basePath -recurse | where {$_.extension -eq ".bot"} 
$localeArr = $locales.split(',')

Write-Host $localeArr

if ($PSBoundParameters.ContainsKey('serviceIds')) {
	$serviceIdArr = $serviceIds.split(',')
}
else {
	$serviceIdArr = @()
}

function UpdateLUIS ($botFilePath, $langCode, $id) {
	$versions = msbot get $id --bot $botFilePath | luis list versions --stdin | ConvertFrom-Json

	if ($versions | where {$_.version -eq "backup"})
	{
		msbot get $id --bot $botFilePath | luis delete version --stdin --versionId backup --force --wait
	}
	
	msbot get $id --bot $botFilePath | luis rename version --newVersionId backup --stdin --wait
	msbot get $id --bot $botFilePath | luis import version --stdin --in "$(Join-Path $PSScriptRoot $langCode $id).luis" --wait
	msbot get $id --bot $botFilePath | luis train version --wait --stdin 
	msbot get $id --bot $botFilePath | luis publish version --stdin
}

function UpdateKB ($botFilePath, $langCode, $id) {
	Write-Host "Updating $($langCode) knowledge base $($id)..."

	msbot get $id --bot $botFilePath | qnamaker replace kb --in "$(Join-Path $PSScriptRoot $langCode $id).qna" --stdin
	msbot get $id --bot $botFilePath | qnamaker publish kb --stdin
}

function ImportLUIS ($botFileName, $botFilePath, $langCode, $id, $sampleService) {
	$luisService = luis import application --appName "$($botFileName)_$($id)" --authoringKey $sampleService.authoringKey --subscriptionKey $sampleService.authoringKey --region $sampleService.region --in "$(Join-Path $recipeBasePath $id).luis" --wait --msbot | ConvertFrom-Json
	Add-Member -InputObject $luisService -MemberType NoteProperty -Name id -Value $id -Force

	$botServices = Get-Content -Raw -Path $botFilePath | ConvertFrom-Json
	$botServices.services += $luisService
	$botFileJson = $botServices | ConvertTo-Json -Depth 10
	Set-Content -Path $botFilePath -Value $botFileJson

	msbot get $id --bot $botFilePath | luis train version --wait --stdin 
	msbot get $id --bot $botFilePath | luis publish version --stdin
}

function ImportKB ($botFilePath, $langCode, $id, $sampleService){
	Write-Host "Importing $($langCode) knowledge base $($id)..."
	$qnaService = qnamaker create kb --in "$(Join-Path $recipeBasePath $id).qna" --name $id --subscriptionKey $sampleService.subscriptionKey --msbot | ConvertFrom-Json
	Add-Member -InputObject $qnaService -MemberType NoteProperty -Name id -Value $id -Force

	$botServices = Get-Content -Raw -Path $botFilePath | ConvertFrom-Json
	$botServices.services += $qnaService
	$botFileJson = $botServices | ConvertTo-Json -Depth 10
	Set-Content -Path $botFilePath -Value $botFileJson

	msbot get $id --bot $botFilePath | qnamaker publish kb --stdin
}

foreach ($locale in $localeArr) {
	Invoke-Expression "$(Join-Path $PSScriptRoot generate_deployment_scripts.ps1) -locale $($locale)"
}

foreach ($botFile in $botFiles) {
	$botFileName = $botFile | % {$_.BaseName}
	$botFilePath = $botFile.FullName
	$langCode = $botFileName.Substring($botFileName.Length - 2, 2)
	$recipeBasePath = Join-Path $PSScriptRoot $langCode
	$recipePath = Join-Path $recipeBasePath "bot.recipe"

	# if locale of bot file is in the list
	if ($localeArr | where {$_ -like "*$($langCode)*"}) {
		
		# get the services from the bot file
		$botServices = Get-Content -Raw -Path $botFilePath | ConvertFrom-Json
		$recipeServices = Get-Content -Raw -Path $recipePath | ConvertFrom-Json

		# if there are any service ids supplied as parameters
		if ($serviceIdArr.Count -gt 0) {

			# foreach supplied service
			foreach ($serviceId in $serviceIdArr) {

				# get the service from .bot and .recipe
				$service = $botServices.services | where { $_.id -eq $serviceId }
				$recipeService = $recipeServices.resources | where { $_.id -eq $serviceId}

				# if service exists in .bot file
				if ($service) {
					# if LUIS or dispatch call UpdateLUIS, else call UpdateKB
					if (($service.type -eq "luis") -or ($service.type -eq "dispatch")) {
						UpdateLUIS $botFilePath $langCode $service.id

						if ($service.id -eq "dispatch") {
							luisgen "$(Join-Path $basePath .. DeploymentScripts $langCode dispatch.luis)" -cs Dispatch -o "$(Join-Path $basePath .. Dialogs Shared Resources)"
						}
						else {
							luisgen "$(Join-Path $basePath .. DeploymentScripts $langCode $service.id).luis" -cs "$($recipeService.Name)LU" -o "$(Join-Path $basePath .. $recipeService.luPath .. .. .. .. Dialogs Shared Resources)"
						}
					}
					elseif ($service.type -eq "qna") {
						UpdateKB $botFilePath $langCode $service.id
					}
				}
				elseif ($recipeService) {
					if ($recipeService.type -eq "luis") {
						$sampleService = $botServices.services | where { $_.type -eq "luis" } | Select-Object -First 1
						ImportLUIS $botFileName $botFilePath $langCode $service.id $sampleService
					}
					elseif ($recipeService.type -eq "dispatch") {
						$sampleService = $botServices.services | where { $_.type -eq "luis" } | Select-Object -First 1
						ImportLUIS $botFileName $botFilePath $langCode $service.id $sampleService
					}
					elseif($recipeService.type -eq "qna"){
						$sampleService = $botServices.services | where { $_.type -eq "qna" } | Select-Object -First 1
						ImportKB $botFilePath $langCode $service.id $sampleService
					}
				}
			}
		}

		# if no service ids were supplied, update everything
		else {
			foreach ($recipeService in $recipeServices.resources) {

				# if service exists in bot file
				$service = $botServices.services | where { $_.id -eq $recipeService.id }

				if ($service) {
					if (($service.type -eq "luis") -or ($service.type -eq "dispatch")) {
						UpdateLUIS $botFilePath $langCode $service.id

						if ($service.id -eq "dispatch") {
							luisgen "$(Join-Path $basePath .. DeploymentScripts $langCode dispatch.luis)" -cs Dispatch -o "$(Join-Path $basePath .. Dialogs Shared Resources)"
						}
						else {
							luisgen "$(Join-Path $basePath .. DeploymentScripts $langCode $service.id).luis" -cs "$($recipeService.Name)LU" -o "$(Join-Path $basePath .. $recipeService.luPath .. .. .. .. Dialogs Shared Resources)"
						}
					}
					elseif ($service.type -eq "qna") {
						UpdateKB $botFilePath $langCode $service.id
					}
				}
				else {
					if ($recipeService.type -eq "luis") {
						$sampleService = $botServices.services | where { $_.type -eq "luis" } | Select-Object -First 1
						ImportLUIS $botFileName $botFilePath $langCode $recipeService.id $sampleService

						if ($service.id -eq "dispatch") {
							luisgen "$(Join-Path $basePath .. DeploymentScripts $langCode dispatch.luis)" -cs Dispatch -o "$(Join-Path $basePath .. Dialogs Shared Resources)"
						}
						else {
							luisgen "$(Join-Path $basePath .. DeploymentScripts $langCode $service.id).luis" -cs "$($recipeService.Name)LU" -o "$(Join-Path $basePath .. $recipeService.luPath .. .. .. .. Dialogs Shared Resources)"
						}
					}
					elseif ($recipeService.type -eq "qna") {
						$sampleService = $botServices.services | where { $_.type -eq "qna" } | Select-Object -First 1
						ImportKB $botFilePath $langCode $recipeService.id $sampleService
					}
				}
			}
		}
	}
}