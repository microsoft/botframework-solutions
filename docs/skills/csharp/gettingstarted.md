# Creating a new Skill (C#)

> [!NOTE]
> This topics applies to v4 version of the SDK.

## Table of Contents
- [Creating a new Skill (C#)](#creating-a-new-skill-c)
  - [Table of Contents](#table-of-contents)
  - [Pre-requisites](#pre-requisites)
  - [Create your project](#create-your-project)
  - [Deployment](#deployment)
  - [Update Manifest](#update-manifest)
  - [Publish Skill](#publish-skill)
  - [Testing Manifest Endpoint](#testing-manifest-endpoint)
  - [Adding your new Skill to a Bot](#adding-your-new-skill-to-a-bot)
  - [Testing](#testing)
  
## Pre-requisites
> It's important to ensure all of the following pre-requisites are installed on your machine prior to attempting deployment otherwise you may run into deployment issues.

1. Install the [Skill Template](TBC)
1. Ensure you have updated [.NET Core](https://www.microsoft.com/net/download) to the latest version.  
1. Ensure the [Node Package manager](https://nodejs.org/en/) is installed.
1. PowerShell Core version 6 (Required for cross platform deployment support)
   * [Download PowerShell Core on Windows](https://aka.ms/getps6-windows)
   * [Download PowerShell Core on macOS and Linux](https://aka.ms/getps6-linux)
1. Install  Bot Framework (CLI) tool dependencies. It's important to do this even if you have earlier versions as the Virtual Assistant makes use of the latest capabilities: 
   ```
   npm install -g botdispatch, ludown, luis-apis, qnamaker, luisgen
   ```
1. Install the [Azure Command Line Tools (CLI)](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)
1. Retrieve your LUIS Authoring Key
   - Review the [LUIS regions](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation page for the correct LUIS portal for the region you plan to deploy to. Note that www.luis.ai refers to the US region and an authoring key retrieved from this portal will not work within a europe deployment. 
   - Once signed in click on your name in the top right hand corner.
   - Choose Settings and make a note of the Authoring Key for the next step.

## Create your project

1. In Visual Studio, click **File > New Project**.
2. Under Bot, select **Skill Template**.
3. Name your project and click **Create**.
4. Click **Tools > NuGet Package Manager > Package Manager Settings**
5. In the Navigation Pane, select **Package Sources**
6. Click '+' to add a new source.
7. Set **Name** to **Microsoft AI MyGet Feed**
8. Set **Source** to **https://botbuilder.myget.org/F/aitemplates/api/v3/index.json**
9. Click **OK**.
10. Build your project to restore your NuGet packages.

You now have your new Skill! Follow the Deployment steps below before you try and run the project as deployment creates key dependencies required for operation.

## Deployment

The Virtual Assistant require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding

> Review the pricing and terms for the services and adjust to suit your scenario.

To deploy your services using the default configuration, follow the steps in this common [deployment documentation page](/docs/virtual-assistant/common/deploymentsteps.md)

> Note that if you choose to deploy your Skill manually or re-use an existing App-Service please ensure that Web Sockets are enabled on the App Service configuration pane. The deployment scripts supplied as part of the Skill template will do this automatically.

## Update Manifest

Your newly created Skill has a basic Skill manifest file provided in the root directory (`manifestTemplate.json`), this has been pre-populated with the Skill ID and name and a sample action which you can modify at this stage if required.

## Publish Skill

- Publish your Skill to Azure

## Testing Manifest Endpoint

- To validate your Skill is deployed and working open up a browser window and navigate to your deployed Skill manifest (`/api/skill/manifest endpoint`). e.g.  `http://localhost:3978/api/skill/manifest`

## Adding your new Skill to a Bot

```
.\Deployment\scripts\add_remote_skill.ps1 -botName "YOUR_BOT_NAME" -manifestUrl https://YOUR_SKILL.azurewebsites.net/api/skill/manifest
```

## Testing

- Test your skill works in your Bot through the emulator by typing "sample dialog"