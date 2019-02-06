# Enterprise Bot Sample

> [!NOTE]
> This topics applies to v4 version of the SDK.

This bot has been created using [Microsoft Bot Framework](https://dev.botframework.com).

## Prerequisites

- Ensure the [Node Package manager](https://nodejs.org/en/) is installed.

- Install the Azure Bot Service command line (CLI) tools. It's important to do this even if you've used the tools before to ensure you have the latest versions.

```bash
npm install -g ludown luis-apis qnamaker botdispatch msbot chatdown
```

- Install the latest Azure Command Line Tools (CLI) from [here](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)

- Install or update the LUISGen tool

```bash
dotnet tool install -g luisgen
```
or
```bash
dotnet tool update -g luisgen
```

## Initial Deployment

This instructions will help you to deploy the desired services for the first time, but the Bot itself will need some prior modifications before being completely functional and deployed.

**Important:** Before deploying, you **must** install all dependencies and build the project. This can be done with the following commands.
```bash
npm install
npm run build
```

>If you have multiple Azure subscriptions and want to ensure the deployment selects the correct one, run the following commands before continuing.

 Follow the browser login process into your Azure Account
```bash
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

Your new Bot project has a deployment recipe enabling the `msbot clone services` command to automate deployment of all the above services into your **Azure** subscription and ensure the .bot file in your project is updated with all of the services including keys, enabling seamless operation of your Bot. It also has multiple configuration options for the following languages: Chinese, English, French, German, Italian, and Spanish.

Run the following command referencing the folder of the language you want to use (e.g. `deploymentScripts\en`). Also, take into account that the services will be deployed to the Resource Group of the same name as the bot, and if it doesn't exist, it will be created.

> Once deployed, review the Pricing Tiers for the created services and adjust to suit your scenario.

```bash
msbot clone services --name "YOUR_BOT_NAME" --luisAuthoringKey "YOUR_AUTHORING_KEY" --folder "deploymentScripts\YOUR_LOCALE_FOLDER" --location "westus"
```
>**Note:** For retrieving your LUIS Authoring Key:
> - Go to the [LUIS Portal](https://www.luis.ai)
> - Sign in
> - Click on your name in the top right hand corner
> - Choose Settings
> - Copy your Authoring Key

### Post Deployment Configuration

Once the Initial Deployment is completed, ensure that you take note of the .bot file secret provided as this will be required in later steps.

Now you can update your `.env.production` file with the following information.
- `BOT_FILE_NAME`: Bot file name* (i.e. `MyEnterpriseBot.bot`)
- `BOT_FILE_SECRET`: Bot file secret
- `APPINSIGHTS_NAME`: AppInsights service name*
- `STORAGE_NAME`: CosmosDB service name*
- `BLOB_NAME`: BlobStorage service name*
- `LUIS_GENERAL`: Luis service name*
- `CONTENT_MODERATOR_NAME`: Content Moderator service name (only if used)*

> \*This information can be found in the `.bot` file

## Final Deployment

Once the **Initial Deployment** and the **Post Deployment Configuration** are completed, the updated Bot itself can be deployed using the `publish.cmd` that the `msbot clone services` created, which will execute the following command
```bash
az bot publish --resource-group "RESOURCE_GROUP_NAME" -n "BOT_NAME" --subscription "SUBSCRIPTION_ID" -v v4 --verbose --code-dir "." 
```

## Testing

Once the **Initial Deployment** and the **Final Deployment Configuration** are completed, you can use the [Microsoft Bot Framework Emulator](https://github.com/microsoft/botframework-emulator) for testing your Bot locally. After the **Final Deployment**, you can test the deployed Bot using the `production endpoint`.

### Testing the bot using Bot Framework Emulator

**Microsoft Bot Framework Emulator** is a desktop application that allows bot developers to test and debug their bots on localhost or running remotely through a tunnel.

- Install the Bot Framework Emulator from [here](https://aka.ms/botframework-emulator)
- Launch Bot Framework Emulator
- File -> Open Bot Configuration and navigate to `TypeScript Enterprise Bot Sample` folder
- Select your `.bot` file
- For debugging locally, use the command `npm start` and use the `development` endpoint (usually the `localhost`)

## Enabling more scenarios

### Authentication

To enable authentication follow these steps:

Register the SignInDialog in the MainDialog constructor
    
  ```typescript
  this.addDialog(new SignInDialog(this._services.authConnectionName);
  ```

Add the following in your code at your desired location to test a simple login flow:
  ```typescript
  const signInResult = await dc.beginDialog('SignInDialog');
  ```

### Content Moderation

Content moderation can be used to identify PII and adult content in the messages sent to the bot. To enable this functionality, go to the azure portal and create a new content moderator service. Collect your subscription key and region to configure your .bot file.

Add your Content Moderator name to the `.env.production` file using `CONTENT_MODERATOR_NAME` key. With this middleware enabled, all messages will be analyzed for inappropriate content, like PII, profanity, etc. The result of content moderation can be accessed via your bot state using the following code:
  ```typescript
  onTurn(context: TurnContext, next: () => Promise<void>): Promise<void> {
        const screenResult: Screen = context.turnState.get(ContentModeratorMiddleware.TextModeratorResultKey);

        // Use screenResult to take action over sensible content in messages.
  }
  ```

## Additional Resources

### Dependencies

- **[Restify](http://restify.com)** Used to host the web service for the bot, and for making REST calls
- **[dotenv](https://github.com/motdotla/dotenv)** Used to manage environmental variables

### Project Structure

- `index.ts` references the bot and starts a Restify server.
- `enterpriseBot.ts` loads the dialogs to run.
- `botServices.ts` generates the services that are used in the bot and are declared in your `.bot` configuration file.
- `/dialogs` folder contains the dialogs presented in this sample.
- `/middleware` folder contains the Content Moderator middleware and all telemetry related classes.

## Further Reading
- [Bot Framework Documentation](https://docs.botframework.com)
- [Bot basics](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-basics?view=azure-bot-service-4.0)
- [Activity processing](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-concept-activity-processing?view=azure-bot-service-4.0)
- [LUIS](https://luis.ai)
- [QnA Maker](https://qnamaker.ai)
- [Prompt Types](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-prompts?view=azure-bot-service-4.0&tabs=javascript)
- [Azure Bot Service Introduction](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-overview-introduction?view=azure-bot-service-4.0)
- [Channels and Bot Connector Service](https://docs.microsoft.com/en-us/azure/bot-service/bot-concepts?view=azure-bot-service-4.0)
