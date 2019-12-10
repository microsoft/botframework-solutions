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

$config | ConvertTo-Json -depth 4 | Set-Content $jsonfile