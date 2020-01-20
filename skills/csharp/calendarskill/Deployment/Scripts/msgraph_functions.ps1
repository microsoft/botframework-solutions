function RequestAuthorization($appId)
{
    $url = "https://login.microsoftonline.com/common/oauth2/v2.0/devicecode" 

    $headers = @{
        'Content-Type' = 'application/x-www-form-urlencoded'}

    $body = @{
    "client_id" = "$appid"
    'scope' = 'Place.Read.All' }

   $result = Invoke-RestMethod -Uri $url -Headers $headers -Method Post -Body $body
   return $result
}

function RequestAccessToken($appId, $deviceCode)
{
    $url = "https://login.microsoftonline.com/common/oauth2/v2.0/token" 

    $headers = @{
        'Content-Type' = 'application/x-www-form-urlencoded'}

    $body = @{
    "client_id" = "$appid"
    "grant_type" = "urn:ietf:params:oauth:grant-type:device_code"
    'code' = "$deviceCode" }

   $result = Invoke-RestMethod -Uri $url -Headers $headers -Method Post -Body $body
   return $result
}

function GetMeetingRoom($accessToken)
{
    $url = "https://graph.microsoft.com/beta/places/microsoft.graph.room"
    $headers = @{
        'Authorization' = "Bearer $($accessToken)"
        'Content-Type' = 'application/json' 
        'Accept' = 'application/json' }

   $result = Invoke-RestMethod -Uri $url -Headers $headers
   return $result
}