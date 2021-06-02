---
layout: tutorial
category: Virtual Assistant
subcategory: Create
language: typescript
title: Provision your Azure resources
order: 4
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Deploy your assistant

The Virtual Assistant requires the following Azure dependencies to run correctly:

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Language Understanding
- QnA Maker (including Azure Search, Azure Web App)

To deploy your Assistant using the Azure Resource Manager (ARM) template provided in the project template, follow these steps:

1. Open **PowerShell Core** (pwsh.exe)
1. Change to the **project directory** of your assistant.
1. Run the following command to login to Azure:
    ```shell
    az login
    ```
1. If you have multiple subscriptions on your Azure account, [change the active subscription](https://docs.microsoft.com/en-us/cli/azure/manage-azure-subscriptions-azure-cli?view=azure-cli-latest#change-the-active-subscription) to the subscription you wish to deploy your Azure resources to.

1. Run the following command to deploy your Azure resources using the default settings:

    ```shell
    ./Deployment/Scripts/deploy.ps1
    ```
    
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
    | logFile | Log file for any errors that occur during script execution. Defaults to **Deployment** folder | No |


You can find more detailed deployment steps including customization instructions in the [Deployment Scripts reference]({{site.baseurl}}/virtual-assistant/handbook/deployment-scripts/). 

> For manual deployment steps, refer to the [Deploy using CLI tools]({{site.baseurl}}/virtual-assistant/tutorials/deploy-assistant/cli/1-intro/) and [Deploy using web]({{site.baseurl}}/virtual-assistant/tutorials/deploy-assistant/web/1-intro/) tutorials.
