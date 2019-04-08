# Email Skill (Productivity)
The Email Skill provides Email related capabilities to a Virtual Assistant. 
The most common scenarios have been implemented in this beta release, with additional scenarios in development.

## Table of Contents
- [Supported Scenarios](#supported-scenarios)
- [Language Model](#language-model)
- [Configuration](#configuration)

## Supported Scenarios
The following scenarios are currently supported by the Skill:

- Send an Email
    - *Send an email to John Smith*
    - *Send an email*
- Find Email
    - *Find email from John Smith*
    - *What email do I have*
 - Add Flag
   - *This email needs to be flagged*
   - *Add a flag to the email Simone Jones just sent to me*
 - Check Messages
   - *Do I have any new mail*
   - *Check my email*
 - Delete 
   - *Do I have any new mail*
   - *Check my email*
 - Forward
   - *Forward all files from Petrina to Jim*
   - *Could you forward this message to Cosmo my email*
 - Query Last Text
   - *Who emailed me last*
   - *What was the last email I got from Dad*
 - Read Aloud
   - *Read the last email from Philippe*
   - *Read unread email* 
 - Reply to an Email
   - *Reply with "I will call you back"*
   - *Respond to my last email*
 - Select an Email
   - *The third search result please*
   - *Open this one*


## Language Model
LUIS models for the Skill are provided in .LU file format as part of the Skill. Further languages are being prioritized.

|Supported Languages|
|-|
|English| 
|French| 
|Italian| 
|German| 
|Spanish| 
|Chinese (simplified)|

### Intents
|Name|Description|
|-|-|
|AddFlag| Matches queries to flag an email as important |
|CheckMessages| Matches queries to check for new messages |
|Delete| Matches queries to delete an email |
|Forward| Matches queries to forward an email |
|QueryLastText| Matches queries to find the most recent emails |
|ReadAloud| Matches queries to read an email aloud |
|Reply| Matches queries to reply to an email|
|SearchMessages| Matches queries to search for specific emails |
|SelectItem| Matches queries to select an email |
|SendEmail| Matches queries to send an email |

### Entities
|Name|Description|
|-|-|
|Attachment| Simple entity matching attenchments|
|Category| Simple entity matching categories|
|ContactName| Simple entity matching contact names|
|Date| Simple entity matching the date|
|EmailAddress| Simple entity matching email addresses|
|EmailPlatform| Simple entity matching email platforms|
|EmailSubject| Simple entity matching email subjects|
|FromRelationshipName| Simple entity|
|Line| Simple entity matching message message lines|
|Message| Simple entity matching messages |
|OrderReference| Simple entity |
|PositionReference| Simple entity|
|RelationshipName| Simple entity matching contact relationships|
|SearchTexts| Simple entity matching messages to search through|
|SenderName| Simple entity matching a sender's name|
|Time| Simple entity matching the time|
|datetimeV2| Prebuilt entity|
|number| Prebuilt entity|
|ordinal| Prebuilt entity|

## Configuration

### Supported Sources

> Office 365 and Outlook.com through the Microsoft Graph is supported along with support for Google accounts.

### Auth Connection Settings

Your Authentication Connection and corresponding Application Registration should have the following Scopes added:

- `User.Read`
- `Mail.Read`
- `Mail.Send`
- `People.Read`

### Skill Parameters
The following Parameters are accepted by the Skill and enable additional personalisation of responses to a given user:
- `IPA.Timezone`

Read [Handling Events With Your Virtual Assistant](../../virtual-assistant/csharp/events.md) to learn how to manage events within a Skill.

### Configuration File Information
The following Configuration entries are required to be passed to the Skill and are provided through the Virtual Assistant appSettings.json file.

- `LuisAppId`
- `LuisSubscriptionKey`
- `LuisEndpoint`

### Example Skill Registration Entry
```
{
    "Name": "Email",
    "DispatcherModelName": "l_Email",
    "Description": "The Email Skill adds Email related capabilities to your Custom Assitant",
    "Assembly": "EmailSkill.EmailSkill, EmailSkill, Version=1.0.0.0, Culture=neutral",
    "AuthConnectionName": "AzureADConnection",
    "Parameters": [
    "IPA.Timezone"
    ],
    "Configuration": {
    "LuisAppId": "YOUR_LUIS_APP_ID",
    "LuisSubscriptionKey": "YOUR_LUIS_SUBSCRIPTION_KEY",
    "LuisEndpoint": "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/"
    }
}
```

### Deploying the Skill in local-mode

The Email skill is added by default when deploying the Virtual Assistant, however if you want to install as a standalone bot for development/testing following the steps below.

Run this PowerShell script from the Email skill directory to deploy shared resources and LUIS models.

```
  pwsh.exe -ExecutionPolicy Bypass -File DeploymentScripts\deploy_bot.ps1
```

You will be prompted to provide the following parameters:
   - Name - A name for your bot and resource group. This must be **unique**.
   - Location - The Azure region for your services (e.g. westus)
   - LUIS Authoring Key - Refer to [this documentation page](../../virtual-assistant/csharp/gettingstarted.md) for retrieving this key.

The msbot tool will outline the deployment plan including location and SKU. Ensure you review before proceeding.

> After deployment is complete, it's **imperative** that you make a note of the .bot file secret provided as this will be required for later steps. The secret can be found near the top of the execution output and will be in purple text.

- Update your `appsettings.json` file with the newly created .bot file name and .bot file secret.
- Run the following command and retrieve the InstrumentationKey for your Application Insights instance and update `InstrumentationKey` in your `appsettings.json` file.

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

- Finally, add the .bot file paths for each of your language configurations

```
"defaultLocale": "en-us",
  "languageModels": {
    "en": {
      "botFilePath": ".\\LocaleConfigurations\\YOUR_EN_BOT_PATH.bot",
      "botFileSecret": ""
    }
    }
```

Once you have followed the deployment instructions above, open the provided .bot file with the Bot Framework Emulator.