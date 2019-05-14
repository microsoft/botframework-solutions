# ToDo Skill (Productivity)

The ToDo Skill provides ToDo related capabilities to a Virtual Assistant.
The most common scenarios have been implemented in this beta release, with additional scenarios in development.

## Table of Contents

- [Supported Scenarios](#supported-scenarios)
- [Language Model](#language-model)
- [Configuration](#configuration)

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

## Configuration

### Supported Sources

> Office 365 and Outlook.com through the Microsoft Graph is supported at this time. Additional sources will be coming in a future release.

### Skill Deployment

The ToDo Skill require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding

> Review the pricing and terms for the services and adjust to suit your scenario.

To deploy your services using the default configuration, follow the steps in this common [deployment documentation page](/docs/tutorials/assistantandskilldeploymentsteps.md) from the folder where your have cloned the GitHub repo.

### Authentication Connection Settings

Your Authentication Connection and corresponding Application Registration should have the following Scopes added, these will be added automatically as part of Skill configuration where possible.

- `Notes.ReadWrite`

### Example Skill Manifest

```
TBC
```