Param(
    [string] $jsonFile,
    [int] $oauthConnections = 1
)
$config = Get-Content -Raw -Path $jsonFile | ConvertFrom-Json

$oauthConnection = [pscustomobject]@{
    name = ""
    provider = ""
}
# Add language models
for($i = 0; $i -lt $oauthConnections; $i++){
    $config.oauthConnections += $oauthConnection
}

# Add botWebAppName and resourceGroupName for connecting skills
$config.cognitiveModels | Add-Member -Force -Name "botWebAppName" -Value "" -MemberType NoteProperty
$config.cognitiveModels | Add-Member -Force -Name "resourceGroupName" -Value "" -MemberType NoteProperty

$config | ConvertTo-Json -depth 4 | Set-Content $jsonfile
