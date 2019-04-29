#Requires -Version 6

Param(
    [Parameter(Mandatory = $true)][string] $config_file,
    [Parameter(Mandatory = $true)][string] $skill_manifest,
    [Parameter(Mandatory = $true)][string] $skill_luis,
    [Parameter(Mandatory = $true)][string] $dispatch,
    [Parameter(Mandatory = $true)][string] $dataFolder,
    [string] $intentName,
    [string] $outFolder = $PSScriptRoot
)

Write-Host "Loading skill manifest ..."
$manifest = Get-Content $skill_manifest | ConvertFrom-Json

# set intent name
if (-not $intentName) {
    $intentName = $manifest.DispatchIntent
}

Write-Host "Initializing skill.config ..."
# add skill manifest to config_file
if (Test-Path $config_file) {
    $skill_config = Get-Content $config_file | ConvertFrom-Json

    if ($skill_config) {
        if ($skill_config.skills) {
            if ($skill_config.skills.Name -eq $manifest.Name) {
                Write-Host "$($manifest.Name) skill already registered."
            }
            else {
                Write-Host "Registering $($manifest.Name) skill ..."
                $skill_config.skills += $manifest
            }
        }
        else {
            Write-Host "Registering $($manifest.Name) skill ..."    
            $skills = @($manifest)
            $skill_config | Add-Member -Type NoteProperty -Force -Name "skills" -Value $skills
        }
    }
}

if (-not $skill_config) {
    $skill_config = @{ skills = @($manifest) }
}

# write out the updated config to the file
$skill_config | ConvertTo-Json -depth 100 | Out-File $config_file

# add file to dispatch 
Write-Host "Adding skill to Dispatch ..."
dispatch add `
    --type file `
    --filePath $skill_luis `
    --intentName $intentName `
    --dataFolder $dataFolder `
    --dispatch $(Join-Path $dataFolder "$($dispatch).dispatch") | Out-Null

# run dispatch refresh
Write-Host "Running dispatch refresh ..."
Write-Host $(Join-Path $dataFolder $dispatch)
dispatch refresh --dispatch $(Join-Path $dataFolder "$($dispatch).dispatch") --dataFolder $dataFolder

# update dispatch.cs
Write-Host "Running LuisGen ..."
luisgen $(Join-Path $dataFolder "$($dispatch).json") -cs "Dispatch" -o $outFolder