---
category: Skills
subcategory: Samples
title: Email Skill
description: Add email capabilities to your Assistant. Powered by Microsoft Graph and Google.
order: 2
toc: true
---

# {{ page.title }}
{:.no_toc}

{{ page.description }}

## Supported scenarios
{:.toc}

The following scenarios are currently supported by the Skill:

- Send an Email
  - *Send an email to John Smith*
  - *Send an email to Harold about the team lunch this Tuesday*
- Find Email
  - *Find email from John Smith*
  - *What email do I have*
- Check Messages
  - *Do I have any new mail*
  - *Check my email*
- Delete
  - *Delete an email*
  - *put the email in the recycle bin*
- Forward
  - *Forward email from megan to alex*
  - *Could you forward this message*
- Read Aloud
  - *Read email from Philippe*
  - *Read unread email*
- Reply to an Email
  - *Reply with I will call you back*
  - *Respond to my email*
- Select an Email
  - *The third search result please*
  - *Open this one*

## Language Understanding
{:.toc}

LUIS models for the Skill are provided in **.lu** file format as part of the Skill. Further languages are being prioritized.

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
{:.no_toc}

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
{:.toc}

### Deployment
{:.no_toc}
Learn how to [provision your Azure resources]({{site.baseurl}}/skills/tutorials/create-skill/csharp/4-provision-your-azure-resources/) in the Create a Skill tutorial.

### Supported content providers
{:.no_toc}
> Office 365 and Outlook.com through the Microsoft Graph is supported along with support for Google accounts.

### Authentication connection Settings
{:.no_toc}

#### Office 365

This skill uses the following authentication scopes:

- **User.ReadBasic.All**
- **Mail.ReadWrite**
- **Mail.Send**
- **People.Read**
- **Contacts.Read**

You must use [these steps]({{site.baseurl}}/{{site.data.urls.SkillManualAuth}}) to manually configure Authentication for the Email Skill. Due to a change in the Skill architecture this is not currently automated. 

> Ensure you configure all of the scopes detailed above.

#### Google Account

To use Google account skill you need follow these steps:
1. Enable Gmail API in [Google API library](https://console.developers.google.com/apis/library)
1. Create your Gmail API credential in [Google developers console](https://console.developers.google.com/apis/credentials).
    1. Choose "Create credential" - "OAuth Client ID"
    1. Choose "Web Application"
    1. Set Redirect URL as **https://token.botframework.com/.auth/web/redirect**
1. Create an OAuth connection setting in your Web App Bot.
    - Connection name: **googleapi**
    - Service Provider: **Google**
    - Client id and secret are generated in step 2
    - Scopes: **https://mail.google.com/ https://www.googleapis.com/auth/contacts**.
1. Add the connection name, client id, secret and scopes in the **appsetting.json** file.

## Events
{:.toc}

Learn how to use [events]({{site.baseurl}}/virtual-assistant/handbook/events) to send backend data to a Skill, like a user's location or time zone.

## Download a transcript
{:.toc}

<a class="btn btn-primary" href="{{site.baseurl}}/assets/transcripts/skills-email.transcript">Download</a>
