#Requires -Version 6

Param(
    [string] $manifestUrl,
	[string] $luisFolder,
    [string] $dispatchFolder,
	[string] $dispatchName,
	[string] $language = "en-us",
    [string] $outFolder = $(Get-Location),
    [string] $lgOutFolder = $(Join-Path $outFolder Services),
	[string] $skillsFile = $(Join-Path $outFolder 'skills.json'),
	[string] $cognitiveModelsFile = $(Join-Path $outFolder 'cognitivemodels.json'),
	[string] $logFile = $(Join-Path $PSScriptRoot .. "remove_skill_log.txt")
)

. $PSScriptRoot\skill_functions.ps1

# Reset log file
if (Test-Path $logFile) {
	Clear-Content $logFile -Force | Out-Null
}
else {
	New-Item -Path $logFile | Out-Null
}

# Set defaults and validate file paths
$langCode = ($language -split "-")[0]
if (-not $luisFolder) {
    $luisFolder = $(Join-Path $PSScriptRoot .. Resources Skills $langCode)
}

if (-not $manifestUrl) {
	$manifestUrl = Read-Host "? Skill Manifest URL (i.e. https://calendarskill.azurewebsites.net/api/skill/manifest)"
}

if (-not $dispatchFolder) {
    $dispatchFolder = $(Join-Path $PSScriptRoot .. Resources Dispatch $langCode)
}

if (-not $dispatchName) {
	if (Test-Path $cognitiveModelsFile) {
		$cognitiveModels = Get-Content $cognitiveModelsFile | ConvertFrom-Json
		$models = $($cognitiveModels.cognitiveModels.PSObject.Properties | Where-Object { $_.Name -eq $langCode } | Select-Object -First 1).Value
		$dispatchName = $models.dispatchModel.name
	}
	else {
		Write-Host "! Could not find file: $($cognitiveModelsFile). Please provide a valid path, or the dispatchName and dispatchFolder parameters." -ForegroundColor DarkRed
		Break
	}
}

$dispatchPath = $(Join-Path $dispatchFolder "$($dispatchName).dispatch")
if (-not $(Test-Path $dispatchPath)) {
	Write-Host "! Could not find file: $($dispatchPath). Please provide the dispatchName and dispatchFolder parameters." -ForegroundColor DarkRed
	Break
}

$dispatchJsonPath = $(Join-Path $dispatchFolder "$($dispatchName).json")
if (-not $(Test-Path $dispatchJsonPath)) {
	Write-Host "! Could not find file: $($dispatchPath). LuisGen will not be run." -ForegroundColor DarkRed
}

# Processing
Write-Host "> Loading skill manifest ..."
$manifest = $(Invoke-WebRequest -Uri $manifestUrl | ConvertFrom-Json) 2>> $logFile

if (-not $manifest) {
	Write-Host "! Could not load manifest from $($manifestUrl). Please check the url and try again." -ForegroundColor DarkRed
	Break
}

Write-Host "> Removing skill from dispatch ..." 
$dispatch = Get-Content $dispatchPath | ConvertFrom-Json
if ($dispatch.services) {
	$toRemove = $dispatch.services | Where-Object { $manifest.id -eq $_.name }
	$dispatch.services = $dispatch.services | Where-Object -FilterScript { $manifest.id -ne $_.name }
	$dispatch.serviceIds = $dispatch.serviceIds | Where-Object -FilterScript { $toRemove.id -notcontains $_ }

	$dispatch | ConvertTo-Json -depth 100 | Out-File $dispatchPath
}
else {
	Write-Host "! No services found in file: $($dispatchPath)" -ForegroundColor DarkRed
}

Write-Host "> Running dispatch refresh ..."
(dispatch refresh `
	--dispatch $(Join-Path $dispatchFolder "$($dispatchName).dispatch") `
	--dataFolder $dispatchFolder) 2>> $logFile | Out-Null

if (Test-Path $dispatchJsonPath) {
	Write-Host "> Running LuisGen ..." 
	luisgen $dispatchJsonPath -cs "DispatchLuis" -o $lgOutFolder 2>> $logFile | Out-Null
}

Write-Host "> Getting skill config ..."
if (Test-Path $skillsFile) {
    $skillConfig = Get-Content $skillsFile | ConvertFrom-Json

    if ($skillConfig) {
        if ($skillConfig.skills) {
            if ($skillConfig.skills.Id -eq $manifest.Id) {
				Write-Host "> Removing $($manifest.Id) from skill config ..."
				$skillConfig.skills = @($skillConfig.skills | Where-Object -FilterScript { $_.Id -ne $manifest.Id})
				$skillConfig | ConvertTo-Json -depth 100 | Out-File $skillsFile
            }
            else {
				Write-Host "! Could not find $($manifest.Id) in skill config." -ForegroundColor Cyan
            }
        }
        else {
			Write-Host "! Could not find $($manifest.Id) in skill config." -ForegroundColor Cyan
        }
    }
}
else {
	Write-Host "! Could not find file: $($skillFile)" -ForegroundColor Cyan
}

Write-Host "> Done."