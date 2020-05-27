---
layout: tutorial
category: Skills
subcategory: Create
language: csharp
title: Provision your Azure resources
order: 4
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

The Skill Template requires the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding

> Review the pricing and terms for the services and adjust to suit your scenario.

To deploy your Skill using the Azure Resource Manager (ARM) template provided in the project template, follow these steps:

1. Open **PowerShell Core** (pwsh.exe)
1. Change to the **project directory** of your skill.
1. Run the following command to login to Azure:
    ```shell
    az login
    ```
1. If you have multiple subscriptions on your Azure account, [change the active subscription](https://docs.microsoft.com/en-us/cli/azure/manage-azure-subscriptions-azure-cli?view=azure-cli-latest#change-the-active-subscription) to the subscription you wish to deploy your Azure resources to.

1. Run the following command to deploy your Azure resources using the default settings:

    ```shell
    ./Deployment/Scripts/deploy.ps1
    ```
    
    Parameter | Description | Required
    --------- | ----------- | --------
    name | **Unique** name for your bot. By default this name will be used as the base name for all your Azure Resources. | **Yes**
    resourceGroup | The name for your Azure resource group. Default value is the name parameter. | **No**
    location | The region for your Azure resource group and default location for all Azure services unless otherwise specified in ARM template parameters. | **Yes**
    appId | The application ID for the Azure Active Directory App required by your bot registration. If not provided, a new app registration will be created. | **No**
    appPassword | The password for the [Azure Active Directory App](https://ms.portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/Overview) that will be used by your bot for authentication purposes. It must be at least 16 characters long, contain at least 1 special character, and contain at least 1 numeric character. If using an existing app, this must be the existing password. | **Yes**
    createLuisAuthoring | Indicates whether a new LUIS authoring resource should be created. If **false**, luisAuthoringKey parameter must be provided. | **Yes**
    luisAuthoringKey | Key for existing LUIS Authoring Key resource. No required if **createAuthoringResource** set to true. | **No**
    luisAuthoringRegion | The authoring region for your LUIS account. Review the [LUIS regions](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation for more information. | **Yes**
    parametersFile | Path to [ARM parameters file](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/parameter-files) for overriding default deployment template values. | **No**

You can find more detailed deployment steps including customization instructions in the [Deployment Scripts reference]({{site.baseurl}}/virtual-assistant/handbook/deployment-scripts/). 

> For manual deployment steps, refer to the [Deploy using CLI tools]({{site.baseurl}}/virtual-assistant/tutorials/deploy-assistant/cli/1-intro/) and [Deploy using web]({{site.baseurl}}/virtual-assistant/tutorials/deploy-assistant/web/1-intro/) tutorials.
