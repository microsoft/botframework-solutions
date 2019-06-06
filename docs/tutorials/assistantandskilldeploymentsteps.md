# Deploy your Virtual Assistant or Skill

In this tutorial
- [Intro](#intro)
- [Deploy your bot](#deploy-your-bot)
- [Known issues](#known-issues)

## Intro
### Prerequisites
Create a Virtual Assistant or a Bot Framework Skill

> Applies to Virtual Assistant Template, Skill Template (C# and TypeScript) and provided Skills

## Deploy your bot

1. Run **PowerShell Core** (pwsh.exe) and **change directory to the project directory** of your assistant/skill.
2. Run the following command:

    ```shell
    .\Deployment\Scripts\deploy.ps1
    ```

    ### What do these parameters mean?

    Parameter | Description | Required
    --------- | ----------- | --------
    `name` | **Unique** name for your bot. By default this name will be used as the base name for all your Azure Resources and must be unique across Azure so ensure you prefix with something unique and **not** *MyAssistant* | **Yes**
    `location` | The region for your Azure Resources. By default, this will be the location for all your Azure Resources | **Yes**
    `appPassword` | The password for the [Azure Active Directory App](https://ms.portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/Overview) that will be used by your bot. It must be at least 16 characters long, contain at least 1 special character, and contain at least 1 numeric character. If using an existing app, this must be the existing password. | **Yes**
    `luisAuthoringKey` | The authoring key for your LUIS account. It can be found at https://www.luis.ai/user/settings or https://eu.luis.ai/user/settings | **Yes**
    `resourceGroup` | Name of the Azure Resource Group. Default value is name parameter. | No
    `appId` | The appId of an existing MSA App Registration. If left blank, a new app will be provisioned automatically. | No
    `parametersFile` | A .json file that can overwrite the default values of the Azure Resource Manager template. | No
    `outFolder` | Output directory for created appsettings.json and cognitivemodels.json files. Default value is current directory. | No
    `languages` | Specifies which languages to deploy cognitive models (Dispatch,LUIS/QNA) in a comma separated string (e.g. `en-us,de-de,es-es,zh-zh`). Defaults to `en-us`.| No

### Customize deployment using the parameters file

If you wish to deploy your Virtual Assistant to a region that doesn't have all of the supporting resources (e.g. Azure Search, QnAMaker, Application Insights) available in the region you need to customize deployment using the provided parameters file to override region for those services that aren't available. See the [Customizing deployment](/docs/howto/assistant/customizedeployment.md) section for more information.

Additionallly you can use the same parameters file to control the pricing tiers for each service. The default configuration doesn't use free tiers (where available) as the numbers of these are limited per Azure subscription resulting in frequent deployment failures.

### Create your own ARM template

We have provided a comprehensive ARM template to deploy all required capabilities which can be customised through the parameters file detailed in the previous section. If however you want to make more substantial changes - such as re-using existing deployed services please refer to the [Azure Resource Manager template documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-authoring-templates) for guidance and you can refer to our template for reference.

## Known Issues
### My Microsoft App Registration could not be automatically provisioned

There is a known issue with some users whereby you might experience the following error when running deployment `Could not provision Microsoft App Registration automatically. Please provide the -appId and -appPassword arguments for an existing app and try again`. In this situation, create your own Azure Active Directory App through [this](https://ms.portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredAppsPreview) part of the Azure portal.

- Click `New Registration`
- Provide a name
- Select `Accounts in any organizational directory`

Once created, retrieve the `Application (ClientId)` and create a new client secret on the `Certificates & secrets` pane

Run the above deployment script again but provide two new arguments `appId` and `appPassword` passing the values you've just retrieved.

> NOTE: Take special care when providing the appSecret step above as special characters (e.g. @) can cause parse issues. Ensure you wrap these parameters in single quotes.
