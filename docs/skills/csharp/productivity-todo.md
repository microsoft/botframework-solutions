# ToDo Skill (Productivity)
The ToDo Skill provides ToDo related capabilities to a Virtual Assistant. 
The most common scenarios have been implemented in this beta release, with additional scenarios in development.

## Table of Contents
- [Supported Scenarios](#supported-scenarios)
- [Language Model](#language-model)
- [Configuration](#configuration)

## Supported Scenarios
The following scenarios are currently supported by the Skill:

- Add a Task
    - *Add some items to the shopping notes*
    - *Put milk on my grocery list*
    - *Create task to meet Leon after 5:00 PM*
- Find Tasks
    - *What tasks do I have*
    - *Browse my groceries*
    - *Show my to do list*
- Delete Tasks
    - *Remove "salad vegetables" from my grocery list*
    - *Remove my to do to "pick up Tom at 6 AM"*
    - *Remove all tasks*
 - Mark Tasks as Complete
    - *Mark the task "get some food" as complete*
    - *Task completed "reserve a restaurant for anniversary"*
    - *Check off "bananas" on my grocery list*
    
## Language Model
LUIS models for the Skill are provided in .LU file format as part of the Skill. Further languages are being prioritized.

|Supported Languages |
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
|AddToDo| Matches queries to add ToDo items to a list |
|ShowToDo| Matches queries to show ToDo items or lists |
|MarkToDo| Matches queries to toggle a ToDo item |
|DeleteToDo| Matches queries to delete a ToDo item |

### Entities
|Name|Description|
|-|-|
|ContainsAll| Simple entity matching a query specifying "all" |
|FoodOfGrocery| List entity matching grocery items |
|ListType| Simple entity matching lists like "grocery", "shopping", etc. |
|ShopContent| Pattern.any entity|
|ShopVerb| List entity matching verbs like "buy", "purchase", etc. |
|TaskContentML| Simple entity matching complex items on a ToDo list |
|TaskContentPattern| Pattern.any |
|number| Prebuilt entity|
|ordinal| Prebuilt entity|

## Configuration

### Supported Sources

> Office 365 and Outlook.com through the Microsoft Graph is supported at this time. Additional sources will be coming in a future release.

### Auth Connection Settings
Your Authentication Connection and corresponding Application Registration should have the following Scopes added:

- `Notes.ReadWrite`

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
    "Name": "ToDo",
    "DispatcherModelName": "l__ToDo",
    "Description": "The ToDo Skill adds ToDo related capabilities to your Custom Assitant",
    "Assembly": "ToDoSkill.ToDoSkill, ToDoSkill, Version=1.0.0.0, Culture=neutral",
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

The ToDo skill is added by default when deploying the Virtual Assistant, however if you want to install as a standalone bot for development/testing following the steps below.

Run this PowerShell script from the ToDo skill directory to deploy shared resources and LUIS models.

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