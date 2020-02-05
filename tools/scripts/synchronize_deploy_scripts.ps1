#Requires -Version 6

$target = Join-Path "Deployment" "Scripts"
$projectRoot = Join-Path $PSScriptRoot ".." ".."

function GetSrc
{
    return Join-Path $projectRoot @args $target "*"
}

function AddPath
{
    param($dsts)
    $dsts.Add($(Join-Path $projectRoot @args $target)) > $null
}

function Synchronize
{
    param($src, $dsts)

    Write-Host "Copy from $src"
    foreach ($dst in $dsts)
    {
        Write-Host "Copy to $dst"
        Copy-Item "$src" -Destination "$dst"
    }    
}

# synchronize VAs

$src = GetSrc "samples" "csharp" "assistants" "virtual-assistant" "VirtualAssistantSample"
$dsts = [System.Collections.ArrayList]@()
AddPath $dsts "templates" "csharp" "VA" "VA"
AddPath $dsts "samples" "csharp" "assistants" "enterprise-assistant" "VirtualAssistantSample"
AddPath $dsts "samples" "csharp" "assistants" "hospitality-assistant" "VirtualAssistantSample"
Synchronize $src $dsts

# synchronize skills

$src = GetSrc "samples" "csharp" "skill" "SkillSample"
$dsts = [System.Collections.ArrayList]@()
AddPath $dsts "templates" "csharp" "Skill" "Skill"
$skills = @("calendarskill", "emailskill", "pointofinterestskill", "todoskill")
foreach ($skill in $skills)
{
    AddPath $dsts "skills" "csharp" $skill
}
$expSkills = @("automotiveskill", "bingsearchskill", "eventskill", "hospitalityskill", "itsmskill", "musicskill", "newsskill", "phoneskill", "restaurantbookingskill", "weatherskill")
foreach ($skill in $expSkills)
{
    AddPath $dsts "skills" "csharp" "experimental" $skill
}
Synchronize $src $dsts
