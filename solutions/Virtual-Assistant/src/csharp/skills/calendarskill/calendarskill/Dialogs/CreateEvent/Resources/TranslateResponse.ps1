param(
    [string]$PATH = "./",
    [array]$LOCALES = @("de", "es", "fr", "it"),
    [string]$FILTER = "*Responses.json"
)
# Recover the original utterance with {TestString} rather than number token {0}
function SentitizeTheText {
    param (
        $utterance,
        $tokens
    )
    $response = $utterance
    foreach ($key in $tokens.Keys)
    {
        $token = $tokens.$key
        $response = $response.Replace("{$key}", $token)
    }
    return $response
}  
# Get Bing Translate result for pure text
function GetBingResultForText {
    param (
        $locales,
        $utterance
    )
    Write-Host "Getting the translate response..."
    $url = "https://api.cognitive.microsofttranslator.com"
    $route = "/translate?api-version=3.0"

    foreach ($locale in $locales)
    {
        $route = $route + "&to=$locale"
    }
    
    $requestUrl = $url + $route
    $header = 
    @{
        "Ocp-Apim-Subscription-Key" = "2f3f99ca52f84036a24c65fedf09b3e4"
        "Content-Type" = "application/json"
    }
    $param = @(@{"text" = $utterance})
    $json = ConvertTo-Json -InputObject $param
    $response = Invoke-WebRequest -Uri $requestUrl -Headers $header -Body $json -Method POST
    $jsonResponse = $response.Content | Out-String | ConvertFrom-Json 
    $returnResult = @{}
    $translation = $jsonResponse[0].translations
    foreach ($item in $translation)
    {
        $to = $item.to
        $text = $item.text
        $returnResult.$to = $text
    }
    return $returnResult
    # {
    #     "de": "de string",
    #     "fr": "fr sring"
    # }
}
# Get Bing Translate result for desentisized utterance struct
function GetBingResult {
    param (
        $locales,
        $utterance
    )
    Write-Host "Getting the translate response..."
    $url = "https://api.cognitive.microsofttranslator.com"
    $route = "/translate?api-version=3.0"

    foreach ($locale in $locales)
    {
        $route = $route + "&to=$locale"
    }
    
    $requestUrl = $url + $route
    $header = 
    @{
        "Ocp-Apim-Subscription-Key" = "2f3f99ca52f84036a24c65fedf09b3e4"
        "Content-Type" = "application/json"
    }
    $param = @(@{"text" = $utterance.text})
    $json = ConvertTo-Json -InputObject $param
    $response = Invoke-WebRequest -Uri $requestUrl -Headers $header -Body $json -Method POST
    $jsonResponse = $response.Content | Out-String | ConvertFrom-Json 
    $returnResult = @{}
    $translation = $jsonResponse[0].translations
    foreach ($item in $translation)
    {
        $to = $item.to
        $text = $item.text
        $returnResult.$to = SentitizeTheText $text $utterance.tokens
    }
    return $returnResult
    # {
    #     "de": "de string",
    #     "fr": "fr sring"
    # }
}
# Replace all {TestString} token to {0} to avoid the translator translating the characters surrounded by {}
function DesensitizeTheText {
    param (
        $utterance
    )
    $patternForToken = [regex]::new("{[a-zA-Z]+}")
    $tokens = $patternForToken.Matches($utterance)

    $theReplaceResult = @{}
    for ($i = 0; $i -lt $tokens.Count; $i++) {
        $curText = $tokens[$i].Value
        $utterance = $utterance.Replace($curText, "{$i}")
        if (!$theReplaceResult.ContainsKey("tokens"))
        {
            $theReplaceResult.tokens = @{}
        }
        $theReplaceResult.tokens.$i = $curText
    }
    $theReplaceResult.text = $utterance
    
    return $theReplaceResult

    # {
    #     text : the utterance with {1}{2}{3},
    #     1: {string1}
    #     2: {string2}
    # }
}
# Create Multi local json file
function CreateMultiLocaleJson {
    param (
        $translateResponse,
        $locale = "zh"
    )
    $jsonResult = @{}
    foreach ($key in $translateResponse.Keys)
    {
        $responseItem = $translateResponse.$key
        foreach ($reply in $responseItem.replies)
        {
            if (!$jsonResult.ContainsKey($key))
            {
                $jsonResult.$key = @{}
            }
            
            $jsonResult.$key.inputHint = $responseItem.inputHint

            if ($responseItem.ContainsKey("suggestedActions"))
            {
                $jsonResult.$key.suggestedActions = @()
                foreach ($actionText in $responseItem.suggestedActions)
                {   
                    if ($locale -eq "zh")
                    {
                        $jsonResult.$key.suggestedActions += $actionText."$locale-Hans"
                    }
                    else 
                    {
                        $jsonResult.$key.suggestedActions += $actionText.$locale
                    }
                }
            }

            if (!$jsonResult.$key.ContainsKey("replies"))
            {
                $jsonResult.$key.replies = @()
            }

            if ($locale -eq "zh")
            {
                $jsonResult.$key.replies += @{
                    speak = $reply.speakTrans."$locale-Hans"
                    text = $reply.textTrans."$locale-Hans"
                }
            }
            else 
            {
                $jsonResult.$key.replies += @{
                    speak = $reply.speakTrans.$locale
                    text = $reply.textTrans.$locale
                }
            }
        }
    }
    return $jsonResult
}
# Remove unexpected indentation in json file
function Format-Json([Parameter(Mandatory, ValueFromPipeline)][String] $json) {
    $indent = 0;
    ($json -Split '\n' |
      % {
        if ($_ -match '[\}\]]') {
          # This line contains  ] or }, decrement the indentation level
          $indent--
        }
        $line = (' ' * $indent * 2) + $_.TrimStart().Replace(':  ', ': ')
        if ($_ -match '[\{\[]') {
          # This line contains [ or {, increment the indentation level
          $indent++
        }
        $line
    }) -Join "`n"
  }

# Replace escape char for ' < > except double quotes "
function Remove-EscapeChar {
    param (
        [Parameter(Mandatory, ValueFromPipeline)][string] $sRawJson
    )
    $dReplacements = @{
        "\\u003c" = "<"
        "\\u003e" = ">"
        "\\u0027" = "'"
    }
    foreach ($oEnumerator in $dReplacements.GetEnumerator()) {
        $sRawJson = $sRawJson -replace $oEnumerator.Key, $oEnumerator.Value
    }
    return $sRawJson
}
# Get all files recursivly under $PATH matching the $FILTER
$Files = Get-ChildItem -Path $PATH -Filter $FILTER -Recurse -ErrorAction SilentlyContinue -Force

foreach ($File in $Files)
{
    try 
    {
        if (!$File.FullName.Contains("\bin\"))
        {
            $fileObj = Get-Content $File.FullName | Out-String | ConvertFrom-Json
            
            $translateResponse = @{}
            foreach ($item in $fileObj.PSObject.Properties)
            {
                $key = $item.Name
                $replies = $item.Value.replies
                if ("suggestedActions" -in $item.Value.PSObject.Properties.Name)
                {
                    foreach ($actionText in $item.Value.suggestedActions)
                    {
                        $transAction = GetBingResultForText $LOCALES $actionText
                        if (!$translateResponse.ContainsKey($key))
                        {
                            $translateResponse.$key = @{}
                            $translateResponse.$key.replies = @()
                            $translateResponse.$key.inputHint = $item.Value.inputHint
                            $translateResponse.$key.suggestedActions = @()
                        }
                        $translateResponse.$key.suggestedActions += $transAction
                    }
                }
                foreach ($reply in $replies)
                {
                    $speak = $reply.speak
                    $text = $reply.text
                    if ($speak -eq $text)
                    {
                        $deText = DesensitizeTheText $text
                        $transText = GetBingResult $LOCALES $deText
                        if (!$translateResponse.ContainsKey($key))
                        {
                            $translateResponse.$key = @{}
                            $translateResponse.$key.replies = @()
                            $translateResponse.$key.inputHint = $item.Value.inputHint
                        }
                    
                        $translateResponse.$key.replies += @{
                            speakTrans = $transText
                            textTrans = $transText
                            speak = $deText
                            text = $deText
                        }
                        
                    }
                    else 
                    {
                        $deText = DesensitizeTheText $text
                        $transText = GetBingResult $LOCALES $deText 
                        $deSpeak = DesensitizeTheText $speak
                        $transSpeak = GetBingResult $LOCALES $deSpeak
                        if (!$translateResponse.ContainsKey($key))
                        {
                            $translateResponse.$key = @{}
                            $translateResponse.$key.replies = @()
                            $translateResponse.$key.inputHint = $item.Value.inputHint
                        }
                        
                        $translateResponse.$key.replies += @{
                            speakTrans = $transSpeak
                            textTrans = $transText
                            speak = $deSpeak
                            text = $deText
                        }
                        
                    }
                }
            }

            foreach ($loc in $LOCALES)
            {
                
                $result = CreateMultiLocaleJson $translateResponse $loc
                $name = $File.FullName.Substring(0, $File.FullName.Length - 4) + "$loc.json"
                Write-Host "$name start..."
                $result | ConvertTo-Json -Depth 50 | Format-Json | Remove-EscapeChar | Out-File $name
            }
        }
    
    }
    catch
    {
        Write-Host $_.Exception.Message
        # do nothing
    }

}


