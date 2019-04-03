# Getting Started With the Virtual Assistant

The Virtual Assistant solution is under ongoing development within an open-source repository, you are invited to participate in the ongoing development.
Follow the instructions below to build, deploy and configure your Virtual Assistant.


## Table of Contents
- [Getting Started With the Virtual Assistant](#getting-started-with-the-virtual-assistant)
  - [Table of Contents](#table-of-contents)
  - [Prerequisites](#prerequisites)
    - [Clone the Repo](#clone-the-repo)
    - [Build the Solution](#build-the-solution)
  - [Deployment](#deployment)
  - [Skill Configuration](#skill-configuration)
    - [Skill Authentication](#skill-authentication)
  - [Testing](#testing)

## Prerequisites
> It's important to ensure all of the following pre-requisites are installed on your machine prior to attempting deployment otherwise you may run into deployment issues.

1. Ensure you have updated [.NET Core](https://www.microsoft.com/net/download) to the latest version.  
2. [Node.js](https://nodejs.org/) version 8.5 or higher.
3. PowerShell Core version 6 (Required for cross platform deployment support)
   * [Download PowerShell Core on Windows](https://aka.ms/getps6-windows)
   * [Download PowerShell Core on macOS and Linux](https://aka.ms/getps6-linux)
4. Install the Azure Bot Service command line (CLI) tools. It's important to do this even if you have earlier versions as the Virtual Assistant makes use of new deployment capabilities. **Minimum version 4.3.2 required for msbot, and minimum version 1.1.0 required for ludown.**
  ```shell
  npm install -g botdispatch chatdown ludown luis-apis msbot qnamaker  
  ```
5. Install [LuisGen](https://github.com/Microsoft/botbuilder-tools/blob/master/packages/LUISGen/src/npm/readme.md)
  ```shell
  dotnet tool install -g luisgen
  ```
6. Install the [Azure Command Line Tools (CLI)](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)
7. Retrieve your LUIS Authoring Key
   - Review the [LUIS regions](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation page for the correct LUIS portal for the region you plan to deploy to. Note that www.luis.ai refers to the US region and an authoring key retrieved from this portal will not work with a europe deployment. 
   - Once signed in click on your name in the top right hand corner.
   - Choose Settings and make a note of the Authoring Key for the next step.

### Clone the Repo

The first step is to clone the [Microsoft Conversational AI GitHub Repo](https://github.com/Microsoft/AI). You'll find the Virtual Assistant solution within the `solutions\Virtual-Assistant` folder.

Once the Solution has been cloned you will see the following folder structure.

    | - Virtual-Assistant
        | - Assistant
        | - LinkedAccounts
        | - Microsoft.Bot.Solutions
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
        | - Tests
      | - VirtualAssistant.sln

### Build the Solution

Once cloned the next step is to build the VirtualAssistant solution within Visual Studio. Deployment must have been completed before you can run the project due to this stage creating key dependencies in Azure along with your configured .bot file.

## Deployment

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

To deploy your Virtual Assistant including all dependencies - e.g. CosmosDb, Application Insights, etc. run the following command from a command prompt within your project folder. Ensure you update the authoring key from the previous step and choose the Azure datacenter location you wish to use (e.g. westus or westeurope). You must check that the LUIS authoring key retrieved on the previous step is for the region you specify below (e.g. westus for luis.ai or westeurope for eu.luis.ai)

Run this PowerShell script to deploy your shared resources and LUIS and QnA Maker resources in English. Ensure you navigate in a command prompt to the `solutions\Virtual-Assistant\src\csharp\assistant` folder. The `pwsh.exe` is the new PowerShell v6 executable which should be added to your path as part of the install, if not you can find in your `ProgramFiles\PowerShell\6` directory.

> Depending on the network connection this deployment process may take 10-15 minutes before progress is shown, ensure you complete the authentication step and check back later for progress.


```shell
  pwsh.exe -ExecutionPolicy Bypass -File DeploymentScripts\deploy_bot.ps1
```
If you would like to support different languages for your scenario add the `-locales` parameter. The following languages are supported: English (en-us), Chinese (zh-cn), German (de-de), French (fr-fr), Italian (it-it), and Spanish (es-es).

```shell
  pwsh.exe -ExecutionPolicy Bypass -File DeploymentScripts\deploy_bot.ps1 -locales "en-us,zh-cn"
```

If you would like to add support for additional languages **after your initial deployment**, you can specify the `-languagesOnly` parameter to deploy only the services for the new language(s).

```
   pwsh.exe -ExecutionPolicy Bypass -File DeploymentScripts\deploy_bot.ps1 -locales "fr-fr,it-it" -languagesOnly
```

You will be prompted to provide the following parameters:
   - Name - A name for your bot and resource group. This must be **unique**.
   - Location - The Azure region for your services.
   - LUIS Authoring Key - Refer to above documentation for retrieving this key.

The msbot tool will outline the deployment plan including location and SKU. Ensure you review before proceeding.

![Deployment Confirmation](../../media/virtualassistant-deploymentplan.png)

> There is a known issue with some users whereby you might experience the following error when running deployment `ERROR: Unable to provision MSA id automatically. Please pass them in as parameters and try again`. In this situation, please browse to https://apps.dev.microsoft.com and manually create a new application retrieving the ApplicationID and Password/Secret. Run the above msbot clone services command but provide two new arguments `appId` and `appSecret` passing the values you've just retrieved.

> After deployment is complete, it's **imperative** that you make a note of the .bot file secret provided as this will be required for later steps. The secret can be found near the top of the execution output and will be in purple text.


- Update your `appsettings.json` file with the newly created .bot file name and .bot file secret.
- Run the following command and retrieve the InstrumentationKey for your Application Insights instance, then add the following `InstrumentationKey` entry to your `appsettings.json` file.

```
msbot list --bot YOURBOTFILE.bot --secret YOUR_BOT_SECRET
```

```
  {
    "botFilePath": ".\\YOURBOTFILE.bot",
    "botFileSecret": "YOUR_BOT_SECRET",
    "ApplicationInsights": {
      "InstrumentationKey": "YOUR_INSTRUMENTATION_KEY"
    }
  }
```

- Finally, edit the .bot file paths for each of your language configurations:

```
"defaultLocale": "en-us",
  "languageModels": {
    "en": {
      "botFilePath": ".\\LocaleConfigurations\\YOUR_EN_BOT_PATH.bot",
      "botFileSecret": ""
    },
    "de": {
      "botFilePath": ".\\LocaleConfigurations\\YOUR_DE_BOT_PATH.bot",
      "botFileSecret": ""
    },
    "es": {
      "botFilePath": ".\\LocaleConfigurations\\YOUR_ES_BOT_PATH.bot",
      "botFileSecret": ""
    },
    "fr": {
      "botFilePath": ".\\LocaleConfigurations\\YOUR_FR_BOT_PATH.bot",
      "botFileSecret": ""
    },
    "it": {
      "botFilePath": ".\\LocaleConfigurations\\YOUR_IT_BOT_PATH.bot",
      "botFileSecret": ""
    },
    "zh": {
      "botFilePath": ".\\LocaleConfigurations\\YOUR_ZH_BOT_PATH.bot",
      "botFileSecret": ""
    }
```

Note: update the language models for the languages that you support and feel free to remove the ones you don't support.

## Skill Configuration

The Virtual Assistant Solution is fully integrated with all available skills out of the box. Skill configuration can be found in your appSettings.json file and is detailed further in the [Adding A Skill](../../skills/csharp/README.md) documentation.

### Skill Authentication

If you wish to make use of the Calendar, Email and Task Skills you need to configure an Authentication Connection enabling uses of your Assistant to authenticate against services such as Office 365 and securely store a token which can be retrieved by your assistant when a user asks a question such as *"What does my day look like today"* to then use against an API like Microsoft Graph.

The [Add Authentication to your bot](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-tutorial-authentication?view=azure-bot-service-3.0) section in the Azure Bot Service documentation covers more detail on how to configure Authentication. However in this scenario, the automated deployment step has already created the **Azure AD v2 Application** for your Bot and you instead need to follow these instructions:

- Navigate to the Azure Portal, Click Azure Active Directory and then `App Registrations (Preview)`
- Find the Application that's been created for your Bot as part of the deployment. You can search for the application by name or ApplicationID as part of the experience but note that search only works across applications currently shown and the one you need may be on a separate page.
- Click Authentication on the left-hand navigation
  - Add a Redirect URI of type `Web` with a URI of `https://token.botframework.com/.auth/web/redirect`
  - Under Implicit Grant select `Access Tokens`
  - Click Save to apply the changes
- Click API permissions on the left-hand navigation
  - Select Add Permission to show the permissions pane
  - Select `Microsoft Graph`
  - Select Delegated Permissions and then add each of the following permissions required for the Productivity Skills:
    -  `Calendars.ReadWrite`
    -  `Contacts.Read`
    -  `Mail.ReadWrite`
    -  `Mail.Send`
    -  `Notes.ReadWrite`
    -  `People.Read`
    -  `Tasks.ReadWrite`
    -  `User.ReadBasic.All`  
 -  Click Add Permissions at the bottom to apply the changes.

Next you need to create the Authentication Connection for your Bot. Ensure you use the same combination of Scopes that you provided in the above command. The first command shown below will retrieve the appId (ApplicationId) and appPassword (Client Secret) that you need to complete this step.

The commands shown below assume you have used the deployment process and your resource group name is the same as your bot. Replace `YOUR_AUTH_CONNECTION_NAME` with the name of the Auth connection you wish to create and use that in the next step. The first step shows the ApplicationID and Secret of your Bot to help complete this step.

```shell
msbot get production --secret YOUR_SECRET

az bot authsetting create --resource-group YOUR_BOT_NAME --name YOUR_BOT_NAME --setting-name "YOUR_AUTH_CONNECTION_DISPLAY_NAME" --client-id "YOUR_APPLICATION_ID" --client-secret "YOUR_APPLICATION_PASSWORD" --service Aadv2 --parameters clientId="YOUR_APPLICATION_ID" clientSecret="YOUR_APPLICATION_PASSWORD" tenantId=common --provider-scope-string "Calendars.ReadWrite Mail.ReadWrite Mail.Send Tasks.ReadWrite Notes.ReadWrite People.Read User.ReadBasic.All Contacts.Read" 
```  

> NOTE: Take special care when running the `authsetting` commands to correctly escape special characters in your client secret key (or parameters that contain special characters).   
> 1. For **Windows command prompt**, enclose the client-secret in double quotes. 
>     - e.g. `--client-secret "!&*^|%gr%"`  
>  
> 2. For **Windows PowerShell**, pass in the client-secret  after the *Powershell* special `--%` argument. 
>     -  e.g. `--% --client-secret "!&*^|%gr%"`  
>
> 3. For MacOS or Linux, enclose the client-secret in single quotes. 
>     -  e.g. `--client-secret "!&*^|%gr%"`

The final step is to update your `.bot` file and associated Skills (in appSettings.config) with the authentication connection name. This is used by the Assistant to enable authentication prompts or use of Linked Accounts.

```shell
msbot connect generic --name "Authentication" --keys "{\"YOUR_AUTH_CONNECTION_NAME\":\"Azure Active Directory v2\"}" --bot YOURBOTFILE.bot --secret "YOUR_BOT_SECRET" --url "portal.azure.net"
```

For PowerShell scenarios you can use the following variation to construct the Authentication connection, you can then pass `$keys` to the `--keys` argument above instead of the inline JSON.
```
$authKeyString = '{"YOUR_AUTH_CONNECTION_NAME":"Azure Active Directory v2"}'
$authKeyObject = ConvertFrom-Json $authKeyString
$keys = ConvertTo-Json -InputObject $authKeyObject
```

> Other Authentication Service Providers exist including the ability to create custom oAuth providers. `az bot authsetting list-providers` is a quick way to review the pre-configured ones.

## Testing
Once deployment is complete, you can start debugging through the following steps:
- Start a Debugging session within Visual Studio for the Virtual Assistant project
- Open the [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator). 
- Within the Emulator, choose Open Bot from the File menu and navigate to the .bot file in your directory which was created in the deployment step and if prompted provide the Bot File Secret
- Choose the `Development` endpoint and you should see the Introduction Card after a short delay at which point you can start using your assistant.

To use the Production Endpoint you will need to publish your Assistant to Azure:
- In Visual Studio, Right Click the Virtual Assistant solution and Click Publish.
- Click New Profile and Choose Select Existing on App Service
- Find the Resource Group for your Bot and choose the App Service (not the qnahost suffixed service)
- Complete Publishing
- Choose the `Production` endpoint within the Emulator. 

> **IMPORTANT NOTES**  
> 1. Ensure you have the latest emulator installed and update the development endpoint to reflect the port number that Visual Studio chooses when you start debugging otherwise you'll receive connection errors. 
> 2. Ensure you have [ngrok](https://ngrok.com/download) downloaded and the path to the executable path configured correctly in the emulator for the demo to work. 
>     - From the emulator edit the emulator settings: Gear icon bottom left of the emulator. 
>     - Browse or enter the path to the ngrok exe. 
>     - Enter a locale based on the language(s) deployed.

You should see an Introduction Adaptive card and the example on-boarding process will start.

See the [Testing](./testing.md) documentation for information on how to test your Virtual Assistant.
