---
category: Skills
subcategory: Samples
title: Calendar Skill
description: Add calendar capabilities to your Assistant. Powered by Microsoft Graph and Google.
order: 1
toc: true
---

# {{ page.title }}
{:.no_toc}

{{ page.description }}

## Supported scenarios
{:.toc}

The following scenarios are currently supported by the [Calendar Skill](https://github.com/microsoft/botframework-skills/tree/main/skills/csharp/calendarskill):

- Accept a Meeting
  - *I'll attend the meeting this afternoon*
  - *Accept the event sent by Yolanda Wong*
- Change an Event
  - *Bring forward my 4:00 appointment two hours*
  - *Reschedule my interview to Tuesday at 1 PM*
- Connect to a Meeting
  - *Connect me to conference call*
  - *Connect me with my 2 o'clock meeting*
- Create a Meeting
  - *Create a meeting tomorrow at 9 AM with Lucy Chen*
  - *Put anniversary on my calendar*
- Delete a Meeting
  - *Cancel my meeting at 3 PM today*
  - *Drop my appointment for Monday*
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
- Find a Meeting Room
  - *Find a meeting room at 3 PM*
  - *Is the room 325 open right now?*
- Time Remaining
  - *How long until my next meeting?*
  - *How many minutes free do I have before next scheduled appointment?*

## Language Understanding
{:.toc}

LUIS models for the Skill are provided in `.lu` file format as part of the Skill. Further languages are being prioritized.

|Supported Languages|
|-|
|English|
|French|
|Italian|
|German|
|Spanish|
|Chinese (simplified)|

### Intents
{:.no_toc}

|Name|Description|
|-|-|
|AcceptEventEntry| Matches queries to accept an event|
|AddCalendarEntryAttribute| Matches queries to add a calendar entry attribute|
|CancelCalendar| Matches queries to cancel an action of the calendar|
|ChangeCalendarEntry| Matches queries to change an event|
|CheckAvailability| Matches queries to check a contact's or a meetingroom's availability|
|ConnectToMeeting| Matches queries to connect to a meeting|
|ContactMeetingAttendees| Matches queries to contact the attendees of a meeting|
|CreateCalendarEntry| Matches queries to create a calendar entry|
|DeleteCalendarEntry| Matches queries to delete a calendar entry|
|FindCalendarDetail| Matches queries to find a calendar entry with details|
|FindCalendarEntry| Matches queries to find a calendar entry|
|FindCalendarWhen| Matches queries to get the time of a meeting|
|FindCalendarWhere| Matches queries to get the location of a meeting|
|FindCalendarWho| Matches queries to get the attendees of a meeting|
|FindDuration| Matches queries to find out how long a meeting is|
|FindMeetingRoom| Matches queries to find a meeting room|
|RejectCalendar| Matches queries to reject an action of the calendar|
|ShowNextCalendar| Matches queries to show the next events of the calendar|
|ShowPreviousCalendar| Matches queries to show previous events of the calendar|
|TimeRemaining| Matches queries to get the time until a meeting begins|

### Entities
{:.no_toc}

|Name|Description|
|-|-|
|AskParameter| Simple entity|
|Building| Simple entity|
|ContactName| Simple entity|
|DestinationCalendar| Simple entity|
|Duration| Simple entity|
|FromDate| Simple entity|
|FloorNumber| Simple entity|
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
|SlotAttributeName| List entity|
|AfterAny| Pattern.Any entity|
|MeetingRoomPatternAny| Pattern.Any entity|
|MeetingRoomKeywordsDesc| RegEx entity|

## Configuration
{:.toc}

### Deployment
{:.no_toc}
Learn how to [provision your Azure resources]({{site.baseurl}}/skills/tutorials/create-skill/csharp/4-provision-your-azure-resources/) in the Create a Skill tutorial.

### Supported content providers
{:.no_toc}

> Office 365 and Outlook.com through the Microsoft Graph is supported along with support for Google accounts.

### Authentication connection settings
{:.no_toc}

#### Office 365

This skill uses the following authentication scopes:

- **User.ReadBasic.All**  
- **Calendars.ReadWrite**
- **People.Read**    
- **Contacts.Read**

You must use [these steps]({{site.baseurl}}/{{site.data.urls.SkillManualAuth}}) to manually configure Authentication for the Calendar Skill. Due to a change in the Skill architecture this is not currently automated.

> Ensure you configure all of the scopes detailed above.

#### Google Account

To use a Google account follow these steps:
1. Enable Calendar API in [Google API library](https://console.developers.google.com/apis/library)
1. Create your calendar API credential in [Google developers console](https://console.developers.google.com/apis/credentials).
    1. Choose "Create credential" - "OAuth Client ID"
    1. Choose "Web Application"
    1. Set Redirect URL as **https://token.botframework.com/.auth/web/redirect**
1. Create an OAuth connection setting in your Web App Bot.
    - Connection name: **googleapi**
    - Service Provider: **Google**
    - Client id and secret are generated in step 2
    - Scopes: **https://www.googleapis.com/auth/calendar https://www.googleapis.com/auth/contacts**.
1. Add the connection name, client id, secret and scopes in the **appsetting.json** file.

## Meeting Room Booking Support

The Calendar skill provides additional support to search and book meeting rooms. Due to search limitations in Microsoft Graph limiting the experience we leverage Azure Search to provide fuzzy meeting room name matching, floor level, etc. 

1. To simplify the process of extracting your meeting room data and inserting into Azure Search we have provided an example PowerShell script. However, you should ensure that `displayName`, `emailAddress`, `building` and `floorNumber` are populated within your Office 365 tenant (example below). You can do this through the [Graph Explorer](https://developer.microsoft.com/en-us/graph/graph-explorer/preview) using the query shown below, this information is required for the Meeting Room booking experience.

    `https://graph.microsoft.com/beta/places/microsoft.graph.room`
    ```json
    {
      "value": [
          {
              "id": "94a7966e-b7f8-4466-b0c7-435251dab6eb",
              "displayName": "London ConfRoom Excalibur",
              "emailAddress": "Excalibur@ContosoVirtualAssist.onmicrosoft.com",
              "building": "4",
              "floorNumber": 2,
          },
          {
              "id": "45d9a9b4-ca7f-4cc5-aa50-f4a151bed172",
              "displayName": "London ConfRoom Enterprise",
              "emailAddress": "Enterprise@ContosoVirtualAssist.onmicrosoft.com",
              "building": "4",
              "floorNumber": 1
          },
      ]
    }
    ```

1. In the **Azure Portal**, Configure the settings of your registered Calendar Skill app at **Azure Active Directory** > **App registrations**
    - This app will request the permission for **Place.Read.All** scope. There are two ways to grant the consent:
      1. In the **API permissions** tab, add a permission for **Place.Read.All** scope, and grant admin consent for your organization.
      2. Make sure your account has permission to access your tenant's meeting room data so that you can consent on behalf of your organization in the login step, testing the previous query will validate this.
    - In the **Authentication** tab
      - Toggle **Default client type** > **Treat application as a public client** to "Yes"
      - Set **Supported account types** according to your own requirements

1. Run the following command to install the module:
    ```powershell
    Install-Module -Name CosmosDB
    ```
    
1. Run the following command:
    ```powershell
    ./Deployment/Scripts/enable_findmeetingroom.ps1
    ```

![A successful run of the Meeting Room script]({{site.baseurl}}/assets/images/calendar-meeting-room-script.png)

### What do these parameters mean? 

|Parameter|Description|Required|
|----|----|----|
|resourceGroup | An existing resource group where the Azure Search Service will be deployed. | Yes |
|cosmosDbAccount | The account name of an existing CosmosDb deployment where the meeting room data will be stored, this will then be used as a data source by Azure Search. | Yes |
|primaryKey | The primary key of the CosmosDB deployment | Yes |
|appId | AppId of an authorised Azure AD application which can access Meeting room data | Yes |
|tenantId | The tenantId corresponding to the application. If you have set "Supported account types" as "Multitenant" and your account has a differet tenant, please use "common" | Yes|
|migrationToolPath | The local path to your data migration tool "dt.exe", e.g., "C:\Users\tools\dt1.8.3\drop". The data migration tool is a tool which can migrate your data to Azure Cosmos DB with parallel requests. This is recommended if you have a large amount of meeting room data. You can download the tool [here](https://docs.microsoft.com/en-us/azure/cosmos-db/import-data). | No |

You can access all the required parameters from the [Deployment](#deployment) step.
> When running the script, you will be asked to sign in with your account which can access the meeting room data in the MSGraph.

Follow the general instructions [here]({{site.baseurl}}/{{site.data.urls.SkillManualAuth}}) to configure this using the scopes shown above.

## Events
{:.toc}
Learn how to use [events]({{site.baseurl}}/virtual-assistant/handbook/events) to send backend data to a Skill, like a user's location or time zone.

## Download a transcript
{:.toc}

<a class="btn btn-primary" href="{{site.baseurl}}/assets/transcripts/skills-calendar.transcript">Download</a>