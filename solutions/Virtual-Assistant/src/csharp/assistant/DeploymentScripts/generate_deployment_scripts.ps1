﻿param (
    [string] [Parameter(Mandatory=$true)]$locale
)

$locale = $locale.ToLower()
$langCode = ($locale -split "-")[0]
$basePath = "$($PSScriptRoot)\.."
$outputPath = "$($PSScriptRoot)\..\DeploymentScripts\$($langCode)"

# lu file paths
$calendarLUPath = "$($basePath)\..\skills\calendarskill\CognitiveModels\LUIS\$($langCode)\calendar.lu"
$emailLUPath = "$($basePath)\..\skills\emailskill\CognitiveModels\LUIS\$($langCode)\email.lu"
$todoLUPath = "$($basePath)\..\skills\todoskill\CognitiveModels\LUIS\$($langCode)\todo.lu"
$poiLUPath = "$($basePath)\..\skills\pointofinterestskill\CognitiveModels\LUIS\$($langCode)\pointofinterest.lu"
$generalLUPath = "$($basePath)\CognitiveModels\LUIS\$($langCode)\general.lu"
$faqLUPath = "$($basePath)\CognitiveModels\QnA\$($langCode)\faq.lu"
$dispatchLUPath = "$($basePath)\CognitiveModels\LUIS\$($langCode)\dispatch.lu"

$luArr = @($calendarLUPath, $emailLUPath, $todoLUPath, $poiLUPath,$generalLUPath, $dispatchLUPath)

Write-Host "Updating $($locale) deployment scripts ..."
foreach ($lu in $luArr) 
{
	$duplicates = Get-Content $lu | Group-Object | Where-Object { $_.Count -gt 1 } | Select -ExpandProperty Name

	if ($duplicates.Count -gt 1) 
	{
		Write-Error "$($duplicates.Count - 1) duplicate utterances found in $($lu). This could cause issues in your model accuracy."
	}
}

Write-Host "Generating $($locale) LUIS and QnA Maker models from .lu files ..."
ludown parse toqna  -o $outputPath --in $faqLUPath -n faq.qna 
ludown parse toluis -c $($locale) -o $outputPath --in $calendarLUPath --out calendar.luis -n Calendar
ludown parse toluis -c $($locale) -o $outputPath --in $emailLUPath --out email.luis -n Email
ludown parse toluis -c $($locale) -o $outputPath --in $todoLUPath --out todo.luis -n ToDo
ludown parse toluis -c $($locale) -o $outputPath --in $poiLUPath --out pointofinterest.luis -n PointOfInterest
ludown parse toluis -c $($locale) -o $outputPath --in $generalLUPath --out general.luis -n General
ludown parse toluis -c $($locale) -o $outputPath --in $dispatchLUPath --out dispatch.luis -n Dispatch -i Dispatch