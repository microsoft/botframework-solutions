param (
    [string] [Parameter(Mandatory=$true)]$locale
)

$langCode = ($locale -split "-")[0]
$basePath = "$($PSScriptRoot)\.."
$outputPath = "$($PSScriptRoot)\$($langCode)"

# lu file paths
$emailLUPath = "$($basePath)\CognitiveModels\LUIS\$($langCode)\email.lu"
$generalLUPath = "$($basePath)\..\..\assistant\CognitiveModels\LUIS\$($langCode)\general.lu"

$luArr = @($emailLUPath, $generalLUPath)
$hasDuplicates = 0

Write-Host "Updating $($locale) deployment scripts ..."

foreach ($lu in $luArr) 
{
	$duplicates = Get-Content $lu | Group-Object | Where-Object { $_.Count -gt 1 } | Select -ExpandProperty Name

	if ($duplicates.Count -gt 1) 
	{
		Write-Host "$($duplicates.Count - 1) duplicate utterances found in $($lu):"
		Write-Host $duplicates 
		$hasDuplicates = 1
	}
}

if ($hasDuplicates -eq 0) 
{
	Write-Host "Generating $($locale) LUIS models from .lu files ..."
	ludown parse toluis -c $($locale) -o $outputPath --in $emailLUPath --out email.luis -n Email
	ludown parse toluis -c $($locale) -o $outputPath --in $generalLUPath --out general.luis -n General
}