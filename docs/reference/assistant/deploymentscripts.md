# Deployment

## In this reference
- [Intro](#intro)
- [Resources](#resources)
- [Scripts](#scripts)
    - [deploy.ps1](#deploy.ps1)
    - [deploy_cognitive_models.ps1](#deploy_cognitive_models.ps1)
    - [update_cognitive_models.ps1](#update_cognitive_models.ps1)
    - [publish.ps1](#publish.ps1)
- [Common Questions](#common-questions)
    - [What services are deployed by the script?](#what-services-are-deployed-by-the-script)
    - [How do I reduce my Azure costs during development?](#how-do-I-reduce-my-azure-costs-during-development)
    - [How do I customize my Azure resource deployment?](#how-do-I-customize-my-azure-resource-deployment)
    - [How do I use my existing Azure resources from the same resource group?](#How-do-i-use-my-existing-Azure-resources-from-the-same-resource-group)
    - [How do I use my existing Azure resources from a different resource group?](#How-do-I-use-my-existing-Azure-resources-from-a-different-resource-group)

## Intro

A number of PowerShell scripts are provided in the Virtual Assistant Template to help deploy and configure your different resources. Please find details on each script's purpose, parameters, and outputs below.

## Resources
**LU** - this folder contains localized .lu files representing the basic LUIS models provided in the project.

**QnA** - this folder contains localized .lu files representing the basic knowledge models provided in the project. 

**template.json** - this file is the ARM template used to deploy the Azure Resources required by the project.

**parameters.template.json** - this file can be used to modify the default parameters in template.json for your specific implementation.

## Scripts

### deploy.ps1

This script orchestrates the deployment of all Azure Resources and Cognitive Models to get the Virtual Assistant running.

| Parameter | Description | Required? |
| --------- | ----------- | --------- |
| `name` | The name for your Azure resources. | Yes |
| `location` | The region for your Azure resource group and resources. | Yes |
| `appPassword` | The password for your Microsoft App Registration. If `-appId` is provided this should be the password for your existing Microsoft App Registration. Otherwise, a new registration will be created using this password. | Yes |
| `luisAuthoringRegion` | The region to deploy LUIS apps`| Yes |
| `luisAuthoringKey` | The authoring key for the LUIS portal. Must be valid key for `-luisAuthoringRegion`. | Yes |
| `resourceGroup` | The name for your Azure resource group. Default value is the name parameter. | No
| `appId` | The application Id for your Microsoft App Registration. | No |
| `parametersFile` | Optional configuration file for ARM Template deployment. | No |
| `languages` | Specifies which languages to deploy cognitive models in a comma separated string (e.g. "en-us,de-de,es-es"). Defaults to "en-us". | No |
| `projDir` | Location to save `appsettings.json` and `cognitivemodels.json` configuration files. Defaults to current directory. | No |
| `logFile` | Log file for any errors that occur during script execution. Defaults to `Deployment` folder | No |

### deploy_cognitive_models.ps1

This script deploys all the language models found in `Deployment\Resources\LU` and the knowledgebases found in `Deployment\Resources\QnA`. Finally it creates a Dispatch model to dispatch between all cognitive models.

| Parameter | Description | Required? |
| --------- | ----------- | --------- |
| `name` | The base name for all Cognitive Models. Model language and name will be appended. (e.g MyAssistanten_General )| Yes |
| `luisAuthoringRegion` | The region to deploy LUIS apps | Yes |
| `luisAuthoringKey` | The authoring key for the LUIS portal. Must be valid key for `-luisAuthoringRegion`. | Yes |
| `luisAccountName` | The LUIS service name from the Azure Portal. | Yes |
| `resourceGroup` | The resource group where the LUIS service is deployed  | Yes |
| `luisSubscriptionKey` | The LUIS service subscription key from the Azure Portal. | Yes |
| `luisAccountRegion` | The LUIS service region from the Azure Portal. | Yes |
| `qnaSubscriptionKey` | The subscription key for the QnA Maker service. Can be found in the Azure Portal. | Yes |
| `languages` | Specifies which languages to deploy cognitive models in a comma separated string (e.g. "en-us,de-de,es-es"). Defaults to "en-us". | No |
| `outFolder` | Location to save `cognitivemodels.json` configuration file. Defaults to current directory. | No |
| `logFile` | Log file for any errors that occur during script execution. Defaults to `Deployment` folder | No |

### update_cognitive_models.ps1

This script updates your hosted language models and knowledgebases based on local .lu files. Or, it can update your local .lu files based on your current models. Finally, it refreshes your dispatch model with the latest changes.

| Parameter | Description | Required? |
| --------- | ----------- | --------- |
| `RemoteToLocal` | Flag indicating that local files should be updated based on hosted models. Defaults to false. | No |
| `configFile` | The folder path to the cognitivemodels.json file. Defaults to current directory. | No |
| `dispatchFolder` | The folder path to the .dispatch file. Defaults to `Deployment\Resources\Dispatch` | No |
| `luisFolder` | The folder path to the .lu files for your LUIS models. Defaults to `Deployment\Resources\LU` | No |
| `qnaFolder` | The folder path to the .lu files for your QnA Maker knowledgebases. Defaults to `Deployment\Resources\QnA` | No |
| `lgOutFolder` | The folder path output LuisGen file for your Dispatch model. Defaults `.\Services` | No |
| `logFile` | Log file for any errors that occur during script execution. Defaults to `Deployment` folder | No |

### publish.ps1
This script builds and publishes your local project to your Azure.

| Parameter | Description | Required? |
| --------- | ----------- | --------- |
| `botWebAppName` | The name of the Azure Web App for deployment | Yes |
| `resourceGroup` |  The resource group for the Azure Web App | Yes |
| `projFolder` |  The project folder. Defaults to | No |

## Common Questions

### ❓ What services are deployed by the script?
The Virtual Assistant Template relies on a number of Azure resources to run. The included deployment scripts and ARM template use the following services:

Resource | Notes |
-------- | ----- |
Azure Bot Service | The Azure Bot Service resource stores configuration information that allows your Virtual Assistant to be accessed on the supported Channels and provide OAuth authentication. |
Azure Blob Storage | Used to store conversation transcripts.
Azure Cosmos DB | Used to store conversation state. |
Azure App Service Plan | Used to host your Bot Web App and QnA Maker Web App. |
Azure Application Insights | Used to capture conversation and application telemetry. [Available regions](https://azure.microsoft.com/en-us/global-infrastructure/services/?products=monitor)
Bot Web App | Hosts your Bot application.
Language Understanding | Subscription keys for Language Understanding Cognitive Service.
QnA Maker | Subscription keys for QnA Maker Cognitive Service. [Available regions](https://azure.microsoft.com/en-us/global-infrastructure/services/?products=cognitive-services)
QnA Maker Web App | Hosts your QnA Maker knowledgebases.
QnA Maker Azure Search Service | Search index for your QnA Maker knowledgebases. [Available regions](https://azure.microsoft.com/en-us/global-infrastructure/services/?products=search)
Content Moderator | Subscription keys for Content Moderator Cognitive Service.

### ❓ How do I reduce my Azure costs during development?
The default `parameters.template.json` file is configured to use all free service tiers to reduce the cost of testing. Provide this file in the `-parametersFile` parameter on the `deploy.ps1` script. **Note: There are service limits associated with free tiers (e.g. Azure Search permits only 1 free tier per subscription). Free tiers should only be used for development, not for production implementations.**

### ❓ How do I customize my Azure resources?
Any of the following parameters in the ARM template can be overridden with your preferred values using the `parameters.template.json` file provided in the `Deployment\Resources` folder:

| Parameters | Default Value |
| ---------- | ------------- |
| name | Resource group name |
| location   | Resource group region |
| suffix | Unique 7 digit string |
| microsoftAppId | N/A |
| microsoftAppPassword | N/A |
| cosmosDbName | [name]-[suffix] |
| storageAccountName | [name][suffix] |
| appServicePlanName | [name]-[suffix] |
| appServicePlanSku | S1 |
| appInsightsName | [name]-[suffix] |
| appInsightsLocation | Resource group location |
| botWebAppName | [name]-[suffix] |
| botServiceName | [name]-[suffix] |
| botServiceSku | S1 |
| contentModeratorName | [name]-cm-[suffix] |
| contentModeratorSku | S0 |
| contentModeratorLocation | Resource group location |
| luisServiceName | [name]-luis-[suffix] |
| luisServiceSku | S0 |
| luisServiceLocation | Resource group location |
| qnaMakerServiceName | [name]-qna-[suffix] |
| qnaMakerServiceSku | S0 |
| qnaServiceLocation | Resource group location |
| qnaMakerSearchName | [name]-search-[suffix] |
| qnaMakerSearchSku | Standard | |
| qnaMakerWebAppName | [name]-qnahost-[suffix] |

Simply update the parameters.template.json file with your preferred values, like so:

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "appInsightsLocation": {
      "value": "westus2"
    },
    "qnaMakerSearchSku": {
      "value": "basic"
    }
  }
}
```

Then provide the path to the file as an argument on the `deploy.ps1` script:

```
.\Deployment\Scripts\deploy.ps1 -parametersFile .\Deployment\Resources\parameters.template.json
```

### ❓ How do I use my existing Azure resources from the same resource group?
If you want to use existing resources from the same resource group, override the parameters for the services you want in the `parameters.template.json`. Provide this file in the `-parametersFile` parameter on the `deploy.ps1` script. 

#### parameters.template.json
```json
{
    "cosmosDbName": {
      "value": "MyCosmosDbName"
    },
}
```

### ❓ How do I use my existing Azure resources from a different resource group?
If you want to use an existing resource from a different resource group, follow these steps:

#### Cosmos DB
1. Provide the following parameter in the `parameters.template.json` file:
    ```json
    "useCosmosDb": {
        "value": false
    }
    ```
2. Update the following properties in `appsettings.json` with your service configuration from the [Azure Portal](https://portal.azure.com):
    ```json
    "cosmosDb": {
        "authkey": "",
        "cosmosDBEndpoint": "",
        "collectionId": "skillstate-collection",
        "databaseId": "botstate-db"
    }
    ```

#### Storage Account
1. Provide the following parameter in the `parameters.template.json` file:
    ```json
    "useStorage": {
        "value": false
    }
    ```
2. Update the following properties in `appsettings.json` with your service configuration from the [Azure Portal](https://portal.azure.com):
    ```json
    "blobStorage": {
        "connectionString": "",
        "container": "transcripts"
    },
    ```

#### Other services
1. Remove the resource from the `resources` array in `template.json`.
2. Provide the appropriate configuration in `appsettings.json` from the [Azure Portal](https://portal.azure.com).
