---
category: Reference
subcategory: Skills
title: Email Skill
description: Add email capabilities to your Assistant. Powered by Microsoft Graph and Google.
order: 9
---

# {{ page.title }}
{:.no_toc}

## In this reference
{:.no_toc}

* 
{:toc}

## Overview
The Email Skill provides email related capabilities to a Virtual Assistant.

## Supported scenarios

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

## Language Understanding (LUIS)

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
|Attachment| Simple entity matching attachments|
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
### Deployment
Learn how to [provision your Azure resources]({{site.baseurl}}/tutorials/csharp/create-skill/4_provision_your_azure_resources/) in the Create a Skill tutorial.

### Supported content providers
> Office 365 and Outlook.com through the Microsoft Graph is supported along with support for Google accounts.

To use Google account skill you need follow these steps:
1. Create your calendar API credential in [Google developers console](https://console.developers.google.com). 
2. Create an OAuth connection setting in your Web App Bot.
    - Connection name: `googleapi`
    - Service Provider: `Google`
    - Client id and secret are generated in step 1
    - Scopes: `https://www.googleapis.com/auth/calendar https://www.googleapis.com/auth/contacts`.
3. Add the connection name, client id, secret and scopes in the `appsetting.json` file.

### Authentication connection Settings
If you plan to use the skill as part of a Virtual Assistant the process of registering a skill with your Virtual Assistant will create the supporting authentication connection information automatically for your Virtual Assistant. This skill uses the following authentication scopes which are registered automatically:
- `User.ReadBasic.All`
- `Mail.ReadWrite`
- `Mail.Send`
- `People.Read`
- `Contacts.Read`

**However**, if you wish to use the Skill directly without using a Virtual Assistant please use the following steps to manually configure Authentication for the Calendar Skill. This is **not** required when using the Skill with a Virtual Assistant.

Follow the general instructions [here]({{site.baseurl}}/howto/skills/manualauthsteps) to configure this using the scopes shown above.

## Events
Learn how to use [events]({{site.baseurl}}/reference/virtual-assistant/events) to send backend data to a Skill, like a user's location or time zone.