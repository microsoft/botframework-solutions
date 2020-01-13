---
layout: tutorial
category: Virtual Assistant
subcategory: Create
language: csharp
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
2. Change to the **project directory** of your assistant.
1. Run the following command to login to Azure:
    ```shell
    az login
    ```

1. Run the following command to deploy your Azure resources using the default settings:

    ```shell
    ./Deployment/Scripts/deploy.ps1
    ```
    
    Parameter | Description | Required
    --------- | ----------- | --------
    name | **Unique** name for your bot. By default this name will be used as the base name for all your Azure Resources. | **Yes**
    location | The region for your Azure resource group. By default, this will be the location for all your Azure Resources. | **Yes**
    appPassword | The password for the [Azure Active Directory App](https://ms.portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/Overview) that will be used by your bot. It must be at least 16 characters long, contain at least 1 special character, and contain at least 1 numeric character. If using an existing app, this must be the existing password. | **Yes**
    `luisAuthoringRegion` | The authoring region for your LUIS account. Review the [LUIS regions](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation for more information. | **Yes**

You can find more detailed deployment steps including customization instructions in the [Deployment Scripts reference]({{site.baseurl}}/virtual-assistant/handbook/deployment-scripts/). 

> For manual deployment steps, refer to the [Deploy using CLI tools]({{site.baseurl}}/virtual-assistant/tutorials/deploy-assistant/cli/1-intro/) and [Deploy using web]({{site.baseurl}}/virtual-assistant/tutorials/deploy-assistant/web/1-intro/) tutorials.
