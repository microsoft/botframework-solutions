Param(
	[string] $name,
	[string] $environment,
	[string] $luisAuthoringKey,
	[string] $luisAuthoringRegion,
	[string] $projFolder = $(Get-Location),
	[string] $botPath,
	[string] $logFile = $(Join-Path $PSScriptRoot .. "deploy_log.txt")
)

if ($PSVersionTable.PSVersion.Major -lt 6) {
	Write-Host "! Powershell 6 is required, current version is $($PSVersionTable.PSVersion.Major), please refer following documents for help."
	Write-Host "For Windows - https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-windows?view=powershell-6"
	Write-Host "For Mac - https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-macos?view=powershell-6"
	Break
}

if ((dotnet --version) -lt 3) {
	Write-Host "! dotnet core 3.0 is required, please refer following documents for help."
	Write-Host "https://dotnet.microsoft.com/download/dotnet-core/3.0"
	Break
}

# Get mandatory parameters
if (-not $name) {
	$name = Read-Host "? Bot Web App Name"
}

if (-not $environment) {
	$environment = Read-Host "? Environment Name (single word, all lowercase)"
	$environment = $environment.ToLower().Split(" ") | Select-Object -First 1
}


# Reset log file
if (Test-Path $logFile) {
	Clear-Content $logFile -Force | Out-Null
}
else {
	New-Item -Path $logFile | Out-Null
}

# Check for existing deployment files
if (-not (Test-Path (Join-Path $projFolder '.deployment'))) {
	# Add needed deployment files for az
	az bot prepare-deploy --lang Csharp --code-dir $projFolder --proj-file-path BotProject.csproj --output json | Out-Null
}

# Delete src zip, if it exists
$zipPath = $(Join-Path $projFolder 'code.zip')
if (Test-Path $zipPath) {
	Remove-Item $zipPath -Force | Out-Null
}

# Init user secret id
dotnet user-secrets init

# Perform dotnet publish step ahead of zipping up
$publishFolder = $(Join-Path $projFolder 'bin\Release\netcoreapp3.1')
dotnet publish -c release -o $publishFolder -v q > $logFile


# Copy bot files to running folder
$remoteBotPath = $(Join-Path $publishFolder "ComposerDialogs")
$localBotPath = $(Join-Path $projFolder "ComposerDialogs")
Remove-Item $remoteBotPath -Recurse -ErrorAction Ignore

if ($botPath) {
	Write-Host "Publishing dialogs from external bot project: $($botPath)"
	Copy-Item -Path $botPath -Recurse -Destination $remoteBotPath -Container -Force
}
else {
	Copy-Item -Path $localBotPath -Recurse -Destination $publishFolder -Container -Force
}

# Try to get luis config from appsettings
$settings = Get-Content $(Join-Path $projFolder appsettings.deployment.json) | ConvertFrom-Json
$luisSettings = $settings.luis

if (-not $luisAuthoringKey) {
	$luisAuthoringKey = $luisSettings.authoringKey
}

if (-not $luisEndpointKey) {
	$luisEndpointKey = $luisSettings.endpointKey
}

if (-not $luisAuthoringRegion) {
	$luisAuthoringRegion = $luisSettings.region
}

# Add Luis Config to appsettings
if ($luisAuthoringKey -and $luisAuthoringRegion) {

	Set-Location -Path $remoteBotPath
	$models = Get-ChildItem $remoteBotPath -Recurse -Filter "*.lu" | Resolve-Path -Relative

	$noneEmptyModels = [System.Collections.ArrayList]@()

	foreach ($model in $models) {
		$stringContent = Get-Content $model | Out-String
		if ($stringContent.Length -gt 0) {
			$noneEmptyModels.Add($model)
		}
	}

	# Generate Luconfig.json file
	$luconfigjson = @{
		"name"            = $name;
		"defaultLanguage" = "en-us";
		"models"          = $noneEmptyModels
	}
	
	$luconfigjson | ConvertTo-Json -Depth 100 | Out-File $(Join-Path $remoteBotPath luconfig.json)

	# Execute bf luis:build command
	if (Get-Command bf -errorAction SilentlyContinue) {
		$customizedSettings = Get-Content $(Join-Path $remoteBotPath settings appsettings.json) | ConvertFrom-Json
		$customizedEnv = $customizedSettings.luis.environment
		
		# create generated folder if not exists
		if (!(Test-Path generated)) {
			New-Item -ItemType Directory -Force -Path generated
		}
		
		bf luis:build --in .\ --botName $name --authoringKey $luisAuthoringKey --dialog --out .\generated --suffix $customizedEnv -f --region $luisAuthoringRegion
	}
	else {
		Write-Host "bf luis:build does not exist, use the following command to install:"
		Write-Host "1. npm config set registry https://botbuilder.myget.org/F/botframework-cli/npm/"
		Write-Host "2. npm install -g @microsoft/botframework-cli"
		Write-Host "3. npm config set registry http://registry.npmjs.org"
		Break
	}
	
	if ($?) {
		Write-Host "lubuild succeeded"
	}
	else {
		Write-Host "lubuild failed, please verify your luis models."
		Break	
	}

	Set-Location -Path $projFolder

	# change setting file in publish folder
	if (Test-Path $(Join-Path $publishFolder appsettings.deployment.json)) {
		$settings = Get-Content $(Join-Path $publishFolder appsettings.deployment.json) | ConvertFrom-Json
	}
	else {
		$settings = New-Object PSObject
	}

	$luisConfigFiles = Get-ChildItem -Path $publishFolder -Include "luis.settings*" -Recurse -Force

	$luisAppIds = @{ }

	foreach ($luisConfigFile in $luisConfigFiles) {
		$luisSetting = Get-Content $luisConfigFile.FullName | ConvertFrom-Json
		$luis = $luisSetting.luis
		$luis.PSObject.Properties | Foreach-Object { $luisAppIds[$_.Name] = $_.Value }
	}

	$luisEndpoint = "https://$luisAuthoringRegion.api.cognitive.microsoft.com"

	$luisConfig = @{ }
	
	$luisConfig["endpoint"] = $luisEndpoint
	$luisConfig["endpointKey"] = $luisEndpointKey

	foreach ($key in $luisAppIds.Keys) { $luisConfig[$key] = $luisAppIds[$key] }

	$settings | Add-Member -Type NoteProperty -Force -Name 'luis' -Value $luisConfig

	$settings | ConvertTo-Json -depth 100 | Out-File $(Join-Path $publishFolder appsettings.deployment.json)

	$tokenResponse = (az account get-access-token) | ConvertFrom-Json
	$token = $tokenResponse.accessToken

	if (-not $token) {
		Write-Host "! Could not get valid Azure access token"
		Break
	}

	Write-Host "Getting Luis accounts..."
	$luisAccountEndpoint = "$luisEndpoint/luis/api/v2.0/azureaccounts"
	$luisAccount = $null
	try {
		$luisAccounts = Invoke-WebRequest -Method GET -Uri $luisAccountEndpoint -Headers @{"Authorization" = "Bearer $token"; "Ocp-Apim-Subscription-Key" = $luisAuthoringKey } | ConvertFrom-Json

		foreach ($account in $luisAccounts) {
			if ($account.AccountName -eq "$name-$environment-luis") {
				$luisAccount = $account
				break
			}
		}
	}
	catch {
		Write-Host "Return invalid status code while gettings luis accounts: $($_.Exception.Response.StatusCode.Value__), error message: $($_.Exception.Response)"
		break
	}

	$luisAccountBody = $luisAccount | ConvertTo-Json

	# Assign each luis id in luisAppIds with the endpoint key
	foreach ($k in $luisAppIds.Keys) {
		$luisAppId = $luisAppIds.Item($k)
		Write-Host "Assigning to Luis app id: $luisAppId"
		$luisAssignEndpoint = "$luisEndpoint/luis/api/v2.0/apps/$luisAppId/azureaccounts"
		try {
			$response = Invoke-WebRequest -Method POST -ContentType application/json -Body $luisAccountBody -Uri $luisAssignEndpoint -Headers @{"Authorization" = "Bearer $token"; "Ocp-Apim-Subscription-Key" = $luisAuthoringKey } | ConvertFrom-Json
			Write-Host $response
		}
		catch {
			Write-Host "Return invalid status code while assigning key to luis apps: $($_.Exception.Response.StatusCode.Value__), error message: $($_.Exception.Response)"
			exit
		}
	}
}

$resourceGroup = "$name-$environment"

if ($?) {     
	# Compress source code
	Get-ChildItem -Path "$($publishFolder)" | Compress-Archive -DestinationPath "$($zipPath)" -Force | Out-Null

	# Publish zip to Azure
	Write-Host "> Publishing to Azure ..." -ForegroundColor Green
	$deployment = (az webapp deployment source config-zip `
			--resource-group $resourceGroup `
			--name "$name-$environment" `
			--src $zipPath `
			--output json) 2>> $logFile
		
	if ($deployment) {
		Write-Host "Publish Success"
	}
	else {
		Write-Host "! Deploy failed. Review the log for more information." -ForegroundColor DarkRed
		Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed
	}
} 
else {       
	Write-Host "! Could not deploy automatically to Azure. Review the log for more information." -ForegroundColor DarkRed
	Write-Host "! Log: $($logFile)" -ForegroundColor DarkRed    
}       
