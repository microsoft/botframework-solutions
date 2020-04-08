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

1. First of all, **configure the Release State environment** creating the necessary variables. The highlighted variables are the ones for the [az login](https://docs.microsoft.com/en-us/cli/azure/reference-index?view=azure-cli-latest#az-login) command. The rest of them are used to fill the `cognitivemodels.json` file.

![Configure Release Pipeline 1]({{site.baseurl}}/assets/images/configure_release_pipeline_1.png)

1. Create the PowerShell task to **login your Azure account** using [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/?view=azure-cli-latest).
The `AzureUsername` and `AzurePassword` refers to the email and password access of your Azure account. Also, the `AzureTenant` is present in your Azure AD or you can check the ID using a PowerShell console running the command [az account list](https://docs.microsoft.com/en-us/cli/azure/account?view=azure-cli-latest#az-account-list).
```node
az login --user $(AzureUsername) --password $(AzurePassword) --tenant $(AzureTenant)
```
![Configure Release Pipeline 2]({{site.baseurl}}/assets/images/configure_release_pipeline_2.png)

1. Create a PowerShell task to **install the dependencies** to update the Bot's services, using [npmjs](https://www.npmjs.com/).
```node
npm install -g botdispatch ludown luis-apis qnamaker luisgen@2.0.2
```
![Configure Release Pipeline 3]({{site.baseurl}}/assets/images/configure_release_pipeline_3.png)

1. Create an Azure PowerShell task to **validate the resource group** created in your Azure account.
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

1. Create the necessary variables in Pipelines to avoid uploading sensitive keys into the logs,use variable groups to fill in the PowerShell task called **Update cognitive models**.This replaces the configuration of the cognitivemodels.json stored in the artifact configuration.In this case `VATest` is the example name of the folder that the artifact is located.
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
          "name": "$(BotName)_General",
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
          "hostname": "$(Hostname)",
          "endpointKey": "$(EndpointKey)",
          "name": "Chitchat"
      },
      {
          "kbId": "$(KbIdFaq)",
          "id": "Faq",
          "subscriptionKey": "$(SubscriptionKeyKb)",
          "hostname": "$(Hostname)",
          "endpointKey": "$(EndpointKey)",
          "name": "Faq"
      }
      ]
    }
    },
    "defaultLocale": "en-us"
}' | ConvertFrom-Json
```
![Configure Release Pipeline 5]({{site.baseurl}}/assets/images/configure_release_pipeline_5.png)

1. Finally, add a PowerShell task to **update the bot services**.
```
pwsh.exe -ExecutionPolicy Bypass -File $(System.DefaultWorkingDirectory)/csharp-Virtual-Assistant-Sample/drop/VATest/Deployment/Scripts/update_cognitive_models.ps1
```
![Configure Release Pipeline 6]({{site.baseurl}}/assets/images/configure_release_pipeline_6.png)

### YAML Sample

This is the YAML used as example to configure the _Release Stage_ tasks, you can use it as first approach to see how works the _Release_.

```steps:
- powershell: |
   az login --user $(AzureUsername) --password $(AzurePassword) --tenant $(AzureTenant)
   
  pwsh: true
  displayName: 'Az login '

- powershell: |
   npm install -g botdispatch ludown luis-apis qnamaker luisgen@2.0.2
   
  pwsh: true
  displayName: Commands

- task: AzurePowerShell@3
  displayName: 'Check resource group exists'
  inputs:
    azureSubscription: ManxAppPipeline
    ScriptType: InlineScript
    Inline: |
     Get-AzureRmResourceGroup -Name $(ResourceGroup) -ErrorVariable notPresent -ErrorAction SilentlyContinue
     
     if ($notPresent)
     {
         Write-Host "ResourceGroup $(ResourceGroup) doesn't exist"
     }
     else
     {
         Write-Host "ResourceGroup $(ResourceGroup) exists."
     }
    azurePowerShellVersion: LatestVersion

- powershell: |
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
            "name": "$(BotName)_General",
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
            "hostname": "$(Hostname)",
            "endpointKey": "$(EndpointKey)",
            "name": "Chitchat"
        },
        {
            "kbId": "$(KbIdFaq)",
            "id": "Faq",
            "subscriptionKey": "$(SubscriptionKeyKb)",
            "hostname": "$(Hostname)",
            "endpointKey": "$(EndpointKey)",
            "name": "Faq"
        }
        ]
    }
    },
    "defaultLocale": "en-us"
}' | ConvertFrom-Json
pwsh: true
workingDirectory: '$(System.DefaultWorkingDirectory)/csharp-Virtual-Assistant-Sample/drop/VATest'
displayName: 'Update cognitivemodels.json'

- powershell: 'pwsh.exe -ExecutionPolicy Bypass -File $(System.DefaultWorkingDirectory)/csharp-Virtual-Assistant-Sample/drop/VATest/Deployment/Scripts/update_cognitive_models.ps1'
pwsh: true
workingDirectory: '$(System.DefaultWorkingDirectory)/csharp-Virtual-Assistant-Sample/drop/VATest'
displayName: 'Run update cognitive models script'
```

For further information, you can check the [yaml](https://github.com/microsoft/botframework-solutions/tree/master/build/yaml) folder.