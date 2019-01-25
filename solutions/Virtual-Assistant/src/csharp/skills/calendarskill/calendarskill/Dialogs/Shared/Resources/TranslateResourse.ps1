param(
    [string]$PATH,
    [array]$LOCALES = @("de", "es", "fr", "it"),
    [string]$FILTER = "*Strings.resx"
)
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
}
function CreateLocaleResourse {
    param (
        $locales,
        $translateResponse,
        $file
    )
    foreach ($locale in $locales)
    {
        $resxName = $file.Substring(0, $file.Length - 4) + $locale + ".resx"
        $resxWriter = New-Object -TypeName "System.Resources.ResXResourceWriter" -ArgumentList $resxName
        foreach ($name in $translateResponse.Keys)
        {
            if ($locale -eq "zh")
            {
                $resxWriter.AddResource($name, $translateResponse.$name."$locale-Hans")
            }
            else 
            {
                $resxWriter.AddResource($name, $translateResponse.$name.$locale)    
            }
            
        }
        $resxWriter.Close()

        Write-Host "$locale Done."
    }

    Write-Host "$file Done."
}

$Files = Get-ChildItem -Path $PATH -Filter $FILTER -Recurse -ErrorAction SilentlyContinue -Force

foreach ($File in $Files)
{
    try 
    {
        Write-Host "$File Start..."
    
        Add-Type -AssemblyName System.Windows.Forms 
        
        $ResourceSet = New-Object -TypeName 'System.Resources.ResXResourceSet' -ArgumentList $File.FullName
        
        $rootResourse = $ResourceSet.GetEnumerator()
        $translateResponse = @{}
        
        $locales = $LOCALES
        while ($rootResourse.MoveNext()) 
        {
            $curRes = GetBingResult $locales $rootResourse.Value
            $key = $rootResourse.key
            $translateResponse.$key = $curRes
        }

        CreateLocaleResourse $locales $translateResponse $File.FullName
    }
    catch
    {
        Write-Host $_.Exception.Message
        # do nothing
    }
}


