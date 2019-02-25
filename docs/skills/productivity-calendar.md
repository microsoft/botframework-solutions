# Calendar Skill (Productivity)
The Calendar Skill provides Calendar related capabilities to a Virtual Assistant. The most common scenarios have been implemented in this first release with additional scenarios in development.

## Table of Contents
- [Supported Scenarios](#supported-scenarios)
- [Language Model](#language-model)
- [Configuration](#configuration)

## Supported Scenarios
The following scenarios are currently supported by the Skill:


- Accept a Meeting
  - *I'll attend the meeting this afternoon*
  - *Accept the event sent by Yolanda Wong*
- Change an Event
  - *Bring forward my 4:00 appointment two hours*
  - *Change my vacation from ending on Friday to Monday*
- Check Availability
  - *Is Debra available on Saturday?*
  - *Am I busy this weekend?*
- Connect to a Meeting
- Contact Attendees
  - Notify Noah that our meeting is pushed back 30 minutes
  - Tell Jennifer that I'm running late
- Create a Meeting
  - Create a meeting tomorrow at 9 AM with Lucy Chen
  - Put anniversary on my calendar
- Delete a Meeting
  - Cancel my meeting with Abigail at 3 PM today
  - Clear all my appointments
- Get Meeting Details
  - *Tell me about my meeting today*
  - *What are the plans for the dinner date with Stephanie?*
- Find a Meeting
  - *Do I have any appointments today?*
  - *Search for the "employee orientation" meeting*
- Find an Event by Time
  - *What day is Lego Land scheduled for?*
  - *What time is my next appointment?*
- Find an Event by Location
  - *Where is my meeting with Kayla?*
  - *Where do I need to be next?*
- Find an Event by Attendee
  - Who am I meeting at 10 AM tomorrow?
  - Who is in my next meeting?
- Find an Event's Duration
  - How long will the next meeting last?
  - What's the duration of my 4 PM meeting?
- Find a Meeting Room
  - Book a conference room
  - Can you reserve room 258 on Thursday at 1 PM?
- Calendar Navigation
  - *Show me the next meeting please*
  - *What was my previous appointment?*
- Time Remaining
  - *How long until my next meeting?*
  - *How many days are there until Thanksgiving?*

## Language Model
LUIS models for the Skill are provided in .LU file format as part of the Skill. Further languages are being prioritized.

|Supported Languages|
|-|-|
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

## Configuration

### Supported Sources

> Office 365 and Outlook.com through the Microsoft Graph is supported along with support for Google accounts.

### Authentication Connection Settings

Your Authentication Connection and corresponding Application Registration should have the following Scopes added:

- `User.Read`
- `Calendars.ReadWrite`
- `People.Read`


### Skill Parameters
The following Parameters are accepted by the Skill and enable additional personalisation of responses to a given user:
- `IPA.Timezone`

### Configuration File Information
The following Configuration entries are required to be passed to the Skill and are provided through the Virtual Assistant appSettings.json file.

- `LuisAppId`
- `LuisSubscriptionKey`
- `LuisEndpoint`

### Example Skill Registration Entry
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