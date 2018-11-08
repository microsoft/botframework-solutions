# Virtual Assistant Skills - Productivity (ToDo)

## Overview
The ToDo Skill provides ToDo related capabilities to a Virtual Assistant. The most common scenarios have been implemented in this first release with additional scenarios in development.

## Supported Scenarios
The following scenarios are currently supported by the Skill:

- Add a Task
    - Remind me to pickup milk
    - Add task
- Find Tasks
    - What tasks do I have

## Supported Sources

> Office 365 and Outlook.com through the Microsoft Graph is supported at this time. Additional sources will be coming in a future release.

## Auth Connection Settings
Your Authentication Connection and corresponding Application Registration should have the following Scopes added:

- Notes.ReadWrite

## Skill Parameters
The following Parameters are accepted by the Skill and enable additional personalisation of responses to a given user:
- IPA.Timezone

## Configuration File Information
The following Configuration entries are required to be passed to the Skill and are provided through the Virtual Assistant appSettings.json file.

- LuisAppId
- LuisSubscriptionKey
- LuisEndpoint

## Example Skill Registration Entry
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

## LUIS Model Intents and Entities
LUIS models for the Skill are provided in .LU file format as part of the Skill. These are currently avaialble in English, French, Italian, German and Spanish languages. Further languages are being prioritised.

The following Top Level intents are available:

- AddToDo
- ShowToDo
- MarkToDo
- DeleteToDo
- ConfirmYes
- ConfirmNo

The following secondary level intents (used as part of the above scenarios) are available:

- Next
- Previous
- Confirm