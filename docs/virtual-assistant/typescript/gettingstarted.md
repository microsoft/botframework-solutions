# Getting Started With the Virtual Assistant (TypeScript)

> [!NOTE]
> This topics applies to v4 version of the SDK.

## Table of Contents
- [Getting Started With the Virtual Assistant (TypeScript)](#getting-started-with-the-virtual-assistant-typescript)
  - [Table of Contents](#table-of-contents)
  - [Prerequisites](#prerequisites)
  - [Create your project](#create-your-project)
    - [Generate the assistant using prompts](#generate-the-assistant-using-prompts)
    - [Generate the sample using CLI parameters](#generate-the-sample-using-cli-parameters)
      - [Example](#example)
  - [Deployment](#deployment)
  - [Starting your assistant](#starting-your-assistant)
  - [Testing](#testing)
 
## Prerequisites
> It's important to ensure all of the following pre-requisites are installed on your machine prior to attempting deployment otherwise you may run into deployment issues.

1. Ensure the [Node Package manager](https://nodejs.org/en/) is installed.
1. PowerShell Core version 6 (Required for cross platform deployment support)
   * [Download PowerShell Core on Windows](https://aka.ms/getps6-windows)
   * [Download PowerShell Core on macOS and Linux](https://aka.ms/getps6-linux)
1. Install  Bot Framework (CLI) tool dependencies. It's important to do this even if you have earlier versions as the Virtual Assistant makes use of the latest capabilities: 
   ```
   npm install -g botdispatch, ludown, luis-apis, qnamaker, luisgen
   ```
1. Install [Yeoman](http://yeoman.io)
   ```
   npm install -g yo
   ```
1. Install the [Azure Command Line Tools (CLI)](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)
1. Retrieve your LUIS Authoring Key
   - Review the [LUIS regions](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation page for the correct LUIS portal for the region you plan to deploy to. Note that www.luis.ai refers to the US region and an authoring key retrieved from this portal will not work within a europe deployment. 
   - Once signed in click on your name in the top right hand corner.
   - Choose Settings and make a note of the Authoring Key for the next step.

## Create your project

>//Temporary ahead of package publishing>

- Clone the [Microsoft AI](https://github.com/Microsoft/AI) repository
- Go to `templates\Virtual-Assistant-Template\typescript\generator-botbuilder-assistant` folder in a command line
- Run npm link to symlink the package folder
>//

Now you can execute the Virtual Assistant generator with this command.

```bash
yo botbuilder-assistant
```

**At this point you have two different options to proceed:**

### Generate the assistant using prompts

- The generator will start prompting for some information that is needed for generating the sample:
    - `What's the name of your assistant? (customAssistant)`
        > The name of your assistant (used also as your project's name and for the root folder's name).
    - `What's the description of your assistant? ()`
        > The description of your assistant.
    - `Which languages will your assistant use? (by default takes all the languages)`
        - [x] Chinese (`zh`)
        - [x] Deutsch (`de`)
        - [x] English (`en`)
        - [x] French (`fr`)
        - [x] Italian (`it`)
        - [x] Spanish (`es`)
    - `Do you want to change the new assistant's location?`
        > A confirmation to change the destination for the generation.
        - `Where do you want to generate the assistant? (by default takes the path where you are running the generator)`
            > The destination path for the generation.
    - `Looking good. Shall I go ahead and create your new assistant?`
        > Final confirmation for creating the desired assistant.

### Generate the sample using CLI parameters

| Option                            | Description                                                                                                  |
|-----------------------------------|--------------------------------------------------------------------------------------------------------------|
| -n, --assistantName <name>              | name of new assistant (by default takes `customAssistant`)                                                          |
| -d, --assistantDesc <description>       | description of the new assistant (by default takes ``) |
| -l, --assistantLang <array of languages>| languages for the new assistant. Possible values are `de`, `en`, `es`, `fr`, `it`, `zh` (by default takes all the languages)| 
| -p, --assistantGenerationPath <path>    | destination path for the new assistant (by default takes the path where you are runnning the generator)            |
| --noPrompt                        | indicates to avoid the prompts                                                                               |

**NOTE:** If you don't use the _--noPrompt_ option, the process will keep prompting, but using the input values by default.

#### Example

```bash
> yo botbuilder-assistant -n "Virtual Assistant" -d "A description for my new assistant" -l "en,es" -p "\aPath" --noPrompt
```

After this, you can check the summary in your screen:
```bash
- Name: <aName>
- Description: <aDescription>
- Selected languages: <languages>
- Path: <aPath>
```

>**WARNING:** The process will fail if it finds another folder with the same name of the new assistant.

>**NOTE:** Remind to have an **unique** assistant's name for deployment steps. 

You now have your own Assistant! Follow the Deployment steps below before you try and run the project as deployment creates key dependencies required for operation.

## Deployment

The Virtual Assistant require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding
- Azure Cognitive Services - QnAMaker (including Azure Search, Azure Web App)

> Review the pricing and terms for the services and adjust to suit your scenario.

To deploy your services using the default configuration, follow the steps in this common [deployment documentation page](/docs/virtual-assistant/common/deploymentsteps.md)

## Starting your assistant

- Open up the generated assistant in your desired IDE (e.g Visual Studio Code).
- Run `npm install`.
- Run `npm run build`.
- Run `npm run start`.

## Testing

Once deployment is complete, you can start debugging through the following steps:
- Open the [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator). 
- Within the Emulator, click **File > New Bot Configuration**.
- Provide the endpoint of your running Bot, e.g: http://localhost:3978/api/messages
- Provide the AppId and Secret values which you can find in your `appsettings.json` file under the `microsoftAppId` and `microsoftAppPassword` configuration settings.
- Click on **Save and Connect**.

You should see an Introduction Adaptive card as shown below

![Introduction Card](https://user-images.githubusercontent.com/43043272/55245287-0e01fe00-5200-11e9-8709-4d24c0f45502.png)
