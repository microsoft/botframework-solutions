# Calendar Skill (Productivity)

The Calendar Skill provides Calendar related capabilities to a Virtual Assistant.
The most common scenarios have been implemented in this beta release, with additional scenarios in development.

## Table of Contents

- [Supported Sources](#supported-sources)
- [Supported Scenarios](#supported-scenarios)
- [Scenario Configurations](#scenario-configurations)
- [Skill Deployment](#skill-deployment)
- [Language Model](#language-model)

## Supported Sources

> Office 365 and Outlook.com through the Microsoft Graph is supported along with support for Google accounts.

To use Google account in skill you need to follow these steps:
1. Create your calendar API credential in [Google developers console](https://console.developers.google.com). 
2. Create an OAuth connection setting in your Web App Bot.
    - Connection name: `googleapi`
    - Service Provider: `Google`
    - Client id and secret are generated in step 1
    - Scopes: `https://www.googleapis.com/auth/calendar https://www.googleapis.com/auth/contacts`.
3. Add the connection name, client id, secret and scopes in appsetting.json file.

## Supported Scenarios

The following scenarios are currently supported by the Skill:

- Accept a Meeting
  - *I'll attend the meeting this afternoon*
  - *Accept the event sent by Yolanda Wong*
- Change an Event
  - *Bring forward my 4:00 appointment two hours*
  - *Reschedule my interview on monday to 1 pm*
- Connect to a Meeting
  - *Connect me to conference call*
  - *Connect me with my 2 o'clock meeting*
- Create a Meeting
  - *Create a meeting tomorrow at 9 AM with Lucy Chen*
  - *Put anniversary on my calendar*
- Delete a Meeting
  - *Cancel my meeting at 3 PM today*
  - *Drop my appointment for monday*
- Find a Meeting
  - *Do I have any appointments today?*
  - *Get to my next event*
- Find an Event by Time
  - *What day is Lego Land scheduled for?*
  - *What time is my next appointment?*
- Find an Event by Location
  - *Where is my meeting with Kayla?*
  - *Where do I need to be next?*
- Find an Event by Attendee
  - *Who am I meeting at 10 AM tomorrow?*
  - *Who is in my next meeting?*
- Find an Event's Duration
  - *How long will the next meeting last?*
  - *What's the duration of my 4 PM meeting?*
- Time Remaining
  - *How long until my next meeting?*
  - *How many days are there until Thanksgiving?*

## Scenario Configurations
In dialogs such as `Create Meeting`, the user needs to provide information such as title and meeting content before able to create a meeting. Depending on the user context (e.g. Speech driven and whilst driving) you can configure default slot-filling to minimise the number of questions - e.g. `Schedule a project review meeting with alex tomorrow at 8pm` would create the meeting with a default length of 30 minutes and no meeting contents.

This behaviour can be configured in the Skill `appsettings.json` by setting `isSkipByDefault` to true, and modify `EventTitle`, `EventContent` and `EventLocation` default values in `CalendarCommonStrings.resx` to set default value of different locales.

The `EventStartDate` default value should be a integer that means date difference with today. For example, if you want to set default start date as next day of meeting created date, the default value should be `1`.

The `EventDuration` default value is the default duration expressed in minutes (number).
 
```json
"defaultValue": {
    "createMeeting": [
        {
            "name": "EventTitle",
            "isSkipByDefault": false
        },
        {
            "name": "EventContent",
            "isSkipByDefault": false
        },
        {
            "name": "EventStartDate",
            "isSkipByDefault": false,
            "defaultValue": ""
        },
        {
            "name": "EventDuration",
            "isSkipByDefault": false,
            "defaultValue": ""
        },
        {
            "name": "EventLocation",
            "isSkipByDefault": false
        }
    ]
}
```
  
## Skill Deployment

The Calendar Skill require the following dependencies for end to end operation which are created through an ARM deployment script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding

> Review the pricing and terms for the services and adjust to suit your scenario.

**To deploy your services using the default configuration, follow the steps in this common [deployment documentation page](/docs/tutorials/assistantandskilldeploymentsteps.md) from the folder where your have cloned the GitHub repo.**

### Authentication Connection Settings

If you plan to use the skill as part of a Virtual Assistant the process of registering a skill with your Virtual Assistant will create the supporting authentication connection information automatically for your Virtual Assistant. This skill uses the following authentication scopes which are registered automatically:

- `User.ReadBasic.All`  
- `Calendars.ReadWrite`
- `People.Read`    
- `Contacts.Read`

**However**, if you wish to use the Skill directly without using a Virtual Assistant please use the following steps to manually configure Authentication for the Calendar Skill. This is **not** required when using the Skill with a Virtual Assistant.

Follow the general instructions [here](/docs/reference/skills/manualauthsteps.md) to configure this using the scopes shown above.

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
|AcceptEventEntry| Matches queries to accept an event|
|ChangeCalendarEntry| Matches queries to change an event|
|CheckAvailability| Matches queries to check a contact's availability |
|ConnectToMeeting| Matches queries to connect to a meeting|
|ContactMeetingAttendees| Matches queries to contact the attendees of a meeting|
|CreateCalendarEntry| Matches queries to create a calendar entry|
|DeleteCalendarEntry| Matches queries to delete a calendar entry|
|FindCalendarDetail| Matches queries to find a calendar entry with details|
|FindCalendarEntry| Matches queries to find a calendar entry|
|FindCalendarWhen| Matches queries to get the time of a meeting|
|FindCalendarWhere| Matches queries to get the location of a meeting |
|FindCalendarWho| Matches queries  to get the attendees of a meeting|
|FindDuration| Matches queries to find out how long a meeting is|
|FindMeetingRoom| Matches queries to find a meeting room|
|GoBack| Matches queries to return to the previous step of a dialog|
|NoLocation| Matches queries to not specify a location|
|ReadAloud| Matches queries to read a calendar entry aloud |
|TimeRemaining| Matches queries to get the time until a meeting begins|

### Entities

|Name|Description|
|-|-|
|AskParameter| Simple entity|
|ContactName| Simple entity|
|DestinationCalendar| Simple entity|
|Duration| Simple entity|
|FromTime| Simple entity|
|Location| Simple entity|
|MeetingRoom| Simple entity|
|MoveEarlierTimeSpan| Simple entity|
|MoveLaterTimeSpan| Simple entity|
|OrderReference| Simple entity|
|PositionReference| Simple entity|
|RelationshipName| Simple entity|
|SlotAttribute| Simple entity|
|Subject| Simple entity|
|ToDate| Simple entity|
|ToTime| Simple entity|
|datetimeV2| Prebuilt entity|
|number| Prebuilt entity|
|ordinal| Prebuilt entity|
