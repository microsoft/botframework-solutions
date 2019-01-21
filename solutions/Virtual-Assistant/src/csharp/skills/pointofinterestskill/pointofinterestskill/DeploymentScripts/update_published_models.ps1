param (
    [string] $locales = "de-de,en-us,es-es,fr-fr,it-it,zh-cn",
	[string] $domains = "general,pointofinterest"
)

$localeArr = $locales.Split(",")
$domainArr = $domains.Split(",")
$basePath = "$($PSScriptRoot)\..\LocaleConfigurations\"
$botFiles = get-childitem $basePath -recurse | where {$_.extension -eq ".bot"}

Write-Host "Updating deployment scripts..."
foreach ($locale in $localeArr) 
{
	Invoke-Expression "$($PSScriptRoot)\generate_deployment_scripts.ps1 -locale $($locale)"
}

foreach ($botFile in $botFiles)
{
 	$botFileName = $botFile | % {$_.BaseName}
	$langCode = $botFileName.Substring($botFileName.Length - 2, 2)
		
	if ($localeArr | where {$_ -like "*$($langCode)*"})
	{	
		Write-Host "Updating $($langCode) LUIS models ..."
		foreach ($domain in $domainArr)
		{
			# Check for existing old version
			$versions = msbot get $domain --bot "$($basePath)$($botFile)" | luis list versions --stdin | ConvertFrom-Json
			if ($versions | where {$_.version -eq "backup"})
			{
				msbot get $domain --bot "$($basePath)$($botFile)" | luis delete version --stdin --versionId backup --force --wait
			}
		
			msbot get $domain --bot "$($basePath)$($botFile)" | luis rename version --newVersionId backup --stdin --wait
			msbot get $domain --bot "$($basePath)$($botFile)" | luis import version --stdin --in "$($PSScriptRoot)\$($langCode)\$($domain).luis" --wait
			msbot get $domain --bot "$($basePath)$($botFile)" | luis train version --wait --stdin 
			msbot get $domain --bot "$($basePath)$($botFile)" | luis publish version --stdin
		}
	}
}