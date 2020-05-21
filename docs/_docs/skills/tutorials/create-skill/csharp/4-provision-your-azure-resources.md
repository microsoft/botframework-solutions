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
2. Run the following command:

    ```shell
    ./Deployment/Scripts/deploy.ps1
    ```
### What do these parameters mean?

    Parameter | Description | Required
    --------- | ----------- | --------
    `name` | **Unique** name for your bot. By default this name will be used as the base name for all your Azure Resources and must be unique across Azure so ensure you prefix with something unique and **not** *MyAssistant* | **Yes**
    `location` | The region for your Azure Resources. By default, this will be the location for all your Azure Resources | **Yes**
    `appPassword` | The password for the [Azure Active Directory App](https://ms.portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/Overview) that will be used by your bot for authentication purposes. It must be at least 16 characters long, contain at least 1 special character, and contain at least 1 numeric character. If using an existing app, this must be the existing password. | **Yes**
    `luisAuthoringRegion` | The authoring region for your LUIS account. Review the [LUIS regions](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation for more information. | **Yes**

You can find more detailed deployment steps including customization instructions in the [Deployment Scripts reference]({{site.baseurl}}/virtual-assistant/handbook/deployment-scripts/). 

> For manual deployment steps, refer to the [Deploy using CLI tools]({{site.baseurl}}/virtual-assistant/tutorials/deploy-assistant/cli/1-intro/) and [Deploy using web]({{site.baseurl}}/virtual-assistant/tutorials/deploy-assistant/web/1-intro/) tutorials.

