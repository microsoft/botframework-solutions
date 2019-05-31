# Create a new Bot Framework Skill (C#)

**APPLIES TO:** ✅ SDK v4

## In this tutorial
- [Intro](#intro)
- [Download and install](#download-and-install)
- [Create your Skill](#create-your-skill)
- [Deploy your Skill](#deploy-your-skill)
- [Test your Skill](#test-your-skill)
- [Update your Skill manifest](#update-your-skill-manifest)
- [Publish your Skill](#publish-your-skill)
- [Validate the Skill manifest endpoint](#validate-the-skill-manifest-endpoint)
- [Adding your Skill to an assistant](#adding-your-skill-to-an-assistant)
- [Testing your Skill](#testing-your-skill)

## Intro

### Purpose

Install Bot Framework development prerequisites and create a Skill using the Bot Framework Skill Template.

### Prerequisites

If you haven't [created a Virtual Assistant](./virtualassistant.md), [download and install](#download-and-install) the Bot Framework development prerequisites.

- Retrieve your LUIS Authoring Key
  - Review the [LUIS regions](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation page for the correct LUIS portal for the region you plan to deploy to. Note that www.luis.ai refers to the US region and an authoring key retrieved from this portal will not work within a Europe deployment.
  - Once signed in replace your name in the top right hand corner.
  - Choose Settings and make a note of the Authoring Key for the next step.

### Time to Complete

20 minutes

### Scenario

A Bot Framework Skill app (in C#) that greets a new user.

## Download and install

> It's important to ensure all of the following pre-requisites are installed on your machine prior to attempting deployment otherwise you may run into deployment issues.

1. Install the [Skill Template](https://marketplace.visualstudio.com/items?itemName=BotBuilder.BotSkillTemplate)
2. Ensure you have updated [.NET Core](https://www.microsoft.com/net/download) to the latest version.  
3. Ensure the [Node Package manager](https://nodejs.org/en/) is installed.
4. PowerShell Core version 6 (Required for cross platform deployment support)
   * [Download PowerShell Core on Windows](https://aka.ms/getps6-windows)
   * [Download PowerShell Core on macOS and Linux](https://aka.ms/getps6-linux)
5. Install  Bot Framework (CLI) tool dependencies. It's important to do this even if you have earlier versions as we make use of the latest capabilities:

   ```
   npm install -g botdispatch ludown luis-apis qnamaker luisgen@2.0.2 botskills
   ```

6. Install the [Azure Command Line Tools (CLI)](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)

## Create your Skill

1. In Visual Studio, click **File > New Project**.
2. Under Bot, select **Skill Template**.
3. Name your project and click **Create**.
4. Build your project to restore your NuGet packages.

You now have your new Skill! Follow the Deployment steps below before you try and run the project as deployment creates key dependencies required for operation.

## Deploy your Skill

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
    .\Deployment\Scripts\deploy.ps1
    ```

    ### What do these parameters mean?

    Parameter | Description | Required
    --------- | ----------- | --------
    `name` | **Unique** name for your bot. By default this name will be used as the base name for all your Azure Resources and must be unique across Azure so ensure you prefix with something unique and **not** *MyAssistant* | **Yes**
    `location` | The region for your Azure Resources. By default, this will be the location for all your Azure Resources | **Yes**
    `appPassword` | The password for the [Azure Active Directory App](https://ms.portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/Overview) that will be used by your bot. It must be at least 16 characters long, contain at least 1 special character, and contain at least 1 numeric character. If using an existing app, this must be the existing password. | **Yes**
    `luisAuthoringKey` | The authoring key for your LUIS account. It can be found at https://www.luis.ai/user/settings or https://eu.luis.ai/user/settings | **Yes**

You can find more detailed deployment steps including customization in the [Virtual Assistant and Skill Template deployment](/docs/tutorials/assistantandskilldeploymentsteps.md) page.

> Note that if you choose to deploy your Skill manually or re-use an existing App-Service please ensure that Web Sockets are enabled on the App Service configuration pane. The deployment scripts supplied as part of the Skill template will do this automatically.

## Test your Skill

Once deployment is complete, you can start debugging through the following steps:

- Open the [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator).
- Within the Emulator, click **File > New Bot Configuration**.
- Provide the endpoint of your running Bot, e.g: http://localhost:3978/api/messages
- Provide the AppId and Secret values which you can find in your `appsettings.json` file under the `microsoftAppId` and `microsoftAppPassword` configuration settings.
- Click on **Save and Connect**.

## Update your Skill manifest

Your newly created Skill has a basic Skill manifest file provided in the root directory (`manifestTemplate.json`), this has been pre-populated with the Skill ID and name and a sample action which you can modify at this stage if required.

## Publish your Skill

You can now publish your Skill to Azure using the usual deployment tools and enable easier invocation of the Skill from your assistant project.

## Validate the Skill manifest endpoint

- To validate your Skill is deployed and working open up a browser window and navigate to your deployed Skill manifest (`/api/skill/manifest endpoint`). e.g.  `http://localhost:3978/api/skill/manifest`

## Adding your Skill to an assistant

To add your new Skill to your assistant/Bot we provide a `botskills` command line tool to automate the process of adding the Skill to your dispatch model and creating authentication connections where needed. 

Run the following command from a command prompt **within the directory of your assistant/Bot**. 

```bash
botskills connect --botName YOUR_BOT_NAME --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest" --luisFolder "<YOUR-SKILL_PATH>\Deployment\Resources\LU\en\" --cs
```

See the [Adding Skills](/docs/howto/skills/addingskills.md) for more detail on how to add skills.

## Testing your Skill

Test your skill works in your Bot through the emulator by typing "sample dialog"