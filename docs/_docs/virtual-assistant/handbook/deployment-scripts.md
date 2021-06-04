---
category: Virtual Assistant
subcategory: Handbook
title: Deployment Scripts
description: Reference for deployment tools provided in the Virtual Assistant Template.
order: 1
toc: true
---

# {{ page.title }}
{:.no_toc}

A number of PowerShell scripts are provided in the Virtual Assistant Template to help deploy and configure your different resources. Please find details on each script's purpose, parameters, and outputs below.

## Resources
**LU** - this folder contains localized `.lu` files representing the basic LUIS models provided in the project.

**QnA** - this folder contains localized `.qna` files representing the basic knowledge bases provided in the project. 

**template.json** - this file is the [ARM template](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/overview) used to deploy the Azure Resources required by the project.

**parameters.template.json** - this file can be used to modify the default parameters in `template.json` for your specific implementation.

## Scripts

### deploy.ps1
{:.no_toc}

This script orchestrates the deployment of all Azure Resources and Cognitive Models to get the Virtual Assistant running.

| Parameter | Description | Required? |
| --------- | ----------- | --------- |
| name | The name for your Azure resources. | Yes |
| resourceGroup | The name for your Azure resource group. Default value is the name parameter. | No
| location | The region for your Azure resource group and resources. | Yes |
| appId | The application Id for your Microsoft App Registration. | No |
| appPassword | The password for your Microsoft App Registration. If **appId** is provided this should be the password for your existing Microsoft App Registration. Otherwise, a new registration will be created using this password. | Yes |
| parametersFile | Optional configuration file for ARM Template deployment. | No |
| createLuisAuthoring | Indicates whether a new LUIS authoring resource should be created. If **false**, luisAuthoringKey and luisEndpoint parameters must be provided. | Yes |
| luisAuthoringKey | The authoring key for the LUIS portal. Must be valid key for **luisAuthoringRegion**| No |
| luisAuthoringRegion | The region to deploy LUIS apps. | Yes |
| armLuisAuthoringRegion | The region to deploy LUIS authoring resource in Azure (**only required for Azure Gov deployments**) | No |
| luisEndpoint | The LUIS endpoint for deploying and managing LUIS applications. Required if **createLuisAuthoring** is set to false. | No |
| useGov | Flag indicating if the deployment is targeting the Azure Government Cloud. Defaults to **false**.| No |
| qnaEndpoint | Endpoint for deploying QnA Maker knowledge bases (**only required for Azure Gov deployments. See note below for more information.**). | No |
| languages | Specifies which languages to deploy cognitive models in a comma separated string (e.g. "en-us,de-de,es-es"). Defaults to "en-us". | No |
| projDir | Location to save **appsettings.json** and **cognitivemodels.json** configuration files. Defaults to current directory. | No |
| logFile | Log file for any errors that occur during script execution. Defaults to **Deployment** folder with the name of `deploy_log.txt`.| No |

> Note: QnA Maker requires three Azure resources, a QnA Maker Cognitive Service subscription, an Azure Search resource, and an Azure web app. The Cognitive Service subscription can only be deployed in West US for Azure Commercial deployments, therefore the QnA Maker endpoint will be the same for all regions unless the service is being deployed for Azure Government.

### deploy_cognitive_models.ps1
{:.no_toc}

This script deploys all the language models found in **Deployment/Resources/LU** and the knowledgebases found in **Deployment/Resources/QnA**. Finally it creates a Dispatch model to dispatch between all cognitive models.

| Parameter | Description | Required? |
| --------- | ----------- | --------- |
| name | The base name for all Cognitive Models. Model language and name will be appended. (e.g MyAssistanten_General)| Yes |
| luisAuthoringRegion | The region to deploy LUIS apps | Yes |
| luisAuthoringKey | The authoring key for the LUIS portal. Must be valid key for **luisAuthoringRegion**. | Yes |
| luisAccountName | The LUIS service name from the Azure Portal. | Yes |
| luisAccountRegion | The LUIS service region from the Azure Portal. | Yes |
| luisSubscriptionKey | The LUIS service subscription key from the Azure Portal. | Yes |
| luisEndpoint | The LUIS endpoint for deploying and managing LUIS apps. | Yes |
| resourceGroup | The resource group where the LUIS service is deployed  | Yes |
| qnaSubscriptionKey | The subscription key for the QnA Maker service. Can be found in the Azure Portal. | Yes |
| qnaEndpoint | The QnA Maker endpoint for deploying and managing QnA Maker knowledge bases. Defaults to _https://westus.api.cognitive.microsoft.com/qnamaker/v4.0_ | No |
| useGov | Flag indicating whether the deployment is targeting the Azure Government Cloud. | No |
| useDispatch | Flag indicating whether a Dispatch model should be created based on the deployed LUIS apps and QnA Maker knowledge bases. | No |
| languages | Specifies which languages to deploy cognitive models in a comma separated string (e.g. "en-us,de-de,es-es"). Defaults to "en-us". | No |
| outFolder | Location to save **cognitivemodels.json** configuration file. Defaults to current directory. | No |
| logFile | Log file for any errors that occur during script execution. Defaults to **Deployment** folder with the name of `deploy_cognitive_models_log.txt`. | No |
| excludedKbFromDispatch | QnA Maker knowledge bases included in this list will be deployed but not added to the Dispatch model. | No |


### update_cognitive_models.ps1
{:.no_toc}

This script updates your hosted language models and knowledgebases based on local `.lu` files. Or, it can update your local `.lu` files based on your current hosted models. Finally, it refreshes your dispatch model with the latest changes.

| Parameter | Description | Required? |
| --------- | ----------- | --------- |
| RemoteToLocal | Flag indicating that local files should be updated based on hosted models. Defaults to **false**. | No |
| useGov | Flag indicating that cognitive models are deployed in Azure Government Cloud. | No |
| useLuisGen | Flag indicating that LUIS Generation files should be updated for the LUIS and Dispatch models. Defaults to **true**. | No |
| configFile | The folder path to the `cognitivemodels.json` file. Defaults to current directory. | No |
| dispatchFolder | The folder path to the `.dispatch` file. Defaults to **Deployment/Resources/Dispatch** | No |
| luisFolder | The folder path to the `.lu` files for your LUIS models. Defaults to **Deployment/Resources/LU** | No |
| qnaFolder | The folder path to the `.qna` files for your QnA Maker knowledgebases. Defaults to **Deployment/Resources/QnA** | No |
| qnaEndpoint | The QnA Maker endpoint for deploying and managing QnA Maker knowledge bases. | No |
| lgOutFolder | The folder path output LuisGen file for your Dispatch model. Defaults to **Services** folder | No |
| logFile | Log file for any errors that occur during script execution. Defaults to **Deployment** folder with the name of `update_cognitive_models_log.txt`. | No |
| excludedKbFromDispatch | QnA Maker knowledge bases included in this list will be deployed but not added to the Dispatch model. | No |


### publish.ps1
{:.no_toc}

This script builds and publishes your local project to your Azure.

| Parameter | Description | Required? |
| --------- | ----------- | --------- |
| name | The name of the Azure Web App Bot for deployment | Yes |
| resourceGroup | The resource group for the Azure Web App Bot | Yes |
| projFolder | The project folder. Defaults to current directory | No |
| logFile | Log file for any errors that occur during script execution. Defaults to **Deployment** folder with the name of `publish_log.txt`. | No |

# Frequently asked questions

## What services are deployed by the script?
{:.no_toc}

The Virtual Assistant Template relies on a number of Azure resources to run. The included deployment scripts and ARM template use the following services:

Resource | Notes |
-------- | ----- |
Azure Bot Service | The Azure Bot Service resource stores configuration information that allows your Virtual Assistant to be accessed on the supported Channels and provide OAuth authentication. |
Azure Blob Storage | Used to store conversation transcripts. |
Azure Cosmos DB | Used to store conversation state. |
Azure App Service Plan | Used to host your Web App Bot and QnA Maker Web App. |
Azure Application Insights | Used to capture conversation and application telemetry. [Available regions](https://azure.microsoft.com/en-us/global-infrastructure/services/?products=monitor)
Web App Bot | Hosts your Bot application.
Language Understanding | Subscription keys for Language Understanding Cognitive Service.
QnA Maker | Subscription keys for QnA Maker Cognitive Service. [Available regions](https://azure.microsoft.com/en-us/global-infrastructure/services/?products=cognitive-services)
QnA Maker Web App | Hosts your QnA Maker knowledgebases.
QnA Maker Azure Search Service | Search index for your QnA Maker knowledgebases. [Available regions](https://azure.microsoft.com/en-us/global-infrastructure/services/?products=search)
Content Moderator | Subscription keys for Content Moderator Cognitive Service.

## How do I reduce my Azure costs during development?
{:.no_toc}

The default `parameters.template.json` file is configured to use all free service tiers to reduce the cost of testing. Provide this file in the `-parametersFile` parameter on the `deploy.ps1` script.

> There are service limits associated with free tiers (e.g. Azure Search permits only 1 free tier per subscription). Free tiers should only be used for development, not for production implementations.

## How do I customize my Azure resource deployment?
{:.no_toc}

Any of the following parameters in the ARM template can be overridden with your preferred values using the `parameters.template.json` file provided in the **Deployment/Resources** folder:

| Parameters | Default Value |
| ---------- | ------------- |
| name | Resource group name |
| location | Resource group location |
| suffix | Unique 7 digit string |
| microsoftAppId | N/A |
| microsoftAppPassword | N/A |
| useCosmosDb | True |
| cosmosDbName | [name]-[suffix] |
| cosmosDbDatabaseName | "botstate-db" |
| cosmosDbDatabaseThroughput | 400 |
| useStorage | True |
| storageAccountName | [name]-[suffix] |
| appServicePlanName | [name]-[suffix] |
| appServicePlanSku | S1 |
| appInsightsName | [name]-[suffix] |
| appInsightsLocation | Resource group location |
| botWebAppName | [name]-[suffix] |
| botServiceName | [name]-[suffix] |
| botServiceSku | S1 |
| usecontentModerator | True |
| contentModeratorName | [name]-cm-[suffix] |
| contentModeratorSku | S0 |
| contentModeratorLocation | Resource group location |
| luisPredictionName | [name]-luisprediction-[suffix] |
| luisPredictionSku | S0 |
| luisPredictionLocation | Resource group location |
| useLuisAuthoring | True |
| luisAuthoringName | [name]-luisauthoring-[suffix] |
| luisAuthoringSku | F0 |
| luisAuthoringLocation | N/A |
| qnaMakerServiceName | [name]-qna-[suffix] |
| qnaMakerServiceSku | S0 |
| qnaMakerServiceLocation | Resource group location |
| qnaMakerSearchName | [name]-search-[suffix] |
| qnaMakerSearchSku | Standard |
| qnaMakerSearchLocation | Resource group location |
| qnaMakerWebAppName | [name]-qnahost-[suffix] |
| qnaMakerWebAppLocation | Resource group location |
| resourceTagName | "bot" |
| resourceTagValue | [name]-[suffix] |

Simply update the `parameters.template.json` file with your preferred values, like so:

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

Then provide the path to the file as an argument on the **deploy.ps1** script:

```
./Deployment/Scripts/deploy.ps1 -parametersFile ./Deployment/Resources/parameters.template.json
```

## How do I use my existing Azure resources from the same resource group?
{:.no_toc}

If you want to use existing resources from the same resource group, override the parameters for the services you want in the `parameters.template.json`. Provide this file in the **parametersFile** parameter on the `deploy.ps1` script. 

### parameters.template.json
{:.no_toc}
```json
{
    "cosmosDbName": {
      "value": "MyCosmosDbName"
    },
}
```

## How do I use my existing Azure resources from a different resource group?
{:.no_toc}

If you want to use an existing resource from a different resource group, follow these steps:

### Cosmos DB
{:.no_toc}
1. Provide the following parameter in the `parameters.template.json` file:
    ```json
    "useCosmosDb": {
        "value": false
    }
    ```
1. Update the following properties in `appsettings.json` with your service configuration from the [Azure Portal](https://portal.azure.com):
    ```json
    "cosmosDb": {
        "authkey": "",
        "cosmosDBEndpoint": "",
        "containerId": "skillstate-collection",
        "databaseId": "botstate-db"
    }
    ```

### Storage Account
{:.no_toc}
1. Provide the following parameter in the `parameters.template.json` file:
    ```json
    "useStorage": {
        "value": false
    }
    ```
1. Update the following properties in `appsettings.json` with your service configuration from the [Azure Portal](https://portal.azure.com):
    ```json
    "blobStorage": {
        "connectionString": "",
        "container": "transcripts"
    },
    ```

### Other services
{:.no_toc}
1. Remove the resource from the **resources** array in `template.json`.
1. Provide the appropriate configuration in `appsettings.json` from the [Azure Portal](https://portal.azure.com).

## How do I update my local deployment scripts with the latest?
{:.no_toc}
Once you have created your Virtual Assistant or Skill projects using the various templates and generators, you may need to update the deployment scripts to reflect ongoing changes to these scripts over time. 

### Sample Project
{:.no_toc}

For each of the template types we provide a sample project which is generated by the most recent template. This enables you to easily retrieve changes such as the deployment scripts. Alternatively you can clone the repro and use these sample projects as your starting point.

See the table below for a direct link to the appropriate sample project for your scenario:

Name | Language | Sample Project Location | Deployment Scripts Folder |
-------- | ---- | ----- | ----- 
Virtual Assistant | C# | [Sample Project Location](https://github.com/microsoft/botframework-solutions/tree/master/samples/csharp/assistants/virtual-assistant) | [Deployment Scripts](https://github.com/microsoft/botframework-solutions/tree/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Deployment)
Virtual Assistant | TypeScript | [Sample Project Location](https://github.com/microsoft/botframework-solutions/tree/master/templates/typescript/samples/sample-assistant) | [Deployment Scripts](https://github.com/microsoft/botframework-solutions/tree/master/templates/typescript/samples/sample-assistant/deployment)
Skill | C# | [Sample Project Location](https://github.com/microsoft/botframework-solutions/tree/master/samples/csharp/skill) | [Deployment Scripts](https://github.com/microsoft/botframework-solutions/tree/master/samples/csharp/skill/SkillSample/Deployment)
Skill | TypeScript | [Sample Project Location](https://github.com/microsoft/botframework-solutions/tree/master/templates/typescript/samples/sample-skill) | [Deployment Scripts](https://github.com/microsoft/botframework-solutions/tree/master/templates/typescript/samples/sample-skill/deployment)

### Updating your deployment scripts
{:.no_toc}

GitHub doesn't provide the ability to download folders or files interactively in the Web Browser. You must therefore clone the [Bot Framework Solutions repo]({{site.repo}}) onto your machine.

1. Clone the repo locally onto your machine 
1. Browse to the appropriate deployment scripts folder using the table above as a reference to the location
1. Copy the entire contents of the **Deployment** folder (resources and script subdirectories) over the files in the **Deployment** folder of your Assistant or Skill project.

You now have the latest scripts for Assistant/Skill deployment and the latest cognitive models.

### Skills
{:.no_toc}

Skills are part of the [Bot Framework Skills repo](https://github.com/microsoft/botframework-skills), so any changes to the deployment scripts will be reflected automatically when you pull the latest changes of that repository.


## How do I use my existing cognitive models (LUIS and/or QnA Maker) with a Virtual Assistant project?

If you would like to use an existing LUIS app or QnA Maker knowledge base with a Virtual Assistant project, please refer to the following steps.

### Use an existing QnA Maker knowledge base

If you have an existing QnA Maker knowledge base that you want to use in your Virtual Assistant project, follow these steps:

1. Add your knowledge base configuration in `cognitivemodels.json`
    ```json
    "knowledgebases": [
      {
        "id": "mykb",
        "name": "<your-knowledge-base-name>",
        "kbId": "<your-knowledge-base-id>",
        "endpointKey": "<your-endpoint-key>",
        "hostname": "https://<your-qna-host>.azurewebsites.net/qnamaker",
        "subscriptionKey": ""
      }
    ]
    ```

    `kbId`, `endpointKey`, and `hostname` can be found in the Publish tab of the QnA Maker portal:

        POST /knowledgebases/<kbId>/generateAnswer
        Host: <hostname>
        Authorization: EndpointKey <endpointKey>
        Content-Type: application/json
        {"question":"<Your question>"}

1. Run the following command from your project directory to import the `.qna` schema of your hosted knowledge base and update your local Dispatch model and `DispatchLuis.cs` file:
    ```
    .\Deployment\Scripts\update_cognitive_model.ps1 -RemoteToLocal
    ```

1. Access your knowledge base in a Dialog using the following code (where "knowledgebase-id" is the `id` property from your `cognitivemodels.json` file):
    ```csharp
    var qnaDialog = TryCreateQnADialog("knowledgebase-id", localizedServices);
    if (qnaDialog != null)
    {
        Dialogs.Add(qnaDialog);
    }

    return await stepContext.BeginDialogAsync(knowledgebaseId, cancellationToken: cancellationToken);
    ```

### Use an existing LUIS model
If you have an existing LUIS application that you want to use in your Virtual Assistant project, follow these steps:

1. Add your LUIS app configuration in `cognitivemodels.json`:
    ```json
    "languageModels": [
        {
          "id": "MyLuisApp",
          "name": "<your-luis-app-name>",
          "appId": "<your-luis-app-id>",
          "endpoint": "<your-luis-endpoint>",
          "authoringkey": "<your-luis-authoring-key>",
          "subscriptionKey": "<your-luis-subscription-key>",
          "region": "<your-luis-region>",
          "version": "0.1"
        }
      ],
    ```

    Each of the above properties can be found in the following locations:
     - Luis application name 
        - Navigate to the LUIS portal for your region (e.g. www.luis.ai for West US region)
        - Open the **Manage > Settings** tab
        - Copy the **App name** property
     - Luis application ID
        - Navigate to the LUIS portal for your region (e.g. www.luis.ai for West US region)
        - Open the **Manage > Settings** tab
        - Copy the **App ID** property
     - Luis endpoint
        - Navigate to the LUIS portal for your region (e.g. www.luis.ai for West US region)
        - Open the **Manage > Azure Resources > Authoring Resource** tab
        - For the assigned prediction resource, copy the **Endpoint URL** property
     - Luis authoring key
        - Navigate to the LUIS portal for your region (e.g. www.luis.ai for West US region)
        - Open the **Manage > Azure Resources > Authoring Resource** tab
        - For the assigned authoring resource, copy the **Primary Key** property
     - Luis subscription key
        - Navigate to the LUIS portal for your region (e.g. www.luis.ai for West US region)
        - Open the **Manage > Azure Resources > Authoring Resource** tab
        - For the assigned prediction resource, copy the **Primary Key** property
     - Luis region
        - Navigate to the LUIS portal for your region (e.g. www.luis.ai for West US region)
        - Open the **Manage > Azure Resources > Authoring Resource** tab
        - For the assigned authoring resource, copy the **Location** property

  1. Run the following command from your project directory to import the `.lu` schema of your hosted LUIS model and update your local Dispatch model and `DispatchLuis.cs` file:
      ```
      .\Deployment\Scripts\update_cognitive_model.ps1 -RemoteToLocal
      ```

  1. Access your LUIS model in a Dialog using the following code (where "luis-app-id" is the `id` property from your `cognitivemodels.json` file and `YourLUIS.cs` is the LUIS generation class created for your application):
      ```csharp
      // Get cognitive models for the current locale.
      var localizedServices = _services.GetCognitiveModels();

      // Run LUIS recognition on General model and store result in turn state.
      var luisResult = await localizedServices.LuisServices["luis-app-id"].RecognizeAsync<YourLUIS.cs>(innerDc.Context, cancellationToken);
      ```

## How do I add support for additional languages to my existing Virtual Assistant?
If you would like to add support for additional languages to your existing Virtual Assistant, please refer to the following steps.

1. Run the following command from your project directory. Replace "locale" with one or more of the supported language codes (en-us, it-it, de-de, es-es, fr-fr, or zh-cn). The values for the remaining parameters can be found in appsettings.json after Virtual Assistant deployment.

    ```
      .\Deployment\Scripts\deploy_cognitive_models.ps1 `
        -languages "locale" `
        -name 'base-name-of-luis-model' `
        -resourceGroup 'resouce-group-for-luis-resource' `
        -luisAuthoringRegion 'luis-authoring-region' `
        -luisAuthoringKey 'luis-authoring-key' `
        -luisAccountName 'luis-account-name' `
        -luisAccountRegion 'luis-account-region' `
        -luisSubscriptionKey 'luis-subscription-key' `
        -luisEndpoint 'luis-endpoint' `
        -qnaSubscriptionKey 'qna-subscription-key'
    ```