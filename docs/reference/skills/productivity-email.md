# Email Skill (Productivity)
The Email Skill provides Email related capabilities to a Virtual Assistant. 
The most common scenarios have been implemented in this beta release, with additional scenarios in development.

## Table of Contents
- [Supported Scenarios](#supported-scenarios)
- [Language Model](#language-model)
- [Configuration](#configuration)

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
|Attachment| Simple entity matching attenchments|
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

### Supported Sources

> Office 365 and Outlook.com through the Microsoft Graph is supported along with support for Google accounts.

### Skill Deployment

The Email Skill require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding

> Review the pricing and terms for the services and adjust to suit your scenario.

To deploy your services using the default configuration, follow the steps in this common [deployment documentation page](/docs/virtual-assistant/common/deploymentsteps.md) from the folder where your have cloned the GitHub repo.

### Authentication Connection Settings

Your Authentication Connection and corresponding Application Registration should have the following Scopes added, these will be added automatically as part of Skill configuration where possible.

- `User.Read`
- `Mail.Read`
- `Mail.Send`
- `People.Read`


### Example Skill Manifest

```
TBC
```