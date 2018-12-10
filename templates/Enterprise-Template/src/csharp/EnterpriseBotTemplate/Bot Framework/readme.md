
# Enterprise Bot Template - Deploying your Bot

> [!NOTE]
> This topics applies to v4 version of the SDK. 

## Prerequisites

- Ensure the [Node Package manager](https://nodejs.org/en/) is installed.

- Install the Azure Bot Service command line (CLI) tools. It's important to do this even if you've used the tools before to ensure you have the latest versions.

```shell
npm install -g ludown luis-apis qnamaker botdispatch msbot chatdown
```

- Install the Azure Command Line Tools (CLI) from [here](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)

- Install the AZ Extension for Bot Service
```shell
az extension add -n botservice
```

- Install the LUISGen tool

```shell
dotnet tool install -g luisgen
```

## Configuration

1. Retrieve your LUIS Authoring Key
   - Go to https://www.luis.ai and signin.
   - Once signed in click on your name in the top right hand corner.
   - Choose Settings and make a note of the Authoring Key for the next step.

## Deployment

>If you have multiple Azure subscriptions and want to ensure the deployment selects the correct one, run the following commands before continuing.

 Follow the browser login process into your Azure Account
```shell
az login
az account list
az account set --subscription "YOUR_SUBSCRIPTION_NAME"
```

Enterprise Template Bots require the following dependencies for end to end operation.
- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding
- Azure Cognitive Services - QnAMaker (including Azure Search, Azure Web App)
- Azure Cognitive Services - Content Moderator (optional manual step)

Your new Bot project has a deployment recipe enabling the `msbot clone services` command to automate deployment of all the above services into your Azure subscription and ensure the .bot file in your project is updated with all of the services including keys enabling seamless operation of your Bot. It also has multiple configuration options for the following languages: Chinese, English, French, German, Italian, and Spanish.

Run the following command referencing the folder of the language you want to use (e.g. `DeploymentScripts\en`).

> Once deployed, review the Pricing Tiers for the created services and adjust to suit your scenario.

```shell
msbot clone services --name "$safeprojectname$" --luisAuthoringKey "YOUR_AUTHORING_KEY" --folder "DeploymentScripts\YOUR_LOCALE_FOLDER" --location "westus"
```

Once this is complete ensure that you make a note of the .bot file secret provided as this will be required for later steps. At this time, take the secret and update the `botFileSecret` entry in your `appsettings.json` file. This will ensure your Bot can decrypt the secrets.

Update your appsettings.json file with the .bot file path, .bot file secret, and AppInsights intrumentation key (this can be found in the generated .bot file).
    
        {
          "botFilePath": ".\\YOUR_BOT_PATH.bot",
          "botFileSecret": "YOUR_BOT_KEY",
          "ApplicationInsights": {
            "InstrumentationKey": "YOUR_INSTRUMENTATION_KEY"
          }
        }
## Testing

Once complete, run your bot project within your development envrionment and open the Bot Framework Emulator. Within the Emulator, choose Open Bot from teh File menu and navigate to the .bot file in your directory.

Then type ```hi``` to verify everything is working.

## Deploy to Azure

Testing can be performed end to end locally, when your ready to deploy your Bot to Azure for additional testing you can use the following command to publish the source code

```shell
az bot publish --name "$safeprojectname$" --resource-group "$safeprojectname$"
```

## Analytics
Now that your bot is collecting telemetry, you can use a sample PowerBI dashboard to view [advanced conversational analytics](https://github.com/Microsoft/AI/tree/master/solutions/analytics).

## Next Steps
Find [additional documentation](http://aka.ms/ent_docs) for enabling more scenarios and further customization.