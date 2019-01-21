param (
    [string] [Parameter(Mandatory=$true)]$locale
)

$locale = $locale.ToLower()
$langCode = ($locale -split "-")[0]
$basePath = "$($PSScriptRoot)\.."
$outputPath = "$($PSScriptRoot)\$($langCode)"

# lu file paths
$poiLUPath = "$($basePath)\CognitiveModels\LUIS\$($langCode)\pointofinterest.lu"
$generalLUPath = "$($basePath)\..\..\assistant\CognitiveModels\LUIS\$($langCode)\general.lu"

$luArr = @($poiLUPath, $generalLUPath)

Write-Host "Updating $($locale) deployment scripts ..."
foreach ($lu in $luArr) 
{
	$duplicates = Get-Content $lu | Group-Object | Where-Object { $_.Count -gt 1 } | Select -ExpandProperty Name

	if ($duplicates.Count -gt 1) 
	{
		Write-Error "$($duplicates.Count - 1) duplicate utterances found in $($lu). This could cause issues in your model accuracy."
	}
}

Write-Host "Generating $($locale) LUIS models from .lu files ..."
ludown parse toluis -c $($locale) -o $outputPath --in $poiLUPath --out pointofinterest.luis -n PointOfInterest
ludown parse toluis -c $($locale) -o $outputPath --in $generalLUPath --out general.luis -n General