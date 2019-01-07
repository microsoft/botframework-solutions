param (
    [string] [Parameter(Mandatory=$true)]$locale
)

$locale = $locale.ToLower()
$langCode = ($locale -split "-")[0]
$basePath = "$($PSScriptRoot)\.."
$outputPath = "$($PSScriptRoot)\$($langCode)"

# lu file paths
$skillLUPath = "$($basePath)\CognitiveModels\LUIS\$($langCode)\skill.lu"
$generalLUPath = "$($basePath)\CognitiveModels\LUIS\$($langCode)\general.lu"

$luArr = @($skillLUPath, $generalLUPath)

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
ludown parse toluis -c $($locale) -o $outputPath --in $skillLUPath --out skill.luis -n Skill
ludown parse toluis -c $($locale) -o $outputPath --in $generalLUPath --out general.luis -n General