# Quickstart: Create your first Virtual Assistant (C#)

**APPLIES TO:** ✅ SDK v4

## In this tutorial
- [Intro](#intro)
- [Download and install](#download-and-install)
- [Create your assistant](#create-your-assistant)
- [Deploy your assistant](#deploy-your-assistant)
- [Run your assistant](#run-your-assistant)
- [Next Steps](#next-steps)

## Intro

### Purpose

Install Bot Framework development prerequisites and create your first Virtual Assistant.

### Prerequisites

[Download and install](#download-and-install) the Bot Framework development prerequisites.

- Retrieve your LUIS Authoring Key
  - Review the [LUIS regions](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation page for the correct LUIS portal for the region you plan to deploy to. Note that www.luis.ai refers to the US region and an authoring key retrieved from this portal will not work within a Europe deployment.
  - Once signed in replace your name in the top right hand corner.
  - Choose Settings and make a note of the Authoring Key for the next step.

### Time to Complete

10 minutes

### Scenario

A Virtual Assistant app (in C#) that greets a new user.

## Download and install

> It's important to ensure all of the following prerequisites are installed on your machine prior to attempting deployment otherwise you may run into deployment issues.

1. Download and install Visual Studio (2017 or 2019) for PC or Mac
1. Download and install the [Virtual Assistant Template](https://marketplace.visualstudio.com/items?itemName=BotBuilder.VirtualAssistantTemplate). *Note that Visual Studio on Mac doesn't support VSIX packages, instead [clone the Virtual Assistant sample from our repository](https://github.com/microsoft/botframework-solutions/tree/master/templates/Virtual-Assistant-Template/csharp/Sample).*
2. Ensure you have updated [.NET Core](https://www.microsoft.com/net/download) to the **latest** version.  
3. Download and install [Node Package manager](https://nodejs.org/en/).
4. Download and install PowerShell Core version 6 (required for cross platform deployment support):
   * [Download PowerShell Core on Windows](https://aka.ms/getps6-windows)
   * [Download PowerShell Core on MacOS](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-macos?view=powershell-6)
   * [Download PowerShell Core on Linux](https://aka.ms/getps6-linux)
5. Download and install the Bot Framework (CLI) tool dependencies. It's important to do this even if you have earlier versions as the Virtual Assistant makes use of the latest capabilities:

   ```
   npm install -g botdispatch ludown luis-apis qnamaker luisgen@2.0.2
   ```
6. Install Botskills (CLI) tool:
   
   ```
   npm install -g botskills
   ```

7. Download and install the [Azure Command Line Tools (CLI)](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest) **(Version 2.0.64 minimum required)**.
8. Download and install the [Bot Framework Emulator](https://aka.ms/botframework-emulator).

## Create your assistant

1. In Visual Studio, replace **File > New Project**.
2. Under Bot, select **Virtual Assistant Template**.
3. Name your project and select **Create**.
4. Build your project to restore the NuGet packages.

You now have your own Virtual Assistant! Before trying to run your assistant locally, continue with the deployment steps (it creates vital dependencies requires to run correctly).

## Deploy your assistant

The Virtual Assistant requires the following Azure dependencies to run correctly. These are created through an [ARM (Azure Resource Manager)](https://azure.microsoft.com/en-us/features/resource-manager/) script (you can modify this to meet your requirements).

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding
- Azure Cognitive Services - QnAMaker (including Azure Search, Azure Web App)

> Review the pricing and terms for the services and adjust to suit your scenario.

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
    `luisAuthoringKey` | The authoring key for your LUIS account. It can be found at https://www.luis.ai/user/settings,  https://eu.luis.ai/user/settings, or https://au.luis.ai/user/settings | **Yes**
    `luisAuthoringRegion` | The authoring region of your LUIC account. It can be found at https://www.luis.ai/user/settings,  https://eu.luis.ai/user/settings, or https://au.luis.ai/user/settings | **Yes**

You can find more detailed deployment steps including customization in the [Virtual Assistant and Skill Template deployment](/docs/tutorials/assistantandskilldeploymentsteps.md) page.

## Run your assistant

When deployment is complete, you can run your Virtual Assistant debugging through the following steps:

1. Press **F5** within Visual Studio to run your assistant.
2. Run the **Bot Framework Emulator**.
3. Select **Open Bot**.

  <p align="center">
  <img src="../../media/quickstart-virtualassistant-openbot.png" width="600">
  </p>

4. Populate the fields in the **Open a Bot** modal with your bot's configuration settings. Provide the endpoint of your running bot, e.g: `http://localhost:3978/api/messages`. Provide the AppId and Secret values. Find these in your `appsettings.json` file, under the `microsoftAppId` and `microsoftAppPassword` configuration settings.

  <p align="center">
  <img src="../../media/quickstart-virtualassistant-openbotmodal.png" width="600">
  </p>

5. Congratulations, you've built and run your first Virtual Assistant!

<p align="center">
<img src="../../media/quickstart-virtualassistant-greetingemulator.png" width="600">
</p>

## Next Steps

Now that you've got the basics, continue [customizing your Virtual Assistant](/docs/tutorials/csharp/customizeassistant.md).
