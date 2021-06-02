---
layout: tutorial
category: Virtual Assistant
subcategory: Deploy
language: web
title: Update application settings
order: 4
---

# Tutorial: {{page.subcategory}} ({{page.language}})
## {{page.title}}

After your Azure resources have been deployed, fill in the following keys and secrets in `appsettings.json` with the values from your deployed resources:
```json
{
  "microsoftAppId": "",
  "microsoftAppPassword": "",
  "ApplicationInsights": {
    "InstrumentationKey": ""
  },
  "blobStorage": {
    "connectionString": "",
    "container": "transcripts"
  },
  "cosmosDb": {
    "containerId": "botstate-collection",
    "databaseId": "botstate-db",
    "cosmosDBEndpoint": "",
    "authKey": ""
  }
}
```