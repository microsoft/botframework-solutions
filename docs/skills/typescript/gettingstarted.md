# Creating a new Skill (Typescript)

> [!NOTE]
> This topics applies to v4 version of the SDK.

## Table of Contents
- [Creating a new Skill (Typescript)](#creating-a-new-skill-typescript)
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

1. Ensure the [Node Package manager](https://nodejs.org/en/) is installed.
1. Install [Yeoman](http://yeoman.io) using npm:
    ```bash
    npm install -g yo
    ```
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

1. Open a terminal in the desired folder for generating the skill.
2. Run the following command for generating our new project.
```bash
> yo botbuilder-skill
```
3. The generator will start prompting for some information that is needed for generating the skill.
4. Check the summary in your screen.

```bash
- Name: <aName>
- Description: <aDescription>
- Path: <aPath>
```

**WARNING:** The process will fail if it finds another folder with the same name of the new skill.

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