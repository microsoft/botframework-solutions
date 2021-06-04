---
layout: tutorial
category: Skills
subcategory: Create
language: typescript
title: Update your Skill Manifest
order: 5
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

A default manifest describing your Skill is provided as part of the project, you can find this within the `src\manifest` folder. Following deployment this requires updating with the deployment URL and Azure AD Application ID. `manifest-1.0` is provided for Power Virtual Agent support only, you should use `manifest-1.1` for Virtual Assistant scenarios.

1. Update `{YOUR_SKILL_URL}` with the URL of your deployed Skill endpoint, this must be prefixed with https.

1. Update `{YOUR_SKILL_APPID}` with the Active Directory AppID of your deployed Skill, you can find this within your `appSettings.json` file.

1. Publish the changes to your Skill endpoint and validate that you can retrieve the manifest using the browser (`/manifest/manifest-1.1.json`). Check the [deployment scripts](https://microsoft.github.io/botframework-solutions/virtual-assistant/handbook/deployment-scripts/) document.
```powershell 
.\Deployment\Scripts\publish.ps1 -botWebAppName {YOUR_SKILL_BOTWEBAPP_NAME} -resourceGroup {YOUR_RESOURCEGROUP_NAME}
```

> **Note**: `{YOUR_SKILL_URL}` is the endpoint URL where the Skill will receive the messages that matches with the "Messaging endpoint" of the Web App Bot resource after deployment (e.g. `https://bf-skill.azurewebsites.net/api/messages`). Also, `{YOUR_SKILL_APPID}` is the `microsoftAppId` value, the `{YOUR_SKILL_BOTWEBAPP_NAME}` is the `botWebAppName` and the `{YOUR_RESOURCEGROUP_NAME}` is the `resourceGroupName` that you can find in the `appsettings.json` file populated after the deployment of the Skill.