#Requires -Version 6

Param(
    [string] $name,
	[string] $resourceGroup,
    [string] $cosmosDbAccount,
    [string] $primaryKey,
    [string] $databaseId,
    [string] $collectionId,
    [string] $cosmosDbPrimaryKey,
    [string] $subscriptionID,
    [string] $appId,
	[string] $languages = "en-us",
	[string] $projDir = $(Get-Location),
	[string] $logFile = $(Join-Path $PSScriptRoot .. "enable_findmeetingroom_log.txt")
)

. $PSScriptRoot\azuresearch_functions.ps1
. $PSScriptRoot\msgraph_functions.ps1

# Reset log file
if (Test-Path $logFile) {
	Clear-Content $logFile -Force | Out-Null
}
else {
	New-Item -Path $logFile | Out-Null
}

# Check for AZ CLI and confirm version
if (Get-Command az -ErrorAction SilentlyContinue) {
    $azcliversionoutput = az -v
    [regex]$regex = '(\d{1,3}.\d{1,3}.\d{1,3})'
    [version]$azcliversion = $regex.Match($azcliversionoutput[0]).value
    [version]$minversion = '2.0.72'

    if ($azcliversion -ge $minversion) {
        $azclipassmessage = "AZ CLI passes minimum version. Current version is $azcliversion"
        Write-Debug $azclipassmessage
        $azclipassmessage | Out-File -Append -FilePath $logfile
    }
    else {
        $azcliwarnmessage = "You are using an older version of the AZ CLI, `
    please ensure you are using version $minversion or newer. `
    The most recent version can be found here: http://aka.ms/installazurecliwindows"
        Write-Warning $azcliwarnmessage
        $azcliwarnmessage | Out-File -Append -FilePath $logfile
    }
}
else {
    $azclierrormessage = 'AZ CLI not found. Please install latest version.'
    Write-Error $azclierrormessage
    $azclierrormessage | Out-File -Append -FilePath $logfile
}

# Get mandatory parameters
if (-not $name) {
    $name = Read-Host "? Bot Name (used as default name for resource group and deployed resources)"
}

if (-not $resourceGroup) {
	$resourceGroup = $name
}

if (-not $cosmosDbAccount) {
    $cosmosDbAccount = Read-Host "? cosmosDb account (used for importing room data)"
}

if (-not $primaryKey) {
    $primaryKey = Read-Host "? primaryKey of cosmosDb account"
}

if (-not $databaseId) {
    $databaseId = "room-db"
}

if (-not $collectionId) {
    $collectionId = "room-collection"
}

if (-not $subscriptionID){
    $subscriptionID = Read-Host "? your subscriptionID (used for user authentification to get room data from MSGraph)"
}

if (-not $appId) {
    $appId = Read-Host "? MSA appId (used for user authentification to get room data from MSGraph)"
}


# Check the CosmosDB package and install it.
if(!(Get-Package CosmosDB)) {Install-Module -Name CosmosDB}

# Create database and collection for import meeting room data if doesn't exist.
$key = ConvertTo-SecureString -String $primaryKey -AsPlainText -Force
$cosmosDbContext = New-CosmosDbContext -Account $cosmosDbAccount -Key $key
$databaseList = Get-CosmosDbDatabase -Context $cosmosDbContext | ConvertTo-Json | ConvertFrom-Json
$database = $databaseList | where {$_.id -eq $databaseId}
if (-not $database) {
    Write-Host "> Creating database ..." -NoNewline
    New-CosmosDbDatabase -Context $cosmosDbContext -Id $databaseId 2>> $logFile | Out-Null
    Write-Host "Done." -ForegroundColor Green
}

$cosmosDbContext = New-CosmosDbContext -Account $cosmosDbAccount -Database $databaseId -Key $key
$collectionList = Get-CosmosDbCollection -Context $cosmosDbContext | ConvertTo-Json | ConvertFrom-Json
$collection = $collectionList | where {$_.id -eq $collectionId}
if (-not $collection){
    Write-Host "> Creating collection ..." -NoNewline
    New-CosmosDbCollection -Context $cosmosDbContext -Id $collectionId 2>> $logFile | Out-Null
    Write-Host "Done." -ForegroundColor Green
}

# Get timestamp
$timestamp = Get-Date -f MMddyyyyHHmmss

# Deploy Azure Search services
Write-Host "> Validating Azure Search deployment ..." -NoNewline
$validation = az group deployment validate `
	--resource-group $resourcegroup `
	--template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'azuresearch.json')" `
	--parameters name=$name `
	--output json

if ($validation) {
	$validation >> $logFile
	$validation = $validation | ConvertFrom-Json

	if (-not $validation.error) {
		Write-Host "validate pass." -ForegroundColor Green
		Write-Host "> Deploying Azure Search services (this could take a while)..." -ForegroundColor Yellow -NoNewline
		$deployment = az group deployment create `
			--name $timestamp `
			--resource-group $resourceGroup `
			--template-file "$(Join-Path $PSScriptRoot '..' 'Resources' 'azuresearch.json')" `
			--parameters name=$name `
            --output json 2>> $logFile | Out-Null
            
		Write-Host "Done." -ForegroundColor Green
	}
	else {
        Write-Host "! Template is not valid with provided parameters. Review the log for more information." -ForegroundColor Red
        Write-Host "! Error: $($validation.error.message)"  -ForegroundColor Red
        Write-Host "! Log: $($logFile)" -ForegroundColor Red
        Write-Host "+ To delete this resource group, run 'az group delete -g $($resourceGroup) --no-wait'" -ForegroundColor Magenta
		Break
	}
}

#Get deployment outputs
$outputs = (az group deployment show `
	--name $timestamp `
	--resource-group $resourceGroup `
	--query properties.outputs `
    --output json) 2>> $logFile

# If it succeeded then we perform the remainder of the steps
if ($outputs)
{
	# Log and convert to JSON
	$outputs >> $logFile
    $outputs = $outputs | ConvertFrom-Json
    if ($outputs.azureSearch.value.azureSearchAccount) { $azureSearchAccount = $outputs.azureSearch.value.azureSearchAccount }
    if ($outputs.azureSearch.value.apiKey) { $apiKey = $outputs.azureSearch.value.apiKey }
}

# Connect to MSGraph to get meeting room data
$authorizationResult = RequestAuthorization `
    -subscriptionID $subscriptionID `
    -appId $appId
if ($authorizationResult.error)
{
    Write-Host "! Error: $($authorizationResult.error_description)"  -ForegroundColor Red
    Write-Host "+ Verify the -subscriptionID and -appId parameters are correct." -ForegroundColor Magenta
    break
}

$deviceCode = $authorizationResult.device_code
$message = $authorizationResult.message

Write-Host $message 
$confirmSignedIn = Read-Host "? Have you signed in ? [y/n]"
if ($confirmSignedIn -ne 'y') {
    Write-Host "! Error: no user signed in"  -ForegroundColor Red
    break;
}

$accessTokenResult =  RequestAccessToken `
        -subscriptionID $subscriptionID `
        -appId $appId `
        -deviceCode $deviceCode

if ($accessTokenResult.error)
{
    Write-Host "! Error: $($accessTokenResult.error_description)"  -ForegroundColor Red
    break
}

$accessToken = $accessTokenResult.access_token
Write-Host "> getting data from MSGraph ..." -NoNewline
$roomData = GetMeetingRoom -accessToken $accessToken
Write-Host "Done." -ForegroundColor Green

$roomData = $roomData | ConvertTo-Json | ConvertFrom-Json
Write-Host "> importing data into CosmosDb (this could take a while)..." -NoNewline
foreach ($room in $roomData.value)
{
    $room = ConvertTo-Json $room
    New-CosmosDbDocument -Context $cosmosDbContext -CollectionId $collectionId -DocumentBody $room 2>> $logFile | Out-Null
}
Write-Host "Done." -ForegroundColor Green

$dataSourceName = "room-datasource"
$indexName = "room-index"
$indexerName = "room-indexer"

# create data source in Azure Search
Write-Host "> creating data source ..." -NoNewline
$dataSourceResult = CreateDataSource `
    -azureSearchAccount $azureSearchAccount `
    -apiKey $apiKey `
    -cosmosDbAccount $cosmosDbAccount `
    -primaryKey $primaryKey `
    -database $databaseId `
    -collection $collectionId `
    -dataSourceName $dataSourceName

if ($dataSourceResult){
    Write-Host "Done." -ForegroundColor Green
}else {
    Write-Host "! Error: failed"  -ForegroundColor Red
    break;   
}

# build index in Azure Search
Write-Host "> building index ..." -NoNewline
$buildIndexResult = BuildIndex `
    -azureSearchAccount $azureSearchAccount `
    -apiKey $apiKey `
    -indexName $indexName

if ($buildIndexResult){
    Write-Host "Done." -ForegroundColor Green
}else {
    Write-Host "! Error: failed"  -ForegroundColor Red
    break;   
}    

# create indexer in Azure Search
Write-Host "> creating indexer..." -NoNewline
$createIndexerResult = CreateIndexer `
    -azureSearchAccount $azureSearchAccount `
    -apiKey $apiKey `
    -indexerName $indexerName `
    -dataSourceName $dataSourceName `
    -indexName $indexName
if ($createIndexerResult){
    Write-Host "Done." -ForegroundColor Green
}else {
    Write-Host "! Error: failed"  -ForegroundColor Red
    break;   
}        

if (-not (Test-Path (Join-Path $projDir 'appsettings.json')))
{
	Write-Host "! Could not find an 'appsettings.json' file in the current directory." -ForegroundColor Red
	Write-Host "+ Please re-run this script from your project directory." -ForegroundColor Magenta
	Break
}

# Update appsettings.json
Write-Host "> Updating appsettings.json ..." -NoNewline
$settings = Get-Content -Encoding utf8 $(Join-Path $projDir appsettings.json) | ConvertFrom-Json

$azureSearchParameter = New-Object PSObject
$azureSearchParameter | Add-Member -Type NoteProperty -Force -Name 'searchServiceName' -Value $azureSearchAccount
$azureSearchParameter | Add-Member -Type NoteProperty -Force -Name 'searchServiceAdminApiKey' -Value $apiKey
$azureSearchParameter | Add-Member -Type NoteProperty -Force -Name 'searchIndexName' -Value $indexName

$settings | Add-Member -Type NoteProperty -Force -Name 'azureSearch' -Value $azureSearchParameter
$settings | ConvertTo-Json -depth 100 | Out-File -Encoding utf8 $(Join-Path $projDir appsettings.json)
Write-Host "Done." -ForegroundColor Green
