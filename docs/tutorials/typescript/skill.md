# Create a new Bot Framework Skill (TypeScript)

**APPLIES TO:** ✅ SDK v4

## In this tutorial
- [Create a new Bot Framework Skill (TypeScript)](#create-a-new-bot-framework-skill-typescript)
  - [In this tutorial](#in-this-tutorial)
  - [Intro](#intro)
    - [Purpose](#purpose)
    - [Prerequisites](#prerequisites)
    - [Time to Complete](#time-to-complete)
    - [Scenario](#scenario)
  - [Download and install](#download-and-install)
  - [Create your Skill](#create-your-skill)
    - [Generate the skill using prompts](#generate-the-skill-using-prompts)
    - [Generate the sample using CLI parameters](#generate-the-sample-using-cli-parameters)
      - [Example](#example)
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
   - Review the [LUIS regions](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation page for the correct LUIS portal for the region you plan to deploy to. Note that www.luis.ai refers to the US region and an authoring key retrieved from this portal will not work within a europe deployment. 
   - Once signed in replace your name in the top right hand corner.
   - Choose Settings and make a note of the Authoring Key for the next step.

### Time to Complete

20 minutes

### Scenario

A Bot Framework Skill app (in TypeScript) that greets a new user.

## Download and install

> It's important to ensure all of the following prerequisites are installed on your machine prior to attempting deployment otherwise you may run into deployment issues.

1. Download and install the [Node Package Manager (NPM)](https://nodejs.org/en/).
2. Download and install PowerShell Core version 6 (required for cross platform deployment support).
   * [Download PowerShell Core on Windows](https://aka.ms/getps6-windows)
   * [Download PowerShell Core on macOS and Linux](https://aka.ms/getps6-linux)
3. Install  Bot Framework (CLI) tool dependencies. It's important to do this even if you have earlier versions as we make use of the latest capabilities: 
   ```
   npm install -g botdispatch ludown luis-apis luisgen botskills
   ```
4. Install [Yeoman](http://yeoman.io)
   ```
   npm install -g yo
   ```
5. Install the [Azure Command Line Tools (CLI)](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest).

## Create your Skill

Install the botbuilder-assistant generator

```bash
    npm install -g generator-botbuilder-assistant
```

Now you can execute the Skill sub-generator with this command.

```bash
yo botbuilder-assistant:skill
```

**At this point you have two different options to proceed:**
### Generate the skill using prompts

- The generator will start prompting for some information that is needed for generating the sample:
    - `What's the name of your skill? (customSkill)`
        > The name of your skill (used also as your project's name and for the root folder's name).
    - `What's the description of your skill? ()`
        > The description of your skill.
    - `Which languages will your skill use? (by default takes all the languages)`
        - [x] Chinese (`zh`)
        - [x] Deutsch (`de`)
        - [x] English (`en`)
        - [x] French (`fr`)
        - [x] Italian (`it`)
        - [x] Spanish (`es`)
    - `Do you want to change the new skill's location?`
        > A confirmation to change the destination for the generation.
        - `Where do you want to generate the skill? (by default takes the path where you are running the generator)`
            > The destination path for the generation.
    - `Looking good. Shall I go ahead and create your new skill?`
        > Final confirmation for creating the desired skill.

### Generate the sample using CLI parameters

| Option                            | Description                                                                                                  |
|-----------------------------------|--------------------------------------------------------------------------------------------------------------|
| -n, --skillName <name>              | name of new skill (by default takes `customSkill`)                                                          |
| -d, --skillDesc <description>       | description of the new skill (by default is empty) |
| -l, --skillLang <array of languages>| languages for the new skill. Possible values are `de`, `en`, `es`, `fr`, `it`, `zh` (by default takes all the languages)| 
| -p, --skillGenerationPath <path>    | destination path for the new skill (by default takes the path where you are running the generator)            |
| --noPrompt                        | indicates to avoid the prompts                                                                               |

**NOTE:** If you don't use the _--noPrompt_ option, the process will keep prompting, but using the input values by default.

#### Example

```bash
> yo botbuilder-assistant:skill -n "My skill" -d "A description for my new skill" -l "en" -p "\aPath" --noPrompt
```

After this, you can check the summary in your screen:
```bash
- Name: <aName>
- Description: <aDescription>
- Selected languages: <languages>
- Path: <aPath>
```

>**WARNING:** The process will fail if it finds another folder with the same name of the new skill.

>**NOTE:** Remind to have an **unique** skill's name for deployment steps. 

You now have your own Skill! Follow the Deployment steps below before you try and run the project as deployment creates key dependencies required for operation.

## Deploy your Skill

The Skill require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding

> Review the pricing and terms for the services and adjust to suit your scenario.

To deploy your services using the default configuration, follow the steps in this common [deployment documentation page](/docs/tutorials/assistantandskilldeploymentsteps.md)

## Test your Skill

Once deployment is complete, you can start debugging through the following steps:
- Open the generated skill in your desired IDE (e.g Visual Studio Code)
- Run `npm run start` 
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

To add your new Skill to your assistant/Bot, run the following command from a command prompt **within the directory of your assistant/Bot**. At this time we have a powershell script and a preview botskills CLI.

``
.\Deployment\scripts\add_remote_skill.ps1 -botName "YOUR_BOT_NAME" -manifestUrl https://YOUR_SKILL.azurewebsites.net/api/skill/manifest
``

```bash
botskills connect --botName YOUR_BOT_NAME --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest" --luisFolder "<YOUR-SKILL_PATH>\deployment\resources\LU\en\" --ts 
```

See the [Adding Skills](/docs/advanced/skills/addingskills.md) for more detail on how to add skills.

## Testing your Skill

Test your skill works in your Bot through the emulator by typing "sample dialog"