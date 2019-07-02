# Email Skill (Productivity)

The Email Skill provides Email related capabilities to a Virtual Assistant.
The most common scenarios have been implemented in this beta release, with additional scenarios in development.

## Table of Contents

- [Supported Sources](#supported-sources)
- [Supported Scenarios](#supported-scenarios)
- [Scenario Configurations](#scenario-configurations)
- [Skill Deployment](#skill-deployment)
- [Language Model](#language-model))

## Supported Sources

> Office 365 and Outlook.com through the Microsoft Graph is supported along with support for Google accounts.

To use Google account in skill you need to follow these steps:
1. Create your Gmail API credential in [Google developers console](https://console.developers.google.com). 
2. Create an OAuth connection setting in your Web App Bot.
    - Connection name: `googleapi`
    - Service Provider: `Google`
    - Client id and secret are generated in step 1
    - Scopes: `"https://mail.google.com/ https://www.googleapis.com/auth/contacts"`.
3. Add the connection name, client id, secret and scopes in appsetting.json file.

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

## Scenario Configurationsn

In dialogs such as `Send Email`, the user needs to provide information such as sibject and  content before able to send an email. Depending on the user context (e.g. Speech driven and whilst driving) you can configure default slot-filling to minimise the number of questions - e.g. `Send an email to alex about the exec review saying can you send me the deck from last week` would send email with no further prompts beyond confirmation.

This behaviour can be configured in the Skill `appsettings.json` by setting `isSkipByDefault` to true, and modify `EmailSubject` and `EmailMessage` default values in `EmailCommonStrings.resx` to set default value of different locales.

```json
"defaultValue": {
    "sendEmail": [
        {
            "name": "EmailSubject",
            "isSkipByDefault": false
        },
        {
            "name": "EmailMessage",
            "isSkipByDefault": false
        }
    ]
 }
```

## Skill Deployment

The Email Skill require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding

> Review the pricing and terms for the services and adjust to suit your scenario.

To deploy your services using the default configuration, follow the steps in this common [deployment documentation page](/docs/tutorials/assistantandskilldeploymentsteps.md) from the folder where your have cloned the GitHub repo.

### Authentication Connection Settings

If you plan to use the skill as part of a Virtual Assistant the process of registering a skill with your Virtual Assistant will create the supporting authentication connection information automatically for your Virtual Assistant. This skill uses the following authentication scopes which are registered automatically:
- `User.ReadBasic.All`
- `Mail.ReadWrite`
- `Mail.Send`
- `People.Read`
- `Contacts.Read`

**However**, if you wish to use the Skill directly without using a Virtual Assistant please use the following steps to manually configure Authentication for the Email Skill. This is **not** required when using the Skill with a Virtual Assistant.

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
