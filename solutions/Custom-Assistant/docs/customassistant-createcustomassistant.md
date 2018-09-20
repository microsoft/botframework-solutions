# Custom Assistant Creation

## Overview

> [!NOTE]
> This topics applies to v4 version of the SDK.

The Custom Assistant Solution provides everything you need to get started with building your own Assistant. Base Assistant capabilities are provided within the solution including language models for you to build upon along with Conversational Skill support enabling you to plug-in additional capabilities through configuration.

The Custom Assistant solution is under ongoing development within an open-source GitHub repo enabling you to participate in the build-out of the Custom Assistant vision.

The Custom Assitant Solution is available for .NET, targetting **V4** versions of the SDK.

### Prerequisites
- Install the Azure Bot Service command line (CLI) tools. It's important to do this even if you've used the tools before to ensure you have the latest versions.

```shell
npm install -g ludown luis-apis qnamaker botdispatch msbot luisgen chatdown
```

- Install the Azure Command Line Tools (CLI) from [here](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)

- Install the AZ Extension for Bot Service
```shell
az extension add -n botservice
```

### Clone the Repo

The first step is to clone the [Microsoft Conversational AI GitHub Repo](https://github.com/Microsoft/AI). You'll find the Custom Assistant solution within the `solutions\Custom-Assistant` folder.

Once the Solution has been cloned you will see the following folder structure.

    | - CustomAssistant
        | - LinkedAccounts.Web
    | - Skills
        | - CalendarSkill
        | - DemoSkill
        | - EmailSkill
        | - PointofInterestSkill
        | - ToDoSkill
        | - Test
    | - TestHarnesses
        | - Assistant-ConsoleDirectLineSample
        | - Assistant-WebTest
    | - CustomAssistant.sln
    | - Skills.sln

### Build the Solution

Once cloned the next step it to build the CustomAssistant and Skills solutions within Visual Studio.

### Deployment

The Custom Assistant require the following dependencies for end to end operation.
- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding
- Azure Cognitive Services - QnAMaker (including Azure Search, Azure Web App)
- Azure Cognitive Services - Content Moderator (optional manual step)

> Review the pricing and terms for the services and adjust to suit your scenario

If you have multiple Azure subscriptions and want to ensure the deployment selects the correct one, run the following commands before continuing.

```shell
az login
az account list
az account set --subscription "YOUR_SUBSCRIPTION_NAME"
```

Your Custom Assistant project has a deployment recipe enabling the `msbot clone services` command to automate deployment of all the above services into your Azure subscription and ensure the .bot file in your project is updated with all of the services including keys enabling seamless operation of your Custom Assistant.

To deploy your Custom Assistant including all dependencies - e.g. CosmosDb, Application Insights, etc. run the following command from a command prompt within your project folder. Ensure you update the authoring key from the previous step and choose the Azure datacenter location you wish to use.

```shell
msbot clone services --name "MyCustomAssistantName" --luisAuthoringKey "YOUR_AUTHORING_KEY" --folder "DeploymentScripts\msbotClone" --location "westus"
```

After deployment is complete, ensure that you make a note of the .bot file secret provided as this will be required for later steps.

Update your `appsettings.json` file with the .bot file path, .bot file secret, and AppInsights intrumentation key (this can be found in the generated .bot file).

        {
          "botFilePath": ".\\darrenjentbot.bot",
          "botFileSecret": "YOUR_BOT_SECRET",
          "ApplicationInsights": {
            "InstrumentationKey": "YOUR_INSTRUMENTATION_KEY"
          }
        }

## Testing

Once deployment is complete, run your bot project within your development envrionment and open the [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator). Within the Emulator, choose Open Bot from the File menu and navigate to the .bot file in your directory which was created in the previous step.

You should see an Introduction Adaptive card and the example on-boarding will start. 

See the [Testing](./customassistant-testing.md) section for information on how to test your Custom Assistant.

> Note that the Deployment will deploy your Custom Assistant but will not configure Skills. These are an optional step which is documented [here](./customassistant-addingskills.md).