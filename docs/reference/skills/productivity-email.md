# Email Skill (Productivity)

The Email Skill provides Email related capabilities to a Virtual Assistant.
The most common scenarios have been implemented in this beta release, with additional scenarios in development.

## Table of Contents

- [Supported Scenarios](#supported-scenarios)
- [Language Model](#language-model)
- [Configuration](#configuration)

## Supported Sources

> Office 365 and Outlook.com through the Microsoft Graph is supported along with support for Google accounts.

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

> Only required if you wish to use the Skill directly and not as part of a Virtual Assistant.

**However**, if you wish to use the Skill directly without using a Virtual Assistant please use the following steps to manually configure Authentication for the Email Skill. This is **not** required when using the Skill with a Virtual Assistant.

If you wish to make use of the Calendar, Email and Task Skills you need to configure an Authentication Connection enabling uses of your Assistant to authenticate against services such as Office 365 and securely store a token which can be retrieved by your assistant when a user asks a question such as *"What does my day look like today"* to then use against an API like Microsoft Graph.

The [Add Authentication to your bot](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-tutorial-authentication?view=azure-bot-service-3.0) section in the Azure Bot Service documentation covers more detail on how to configure Authentication. However in this scenario, the automated deployment step has already created the **Azure AD v2 Application** for your Bot and you instead need to follow these instructions:

- Navigate to the Azure Portal, Click Azure Active Directory and then `App Registrations`
- Find the Application that's been created for your Bot as part of the deployment. You can search for the application by name or ApplicationID as part of the experience but note that search only works across applications currently shown and the one you need may be on a separate page.
- Click API permissions on the left-hand navigation
  - Select Add Permission to show the permissions pane
  - Select `Microsoft Graph`
  - Select Delegated Permissions and then add each of the following permissions required for the Productivity Skills:
    - `User.ReadBasic.All`
    - `Mail.ReadWrite`
    - `Mail.Send`
    - `People.Read`
    - `Contacts.Read`
 -  Click Add Permissions at the bottom to apply the changes.

Next you need to create the Authentication Connection for your Bot. Within the Azure Portal, find the `Web App Bot` resource created when your deployed your Bot and choose `Settings`. 

- Scroll down to the oAuth Connection settings section.
- Click `Add Setting`
- Type in the name of your Connection Setting - e.g. `Outlook`
- Choose `Azure Active Directory v2` from the Service Provider drop-down
- Open the `appSettings.config` file for your Skill
    - Copy/Paste the value of `microsoftAppId` into the ClientId setting
    - Copy/Paste the value of `microsoftAppPassword` into the Client Secret setting
    - Set Tenant Id to common
    - Set scopes to `User.ReadBasic.All Mail.ReadWrite Mail.Send People.Read Contacts.Read`

Finally, open the  `appSettings.config` file for your Email Skill and update the connection name to match the one provided in the previous step.

```
"oauthConnections": [
    {
      "name": "Outlook",
      "provider": "Azure Active Directory v2"
    }
  ],
```

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

### Example Skill Manifest

```
TBC
```