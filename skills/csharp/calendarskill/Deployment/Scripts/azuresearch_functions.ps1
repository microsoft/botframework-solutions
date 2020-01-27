function CreateDataSource($azureSearchAccount, $apiKey, $cosmosDbAccount, $primaryKey, $database, $collection, $dataSourceName)
{
    $url = "https://$($azureSearchAccount).search.windows.net/datasources?api-version=2019-05-06"
    $headers = @{
        'api-key' = $apiKey
        'Content-Type' = 'application/json' 
        'Accept' = 'application/json' }


    $body = @"
    {
    "name": "$dataSourceName",
    "type": "cosmosdb",
    "credentials": {
        "connectionString": "AccountEndpoint=https://$($cosmosDbAccount).documents.azure.com;AccountKey=$($primaryKey);Database=$($database)"
    },
    "container": { "name": "$($collection)", "query": null }
    }
"@

    $result = Invoke-RestMethod -Uri $url -Headers $headers -Method Post -Body $body | ConvertTo-Json
    return $result
}

function BuildIndex($azureSearchAccount, $indexName, $apiKey)
{   
    $url = "https://$($azureSearchAccount).search.windows.net/indexes/$($indexName)?api-version=2019-05-06"
    $headers = @{
        'api-key' = $apiKey
        'Content-Type' = 'application/json' 
        'Accept' = 'application/json' }

    $body = @"
    {
    "name": "$indexName",
    "fields": [
        {"name": "Id", "type": "Edm.String", "facetable": false, "key": true, "retrievable": true, "searchable": true, "sortable": false, "filterable": false},
        {"name": "DisplayName", "type": "Edm.String", "facetable": false, "key": false, "retrievable": true, "searchable": true, "sortable": false, "filterable": false},
        {"name": "EmailAddress", "type": "Edm.String", "facetable": false, "key": false, "retrievable": true, "searchable": true, "sortable": false, "filterable": false},
        {"name": "Building", "type": "Edm.String", "facetable": false, "key": false, "retrievable": true, "searchable": true, "sortable": false, "filterable": false},
        {"name": "FloorNumber", "type": "Edm.String", "facetable": false, "key": false, "retrievable": true, "searchable": false, "sortable": false, "filterable": true}
    ]
    }
"@
    $result = Invoke-RestMethod -Uri $url -Headers $headers -Method Put -Body $body | ConvertTo-Json

    return $result
}

function CreateIndexer($azureSearchAccount, $indexerName, $apiKey, $dataSourceName, $indexName)
{   

    $headers = @{
        'api-key' = $apiKey
        'Content-Type' = 'application/json' 
        'Accept' = 'application/json' }

    $body = @"
    {
        "name" : "$indexerName",
        "dataSourceName" : "$dataSourceName",
        "targetIndexName" : "$indexName"
    }
"@

    $url = "https://$($azureSearchAccount).search.windows.net/indexers?api-version=2019-05-06"

    $result = Invoke-RestMethod -Uri $url -Headers $headers -Method Post -Body $body | ConvertTo-Json

    return $result
}
