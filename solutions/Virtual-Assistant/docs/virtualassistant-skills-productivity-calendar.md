# Virtual Assistant Skills - Productivity (Calendar)

## Overview
The Calendar Skill provides Calendar related capabilities to a Virtual Assistant. The most common scenarios have been implemented in this first release with additional scenarios in development.

## Supported Scenarios
The following scenarios are currently supported by the Skill:

- Show meeting summary - e.g "What's in my calendar"
- Next Meeting - e.g. "what's my next meeting"
- Create a meeting - e.g. Book a meeting
- Update a meeting - e.g. Update meeting
- Delete a meeting - e.g. Delete a meeting

## Supported Sources

> Office 365 and Outlook.com through the Microsoft Graph is supported at this time. Google support will be coming in the next release.
> Tasks currently use a OneNote page, Outlook Task support via the Microsoft Graph is coming in the next release. 

## Authentication Connection Settings

Your Authentication Connection and corresponding Application Registration should have the following Scopes added:

- User.Read
- Calendars.ReadWrite
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
    "Name": "Calendar",
    "DispatcherModelName": "l_Calendar",
    "Description": "The Calendar Skill adds Email related capabilities to your Custom Assitant",
    "Assembly": "CalendarSkill.CalendarSkill, CalendarSkill, Version=1.0.0.0, Culture=neutral",
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

- ChangeCalendarEntry
- CheckAvailability
- ConnectToMeeting
- ContactMeetingAttendees
- CreateCalendarEntry
- DeleteCalendarEntry
- FindCalendarDetail
- FindCalendarEntry
- FindCalendarWhen
- FindCalendarWhere
- FindCalendarWho
- FindDuration
- FindMeetingRoom

The following secondary level intents (used as part of the above scenarios) are available:

- Confirm
- GoBack
- Reject
- ShowNext
- ShowPrevious
- TimeRemaining