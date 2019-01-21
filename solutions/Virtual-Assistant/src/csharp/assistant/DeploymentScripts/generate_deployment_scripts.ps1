param (
    [string] [Parameter(Mandatory=$true)]$locale
)

function CheckForDuplicates($lu) {
	$duplicates = Get-Content $lu | Group-Object | Where-Object { $_.Count -gt 1 } | Select -ExpandProperty Name

	if ($duplicates.Count -gt 1) 
	{
		Write-Warning "$($duplicates.Count - 1) duplicate utterances found in $($lu). This could cause issues in your model accuracy."
	}
}

$locale = $locale.ToLower()
$langCode = ($locale -split "-")[0]
$basePath = "$($PSScriptRoot)\.."
$outputPath = "$($basePath)\DeploymentScripts\$($langCode)"
$recipePath = "$($basePath)\DeploymentScripts\$($langCode)\bot.recipe"
$recipe = Get-Content -Raw -Path $recipePath | ConvertFrom-Json

foreach ($service in $recipe.resources)
{
	Write-Host "Generating $($locale) $($service.name) script ..."
	$path = "$($basePath)\$($service.luPath)"

	if ($service.type -eq "luis")
	{
		CheckForDuplicates $path
		ludown parse toluis -c $($locale) -o $outputPath --in $path --out "$($service.id).luis" -n $service.Name
	}
	elseif ($service.type -eq "qna")
	{
		CheckForDuplicates $path
		ludown parse toqna -o $outputPath --in $path --out "$($service.id).qna"
	}
	elseif ($service.type -eq "dispatch")
	{
		CheckForDuplicates $path
		ludown parse toluis -c $($locale) -o $outputPath --in $path --out "$($service.id).luis" -n $service.Name
	}
}