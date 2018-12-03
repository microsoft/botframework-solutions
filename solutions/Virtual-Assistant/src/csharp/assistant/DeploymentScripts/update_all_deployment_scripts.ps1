$locales = @("de-de", "en-us", "es-es", "fr-fr", "it-it", "zh-cn")

foreach ($locale in $locales) {
	Invoke-Expression "$($PSScriptRoot)\update_locale_deployment_script.ps1 -locale $($locale)"
}