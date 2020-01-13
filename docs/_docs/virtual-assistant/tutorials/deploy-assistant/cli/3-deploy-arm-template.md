---
layout: tutorial
category: Virtual Assistant
subcategory: Deploy
language: cli
title: Deploy an Azure Resource Manager (ARM) templates
order: 3
---

# Tutorial: {{page.subcategory}} ({{page.language}})
## {{page.title}}

Run the following command to deploy the Virtual Assistant ARM template:
```
az group deployment create `
    --resource-group "resource-group-name" `
    --template-file "path-to-arm-template"`
    --parameters "path-to-arm-parameters-file" `
    --parameters microsoftAppId='ms-app-id' microsoftAppPassword='ms-app-pw'
```
