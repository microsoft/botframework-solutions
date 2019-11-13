---
layout: tutorial
category: Solution Accelerators
subcategory: Enable continuous deployment
title: Configure the pipeline to update bot services
order: 3
---

# Tutorial: {{page.subcategory}}

## {{ page.title }}
{:.no_toc}

1. First add variables that will be used to connect your project to the bot resources. In the sample screenshot, the highlighted variables are from the `az login` command and the others are used to fill **cognitivemodels.json** In order.
![Configure Release Pipeline 1]({{site.baseurl}}/assets/images/configure_release_pipeline_1.png)

1. Create a **Powershell** task that logs into your Azure account using the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/?view=azure-cli-latest).
```node
az login --user $(botUsername) --password $(botPassword) --tenant $(botTenant)
```
![Configure Release Pipeline 2]({{site.baseurl}}/assets/images/configure_release_pipeline_2.png)

1. Create a **Powershell** task that installs the required dependencies to update bot services.
```node
npm install -g botdispatch ludown luis-apis qnamaker luisgen@2.0.2
```
![Configure Release Pipeline 3]({{site.baseurl}}/assets/images/configure_release_pipeline_3.png)

1. Create an **Azure Powershell** task that checks if the resource group exists before running the next command.
```pwsh
Get-AzureRmResourceGroup -Name $(ResourceGroup) -ErrorVariable notPresent -ErrorAction SilentlyContinue

if ($notPresent)
{
    Write-Host "ResourceGroup $(ResourceGroup) doesn't exist"
}
else
{
    Write-Host "ResourceGroup $(ResourceGroup) exists."
}
```
![Configure Release Pipeline 4]({{site.baseurl}}/assets/images/configure_release_pipeline_4.png)

1. To avoid uploading sensitive keys into the logs, use variable groups to fill in the **Update cognitive models** task. This replaces the configuration of the **cognitivemodels.json** stored in teh artifact configuration. VATest is the example name of the folder that the artifact is located.
```pwsh
Set-Content -Path "$(System.DefaultWorkingDirectory)/$(Release.PrimaryArtifactSourceAlias)/drop/VATest/cognitivemodels.json" -Value '{
    "cognitiveModels": {
    "en": {
        "dispatchModel": {
        "subscriptionkey": "$(SubscriptionKey)",
        "type": "dispatch",
        "region": "$(Region)",
        "authoringRegion": "$(AuthoringRegion)",
        "appid": "$(AppId)",
        "authoringkey": "$(LuisAuthoringKey)",
        "name": "$(BotName)_Dispatch"
        },
        "languageModels": [
        {
            "region": "$(Region)",
            "id": "General",
            "name": "$(botName)_General",
            "authoringRegion": "$(AuthoringRegion)",
            "authoringkey": "$(LuisAuthoringKey)",
            "appid": "$(AppIdLanguageModels)",
            "subscriptionkey": "$(SubscriptionKey)",
            "version": "0.1"
        }
        ],
        "knowledgebases": [
        {
            "kbId": "$(KbIdChitchat)",
            "id": "Chitchat",
            "subscriptionKey": "$(SubscriptionKeyKb)",
            "hostname": "$(hostname)",
            "endpointKey": "$(endpointKey)",
            "name": "Chitchat"
        },
        {
            "kbId": "$(KbIdFaq)",
            "id": "Faq",
            "subscriptionKey": "$(SubscriptionKeyKb)",
            "hostname": "$(hostname)",
            "endpointKey": "$(endpointKey)",
            "name": "Faq"
        }
        ]
    }
    },
    "defaultLocale": "en-us"
}' | ConvertFrom-Json
```
![Configure Release Pipeline 5]({{site.baseurl}}/assets/images/configure_release_pipeline_5.png)

1. Finally, the **Run update cognitive models script** task runs the following script to update the Bot Services
```
pwsh.exe -ExecutionPolicy Bypass -File $(System.DefaultWorkingDirectory)/csharp-Virtual-Assistant-Sample/drop/VATest/Deployment/Scripts/update_cognitive_models.ps1
```
![Configure Release Pipeline 6]({{site.baseurl}}/assets/images/configure_release_pipeline_6.png)