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

1. Configure the Release State environment creating variables. The highlighted ones are used for the [az login](https://docs.microsoft.com/en-us/cli/azure/reference-index?view=azure-cli-latest#az_login) command. The rest are used to fill the `cognitivemodels.json` file

    ![Release Pipeline Variables]({{site.baseurl}}/assets/images/configure_release_pipeline_variables.png)

1. Create a PowerShell task to login your Azure account using [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/?view=azure-cli-latest)
    * `AzureUserName`: email to access to your Azure account
    * `AzurePassword`: password to access to your Azure account
    * `AzureTenant`: present in your Azure AD or you can check the ID with [az account list](https://docs.microsoft.com/en-us/cli/azure/account?view=azure-cli-latest#az_account_list)
    ```node
    az login --user $(AzureUsername) --password $(AzurePassword) --tenant $(AzureTenant)
    ```

1. Create a PowerShell task to install the Bot Framework CLI tools
    ```node
    npm install -g botdispatch @microsoft/botframework-cli
    ```

1. Create a PowerShell task to install BotSkills CLI tool:
    ```node
    npm install -g botskills@latest
    ```

1. Create an Azure PowerShell task to validate the resource group created in your Azure account
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

1. Create a PowerShell task to update the `cognitivemodels.json` file stored in the artifact.
    ```pwsh
    Set-Content -Path "$(System.DefaultWorkingDirectory)/$(Release.PrimaryArtifactSourceAlias)/drop/VATest/cognitivemodels.json" -Value '{
        "cognitiveModels": {
            "en-us": {
                "dispatchModel": {
                    "appid": "$(AppId)",
                    "authoringkey": "$(LuisAuthoringKey)",
                    "authoringRegion": "$(AuthoringRegion)",
                    "name": "$(BotName)_Dispatch",
                    "region": "$(Region)",
                    "subscriptionkey": "$(SubscriptionKey)",
                    "type": "dispatch"
                },
                "languageModels": [
                    {
                        "appId": "$(AppIdLanguageModels)",
                        "authoringkey": "$(LuisAuthoringKey)",
                        "authoringRegion": "$(AuthoringRegion)",
                        "id": "General",
                        "name": "$(BotName)_General",
                        "region": "$(Region)",
                        "subscriptionkey": "$(SubscriptionKey)",
                        "version": "0.1"
                    }
                ],
                "knowledgebases": [
                    {
                        "endpointKey": "$(EndpointKey)",
                        "hostname": "$(Hostname)",
                        "id": "Chitchat",
                        "kbId": "$(KbIdChitchat)",
                        "name": "Chitchat",
                        "subscriptionKey": "$(SubscriptionKeyKb)"
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

1. Create a PowerShell task to update the bot services executing the `update_cognitive_models.ps1` script
    ```pwsh
    pwsh.exe -ExecutionPolicy Bypass -File $(System.DefaultWorkingDirectory)/csharp-Virtual-Assistant-Sample/drop/VATest/Deployment/Scripts/update_cognitive_models.ps1
    ```

### YAML Example
Check the [yaml]({{site.repo}}/tree/master/build/yaml) folder present in the repository.

```yml
steps:
- powershell: |
   az login --user $(AzureUsername) --password $(AzurePassword) --tenant $(AzureTenant)
   
  pwsh: true
  displayName: 'Az login '

- powershell: |
   npm install -g botdispatch @microsoft/botframework-cli
   
  pwsh: true
  displayName: Commands

- powershell: |
   npm install -g botskills@latest
   
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
            "en-us": {
                "dispatchModel": {
                    "appid": "$(AppId)",
                    "authoringkey": "$(LuisAuthoringKey)",
                    "authoringRegion": "$(AuthoringRegion)",
                    "name": "$(BotName)_Dispatch",
                    "region": "$(Region)",
                    "subscriptionkey": "$(SubscriptionKey)",
                    "type": "dispatch"
                },
                "languageModels": [
                    {
                        "appId": "$(AppIdLanguageModels)",
                        "authoringkey": "$(LuisAuthoringKey)",
                        "authoringRegion": "$(AuthoringRegion)",
                        "id": "General",
                        "name": "$(BotName)_General",
                        "region": "$(Region)",
                        "subscriptionkey": "$(SubscriptionKey)",
                        "version": "0.1"
                    }
                ],
                "knowledgebases": [
                    {
                        "endpointKey": "$(EndpointKey)",
                        "hostname": "$(Hostname)",
                        "id": "Chitchat",
                        "kbId": "$(KbIdChitchat)",
                        "name": "Chitchat",
                        "subscriptionKey": "$(SubscriptionKeyKb)"
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