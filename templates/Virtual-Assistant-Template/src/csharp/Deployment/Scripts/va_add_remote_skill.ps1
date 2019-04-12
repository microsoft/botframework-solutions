Param(
    [Parameter(Mandatory = $true)][string] $configFile,
    [Parameter(Mandatory = $true)][string] $manifestUrl,
    [Parameter(Mandatory = $true)][string] $dispatchName,
    [string] $language = "en-us",
    [string] $luisFolder,
    [string] $dispatchFolder,
    [string] $outFolder = $(Get-Location),
    [string] $lgOutFolder = $(Join-Path $outFolder Services)
)

# Set folder defaults
$langCode = ($language -split "-")[0]
if (-not $luisFolder) {
    $luisFolder = $(Join-Path $PSScriptRoot .. Resources Skills $langCode)
}
if (-not $dispatchFolder) {
    $dispatchFolder = $(Join-Path $PSScriptRoot .. Resources Dispatch $langCode)
}

Write-Host "Loading skill manifest ..."
$manifest = Invoke-WebRequest -Uri $manifestUrl | ConvertFrom-Json

Write-Host "Initializing skill.config ..."
if (Test-Path $configFile) {
    $skillConfig = Get-Content $configFile | ConvertFrom-Json

    if ($skillConfig) {
        if ($skillConfig.skills) {
            if ($skillConfig.skills.Id -eq $manifest.Id) {
                Write-Host "$($manifest.Id) is already registered."
                Break
            }
            else {
                Write-Host "Registering $($manifest.Id) ..."
                $skillConfig.skills += $manifest
            }
        }
        else {
            Write-Host "Registering $($manifest.Id) ..."    
            $skills = @($manifest)
            $skillConfig | Add-Member -Type NoteProperty -Force -Name "skills" -Value $skills
        }
    }
}

if (-not $skillConfig) {
    $skillConfig = @{ skills = @($manifest) }
}

$skillConfig | ConvertTo-Json -depth 100 | Out-File $configFile

Write-Host "Getting intents for dispatch ..."
$dictionary = @{ }
foreach ($action in $manifest.actions) {
    if ($action.definition.triggers.utteranceSources) {
        foreach ($source in $action.definition.triggers.utteranceSources) {
            foreach ($luisStr in $source.source) {
                $luis = $luisStr -Split '#'                
                if ($dictionary.ContainsKey($luis[0])) {
                    $intents = $dictionary[$luis[0]]
                    $intents += $luis[1]
                    $dictionary[$luis[0]] = $intents
                }
                else {
                    $dictionary.Add($luis[0], @($luis[1]))
                }
            }
        }
    }
}

Write-Host "Adding skill to Dispatch ..."
$intentName = $manifest.Id
foreach ($luisApp in $dictionary.Keys) {
    $intents = $dictionary[$luisApp]
    $luFile = Get-ChildItem -Path $(Join-Path $luisFolder "$($luisApp).lu") `

    # Parse LU file
    Write-Host "Parsing $($id) LU file ..."
    ludown parse toluis `
        --in $luFile `
        --luis_culture $language `
        --out_folder $luisFolder `
        --out "$($luisApp).luis"

    $luisFile = Get-ChildItem `
        -Path $luisFolder `
        -Filter "$($luisApp).luis" `
        -ErrorAction SilentlyContinue `
        -Recurse `
        -Force

    dispatch add `
        --type file `
        --filePath $luisFile `
        --intentName $intentName `
        --dataFolder $dispatchFolder `
        --dispatch $(Join-Path $dispatchFolder "$($dispatchName).dispatch")
}

Write-Host "Running dispatch refresh ..."
dispatch refresh --dispatch $(Join-Path $dispatchFolder "$($dispatchName).dispatch") --dataFolder $dispatchFolder

Write-Host "Running LuisGen ..."
luisgen $(Join-Path $dispatchFolder "$($dispatchName).json") -cs "DispatchLuis" -o $lgOutFolder