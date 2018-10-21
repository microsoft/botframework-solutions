# Virtual Assistant Skills - Productivity (Email)

## Overview
The Email Skill provides Email related capabilities to a Virtual Assistant. The most common scenarios have been implemented in this first release with additional scenarios in development.

## Supported Scenarios
The following scenarios are currently supported by the Skill:

- Send an Email
    - Send an email to John Smith
    - Send an email 
- Find Email
    - Find email from John Smith
    - What email do I have

## Supported Sources

> Office 365 and Outlook.com through the Microsoft Graph is supported at this time. Google support will be coming in the next release.

## Auth Connection Settings

Your Authentication Connection and corresponding Application Registration should have the following Scopes added:

- User.Read
- Mail.Read
- Mail.Send
- People.Read

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

## LUIS Model Intents and Entities
LUIS models for the Skill are provided in .LU file format as part of the Skill. These are currently avaialble in English, French, Italian, German and Spanish languages. Further languages are being prioritised.

The following Top Level intents are available:

- CheckMessages
- SearchMessages
- SendEmail

The following secondary level intents (used as part of the above scenarios) are available:

- AddFlag
- AddMore
- Cancel
- Confirm
- Forward
- QueryLastText
- ReadAloud
- Reply
- SearchMessages
- SendEmail
- ShowNext