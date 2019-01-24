param (
	[string] $locales = "de-de,en-us,es-es,fr-fr,it-it,zh-cn",
	[string] $serviceIds
)

$basePath = "$($PSScriptRoot)\..\LocaleConfigurations\"
$botFiles = get-childitem $basePath -recurse | where {$_.extension -eq ".bot"} 
$localeArr = $locales.Split(",")

if ($PSBoundParameters.ContainsKey('serviceIds')) {
	$serviceIdArr = $serviceIds.Split(",")
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
	msbot get $id --bot $botFilePath | luis import version --stdin --in "$($PSScriptRoot)\$($langCode)\$($id).luis" --wait
	msbot get $id --bot $botFilePath | luis train version --wait --stdin 
	msbot get $id --bot $botFilePath | luis publish version --stdin
}

function UpdateQnA ($botFilePath, $langCode, $id) {
	msbot get $id --bot $botFilePath | qnamaker replace kb --in "$($PSScriptRoot)\$($langCode)\$($id).qna" --stdin
	msbot get $id --bot $botFilePath | qnamaker publish kb --stdin
}

foreach ($locale in $localeArr) {
	Invoke-Expression "$($PSScriptRoot)\generate_deployment_scripts.ps1 -locale $($locale)"
}

foreach ($botFile in $botFiles) {
	$botFileName = $botFile | % {$_.BaseName}
	$botFilePath = "$($basePath)$($botFile)"
	$langCode = $botFileName.Substring($botFileName.Length - 2, 2)

	if ($localeArr | where {$_ -like "*$($langCode)*"}) {
		$botServices = Get-Content -Raw -Path $botFilePath | ConvertFrom-Json

		if ($serviceIdArr.Count -gt 0) {
			foreach ($serviceId in $serviceIdArr) {
				$service = $botServices.services | where { $_.id -eq $serviceId }

				if (($service.type -eq "luis") -or ($service.type -eq "dispatch")) {
					UpdateLUIS $botFilePath $langCode $service.id
				}
				elseif ($service.type -eq "faq") {
					UpdateQnA $botFilePath $langCode $service.id
				}
			}
		}
		else {
			foreach ($service in $botServices.services) {
				if (($service.type -eq "luis") -or ($service.type -eq "dispatch")) {
					UpdateLUIS $botFilePath $langCode $service.id
				}
				elseif ($service.type -eq "faq") {
					UpdateQnA $botFilePath $langCode $service.id
				}
			}
		}
	}
}