# Virtual Assistant Deployment

## Overview

The Virtual Assistant Solution provides everything you need to get started with building your own Assistant. Base Assistant capabilities are provided within the solution including language models for you to build upon along with Conversational Skill support enabling you to plug-in additional capabilities through configuration. See the [Overview](./readme.md) for more information.

The Virtual Assistant solution is under ongoing development within an open-source GitHub repo enabling you to participate with our ongoing work. 

Follow the instructions below to build, deploy and configure your Assistant.

### Prerequisites
- [Node.js](https://nodejs.org/) version 8.5 or higher.

- Install the Azure Bot Service command line (CLI) tools. It's important to do this even if you have earlier versions as the Virtual Assistant makes use of new deployment capabilities.

```shell
npm install -g botdispatch chatdown ludown luis-apis luisgen msbot qnamaker  
```
- Install the Azure Command Line Tools (CLI) from [here](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)

- Install the Az Extension for Bot Service
```shell
az extension add -n botservice
```

- Retrieve your LUIS Authoring Key
   - Review [this](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation page for the correct LUIS portal for the region you plan to deploy to. 
   - Once signed in click on your name in the top right hand corner.
   - Choose Settings and make a note of the Authoring Key for the next step.

### Clone the Repo

The first step is to clone the [Microsoft Conversational AI GitHub Repo](https://github.com/Microsoft/AI). You'll find the Virtual Assistant solution within the `solutions\Virtual-Assistant` folder.

Once the Solution has been cloned you will see the following folder structure.

    | - Virtual-Assistant
        | - Assistant
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
        | - Microsoft.Bot.Solutions
      | - VirtualAssistant.sln

### Build the Solution

Once cloned the next step is to build the VirtualAssistant solution within Visual Studio. Deployment must have been completed before you can run the project due to this stage creating key dependencies in Azure along with your configured .bot file.

### Deployment

The Virtual Assistant require the following dependencies for end to end operation.
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

Your Virtual Assistant project has a deployment recipe enabling the `msbot clone services` command to automate deployment of all the above services into your Azure subscription and ensure the .bot file in your project is updated with all of the services including keys enabling seamless operation of your Virtual Assistant.

To deploy your Virtual Assistant including all dependencies - e.g. CosmosDb, Application Insights, etc. run the following command from a command prompt within your project folder. Ensure you update the authoring key from the previous step and choose the Azure datacenter location you wish to use.

> Ensure the LUIS authoring key retrieved on the previous step is for the region you specify below.

```shell
msbot clone services --name "MyCustomAssistantName" --luisAuthoringKey "YOUR_AUTHORING_KEY" --folder "DeploymentScripts\en\msbotClone" --location "westus"
```

The msbot tool will outline the deployment plan including location and SKU. Ensure you review before proceeding.

![Deployment Confirmation](./media/virtualassistant-deploymentplan.png)

>After deployment is complete, it's **imperative** that you make a note of the .bot file secret provided as this will be required for later steps.

- Update your `appsettings.json` file with the newly created .bot file name and .bot file secret.
- Run the following command and retrieve the InstrumentationKey for your Application Insights instance and update `InstrumentationKey` in your `appsettings.json` file.

`msbot list --bot YOURBOTFILE.bot --secret YOUR_BOT_SECRET`

        {
          "botFilePath": ".\\YOURBOTFILE.bot",
          "botFileSecret": "YOUR_BOT_SECRET",
          "ApplicationInsights": {
            "InstrumentationKey": "YOUR_INSTRUMENTATION_KEY"
          }
        }

## Skill Configuration

The Virtual Assistant Solution is fully integrated with all available skills out of the box. Skill configuration can be found in your appSettings.json file. An example of the Skill Configuration entries is shown below for reference.

```
"skills": [
    {
      "type": "skill",
      "id": "calendarSkill",
      "name": "calendarSkill",
      "assembly": "CalendarSkill.CalendarSkill, CalendarSkill, Version=1.0.0.0, Culture=neutral",
      "dispatchIntent": "l_Calendar",
      "authConnectionName": "",
      "luisServiceId": "calendar",
      "parameters": [
        "IPA.Timezone"
      ]
    }
]
```
## Skill Authentication

If you wish to make use of the Calendar, Email and Task Skills you need to configure an Authentication Connection enabling uses of your Assistant to authenticate against services such as Office 365 and securely store a token which can be retrieved by your assistant when a user asks a question such as *"What's my day look like today"* to then use against an API like Microsoft Graph.

The [Add Authentication to your bot](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-tutorial-authentication?view=azure-bot-service-3.0) section in the Azure Bot Service documentation covers more detail on how to configure Authentication. However in this scenario, the automated deployment step has already created the Azure AD v2 Application for your Bot. Therefore you only need to perform the following steps from the above documentation page:

- Navigate to https://apps.dev.microsoft.com/ and find the application created in the previous step which should match your Bot name.
- Under Platforms, click Add Platform.
  - In the Add Platform pop-up, click Web.
  - Leave Allow Implicit Flow checked.
  - For Redirect URL, enter https://token.botframework.com/.auth/web/redirect
  - Leave Logout URL blank.
- Under Microsoft Graph Permissions, you can need to add additional *delegated* permissions.
- Each of the Skills require a specific set of Scopes, refer to the documentation for each skill or use the following list of Scopes that contain the scopes needed for all skills. 
  - `Calendars.ReadWrite`, `Mail.Read`, `Mail.Send`, `Notes.ReadWrite.All`, `People.Read.All`, `User.Read.All`

Next you need to create the Authentication Connection for your Bot. Ensure you use the same combination of Scopes that you provided in the above command. The first command shown below will retrieve the appId (ApplicationId) and appPassword (Client Secret) that you need to complete this step.

The commands shown below assume you have used the deployment process and your resource group name is the same as your bot. Replace `YOUR_AUTH_CONNECTION_NAME` with the name of the auth connection you wish to create and use that in the next step.

```shell
msbot get production --secret YOUR_SECRET

az bot authsetting create --resource-group YOUR_BOT_NAME --name YOUR_BOT_NAME --setting-name "YOUR_AUTH_CONNECTION_NAME" --client-id "YOUR_APPLICATION_ID" --client-secret "YOUR_APPLICATION_PASSWORD" --provider-scope-string "Calendars.ReadWrite Mail.Read Mail.Send Notes.ReadWrite.All People.Read.All User.Read.All" --service Aadv2
```

The final step is to update your .bot file and associated Skills (in appSettings.config) with the Authentication connection name, this is used by the Assistant to enable Authentication prompts or use of Linked Accounts.

```shell
msbot connect generic --name "Authentication" --keys "{\"Azure Active Directory v2\":\"YOUR_AUTH_CONNECTION_NAME\"}" --bot YOURBOTFILE.bot --secret "YOUR_BOT_SECRET" --url "portal.azure.net"
```

Then in the appSettings.config updated the `authConnectionName` for each skill as appropriate. 

> Other Authentication Service Providers exist including the ability to create custom oAuth providers. `az bot authsetting list-providers` is a quick way to review the pre-configured ones.

## Testing
Once deployment is complete, run your bot project within your development environment and open the [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator). Within the Emulator, choose Open Bot from the File menu and navigate to the .bot file in your directory which was created in the previous step. 

>Ensure you have the latest emulator installed and update the development endpoint to reflect the port number that Visual Studio chooses when you start debugging otherwise you'll receive connection errors.

You should see an Introduction Adaptive card and the example on-boarding process will start.

See the [Testing](./virtualassistant-testing.md) section for information on how to test your Virtual Assistant.
