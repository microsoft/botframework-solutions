---
layout: tutorial
category: Skills
subcategory: Create
language: typescript
title: Provision your Azure resources
order: 4
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

The Virtual Assistant require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding

> Review the pricing and terms for the services and adjust to suit your scenario.

1. Run **PowerShell Core** (pwsh.exe) and **change directory to the project directory** of your assistant/skill.
2. Run the following command:

    ```shell
    ./Deployment/Scripts/deploy.ps1
    ```

    ### What do these parameters mean?

    Parameter | Description | Required
    --------- | ----------- | --------
    `name` | **Unique** name for your bot. By default this name will be used as the base name for all your Azure Resources and must be unique across Azure so ensure you prefix with something unique and **not** *MyAssistant* | **Yes**
    `location` | The region for your Azure Resources. By default, this will be the location for all your Azure Resources | **Yes**
    `appPassword` | The password for the [Azure Active Directory App](https://ms.portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/Overview) that will be used by your bot. It must be at least 16 characters long, contain at least 1 special character, and contain at least 1 numeric character. If using an existing app, this must be the existing password. | **Yes**
    `luisAuthoringRegion` | The authoring region for your LUIS account. Review the [LUIS regions](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation for more information. | **Yes**

You can find more detailed deployment steps including customization instructions in the [Deployment Scripts reference]({{site.baseurl}}/virtual-assistant/handbook/deployment-scripts/). 

> For manual deployment steps, refer to the [Deploy using CLI tools]({{site.baseurl}}/virtual-assistant/tutorials/deploy-assistant/cli/1-intro/) and [Deploy using web]({{site.baseurl}}/virtual-assistant/tutorials/deploy-assistant/web/1-intro/) tutorials.
