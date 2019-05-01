# Create a New Skill using Bot Builder Skill Generator

> [!NOTE]
> This topics applies to v4 version of the SDK.

## Table of Contents
- [Create a New Skill using Bot Builder Skill Generator](#create-a-new-skill-using-bot-builder-skill-generator)
  - [Table of Contents](#table-of-contents)
  - [Prerequisites](#prerequisites)
  - [Create your project](#create-your-project)
    - [Generate the assistant using prompts](#generate-the-assistant-using-prompts)
    - [Generate the sample using CLI parameters](#generate-the-sample-using-cli-parameters)
      - [Example](#example)
  - [Deployment](#deployment)
  - [Starting your skill](#starting-your-skill)
  - [Testing](#testing)
 
## Prerequisites
> It's important to ensure all of the following pre-requisites are installed on your machine prior to attempting deployment otherwise you may run into deployment issues.

1. Ensure the [Node Package manager](https://nodejs.org/en/) is installed.
2. PowerShell Core version 6 (Required for cross platform deployment support)
   * [Download PowerShell Core on Windows](https://aka.ms/getps6-windows)
   * [Download PowerShell Core on macOS and Linux](https://aka.ms/getps6-linux)
3. Install  Bot Framework (CLI) tool dependencies. It's important to do this even if you have earlier versions as the Skill template makes use of the latest capabilities: 
   ```
   npm install -g botdispatch, ludown, luis-apis, qnamaker, luisgen
   ```
4. Install [Yeoman](http://yeoman.io)
   ```
   npm install -g yo
   ```
5. Install the [Azure Command Line Tools (CLI)](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)
6. Retrieve your LUIS Authoring Key
   - Review the [LUIS regions](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation page for the correct LUIS portal for the region you plan to deploy to. Note that www.luis.ai refers to the US region and an authoring key retrieved from this portal will not work within a europe deployment. 
   - Once signed in click on your name in the top right hand corner.
   - Choose Settings and make a note of the Authoring Key for the next step.

## Create your project

>//Temporary ahead of package publishing>

- Clone the [Microsoft AI](https://github.com/Microsoft/AI) repository
- Go to `templates\Skill-Template\typescript\generator-botbuilder-skill` folder in a command line
- Run npm link to symlink the package folder
>//

Now you can execute the Skill template generator with this command.

```bash
yo botbuilder-assistant:skill
```

**At this point you have two different options to proceed:**

### Generate the assistant using prompts

- The generator will start prompting for some information that is needed for generating the sample:
    - `What's the name of your skill? (customSkill)`
        > The name of your assistant (used also as your project's name and for the root folder's name).
    - `What will your skill do ()`
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
| -d, --skillDesc <description>       | description of the new skill (by default takes ``) |
| -l, --skillLang <array of languages>| languages for the new skill. Possible values are `de`, `en`, `es`, `fr`, `it`, `zh` (by default takes all the languages)| 
| -p, --skillGenerationPath <path>    | destination path for the new skill (by default takes the path where you are running the generator)            |
| --noPrompt                        | indicates to avoid the prompts                                                                               |

**NOTE:** If you don't use the _--noPrompt_ option, the process will keep prompting, but using the input values by default.

#### Example

```bash
> yo botbuilder-assistant:skill -n "My skill" -d "A description for my new skill" -l "en,es" -p "\aPath" --noPrompt
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

## Deployment

The Skill require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding

> Review the pricing and terms for the services and adjust to suit your scenario.

To deploy your services using the default configuration, follow the steps in this common [deployment documentation page](/docs/tutorials/assistantandskilldeploymentsteps.md)

## Starting your skill

- Open up the generated skill in your desired IDE (e.g Visual Studio Code).
- Run `npm run start`.

## Testing

Once deployment is complete, you can start debugging through the following steps:
- Open the [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator). 
- Within the Emulator, click **File > New Bot Configuration**.
- Provide the endpoint of your running Bot, e.g: http://localhost:3978/api/messages
- Provide the AppId and Secret values which you can find in your `appsettings.json` file under the `microsoftAppId` and `microsoftAppPassword` configuration settings.
- Click on **Save and Connect**.