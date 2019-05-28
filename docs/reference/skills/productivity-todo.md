# ToDo Skill (Productivity)

The ToDo Skill provides ToDo related capabilities to a Virtual Assistant.
The most common scenarios have been implemented in this beta release, with additional scenarios in development.

### Supported Sources

> Office 365 and Outlook.com through the Microsoft Graph is supported at this time. Additional sources will be coming in a future release.

## Supported Scenarios

The following scenarios are currently supported by the Skill:

- Add a Task
  - *Add some items to the shopping notes*
  - *Put milk on my grocery list*
  - *Create task to meet Leon after 5:00 PM*
- Find Tasks
  - *What tasks do I have*
  - *Browse my groceries*
  - *Show my to do list*
- Delete Tasks
  - *Remove "salad vegetables" from my grocery list*
  - *Remove my to do to "pick up Tom at 6 AM"*
  - *Remove all tasks*
- Mark Tasks as Complete
  - *Mark the task "get some food" as complete*
  - *Task completed "reserve a restaurant for anniversary"*
  - *Check off "bananas" on my grocery list*

## Skill Deployment

The ToDo Skill require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding

> Review the pricing and terms for the services and adjust to suit your scenario.

To deploy your services using the default configuration, follow the steps in this common [deployment documentation page](/docs/tutorials/assistantandskilldeploymentsteps.md) from the folder where your have cloned the GitHub repo.

### Authentication Connection Settings

If you plan to use the skill as part of a Virtual Assistant the process of registering a skill with your Virtual Assistant will create the supporting authentication connection information automatically for your Virtual Assistant. This skill uses the following authentication scopes which are registered automatically:
- `Notes.ReadWrite` 
- `User.ReadBasic.All`
- `Tasks.ReadWrite`

**However**, if you wish to use the Skill directly without using a Virtual Assistant please use the following steps to manually configure Authentication for the ToDo Skill. This is **not** required when using the Skill with a Virtual Assistant.

If you wish to make use of the Calendar, Email and Task Skills you need to configure an Authentication Connection enabling uses of your Assistant to authenticate against services such as Office 365 and securely store a token which can be retrieved by your assistant when a user asks a question such as *"What does my day look like today"* to then use against an API like Microsoft Graph.

The [Add Authentication to your bot](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-tutorial-authentication?view=azure-bot-service-3.0) section in the Azure Bot Service documentation covers more detail on how to configure Authentication. However in this scenario, the automated deployment step has already created the **Azure AD v2 Application** for your Bot and you instead need to follow these instructions:

- Navigate to the Azure Portal, Click Azure Active Directory and then `App Registrations`
- Find the Application that's been created for your Bot as part of the deployment. You can search for the application by name or ApplicationID as part of the experience but note that search only works across applications currently shown and the one you need may be on a separate page.
- Click API permissions on the left-hand navigation
  - Select Add Permission to show the permissions pane
  - Select `Microsoft Graph`
  - Select Delegated Permissions and then add each of the following permissions required for the Productivity Skills:
    - `Notes.ReadWrite` 
    - `User.ReadBasic.All`
    - `Tasks.ReadWrite`
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
    - Set scopes to `Notes.ReadWrite, User.ReadBasic.All, Tasks.ReadWrite`

Finally, open the  `appSettings.config` file for your ToDo Skill and update the connection name to match the one provided in the previous step.

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

|Supported Languages |
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
|AddToDo| Matches queries to add ToDo items to a list |
|ShowToDo| Matches queries to show ToDo items or lists |
|MarkToDo| Matches queries to toggle a ToDo item |
|DeleteToDo| Matches queries to delete a ToDo item |

### Entities

|Name|Description|
|-|-|
|ContainsAll| Simple entity matching a query specifying "all" |
|FoodOfGrocery| List entity matching grocery items |
|ListType| Simple entity matching lists like "grocery", "shopping", etc. |
|ShopContent| Pattern.any entity|
|ShopVerb| List entity matching verbs like "buy", "purchase", etc. |
|TaskContentML| Simple entity matching complex items on a ToDo list |
|TaskContentPattern| Pattern.any |
|number| Prebuilt entity|
|ordinal| Prebuilt entity|
