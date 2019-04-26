# Getting Started With the Virtual Assistant (TypeScript)

## Table of Contents
- [Prerequisites](#prerequisites)
- [Deployment](#deployment)
- [Starting your assistant](#starting-your-assistant)
- [Testing](#testing)

## Prerequisites
- Azure Bot Service CLI tools (latest versions)
```bash
npm install -g botdispatch ludown luis-apis qnamaker luisgen
```
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
- [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator)

## Deployment
The Virtual Assistant require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding
- Azure Cognitive Services - QnAMaker (including Azure Search, Azure Web App)
- Azure Cognitive Services - Content Moderator (optional manual step)

The following steps will help you to deploy these services using the provided deployment scripts in the generated assistant:

1. Open up a Command Prompt wherever your assistant was generated.
2. Run the `deploy.ps1` with the following command (being inside the generated assistant).
```bash
pwsh.exe -ExecutionPolicy Bypass -File deployment\scripts\deploy.ps1 -name "<NAME_OF_YOUR_ASSISTANT>" -location "<YOUR_LOCATION>" -appId "<YOUR_APP_ID>" -appPassword "<YOUR_APP_PASSWORD>" -luisAuthoringKey "<YOUR_LUIS_AUTHORING_KEY>"
```
3. Check the deployment finished successfully.

## Starting your assistant
1. Open up the generated assistant in your desired IDE (e.g `Visual Studio Code`).
2. Run `npm install`.
3. Run `npm run build`.
4. Run `npm run start`.

## Testing
1. Open the **Bot Framework Emulator**.
2. Within the Emulator, click **File > New Bot Configuration** .
3. Provide the endpoint of your running Bot, e.g: `http://localhost:3978/api/messages`
4. Provide the AppId and Secret values which you can find in your `appsettings.json` file under the `microsoftAppId` and `microsoftAppPassword` configuration settings.
5. Click on **Save and Connect**.

You should see an Introduction Adaptive card as shown below

![Introduction Card](https://user-images.githubusercontent.com/43043272/55245287-0e01fe00-5200-11e9-8709-4d24c0f45502.png)
