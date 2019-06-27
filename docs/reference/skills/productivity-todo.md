# ToDo Skill (Productivity)

The ToDo Skill provides ToDo related capabilities to a Virtual Assistant. The most common scenarios have been implemented in this initial release, with additional scenarios in development.

## Table of Contents

- [Supported Sources](#supported-sources)
- [Supported Scenarios](#supported-scenarios)
- [Skill Deployment](#skill-deployment)
- [Language Model](#language-model)

## Supported Sources

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
- `Mail.Send`

**However**, if you wish to use the Skill directly without using a Virtual Assistant please use the following steps to manually configure Authentication for the Calendar Skill. This is **not** required when using the Skill with a Virtual Assistant.

Follow the general instructions [here](/docs/reference/skills/manualauthsteps.md) to configure this using the scopes shown above.

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


## Add Your Own List Type

If you want to add your customized list types, for example, your homework list or movie list, please follow these steps:

1.Add your list type to `appsettings.json`

```json
"customizeListTypes": [
    "Homework",
    "Movie"
  ]
```

2.Add your list type name and its synonym in `Responses\Shared\ToDoString.resx`

Name | Value |
---- | ----- |
Homework | Homework |
HomeworkSynonym | homework, home work |

3.Modify your LUIS file. Modify `Deployment\Resources\LU\en\todo.lu` so that your LUIS app can tell these new ListType entities. You can provide more utterance to make your LUIS model perform better.

```diff
## AddToDo
+ - add {TaskContent=History} to my {ListType=homework} list
+ - add {TaskContent=Math} to my {ListType=homework}
```

(Optional) If you want to surport multi languages, please modify corresponding `.resx` files and `.lu` files, such as `Deployment\Resources\LU\zh\todo.lu`.

```diff
## AddToDo
+ - 在{ListType=作业}列表里加上{TaskContent=数学}
```

(Optional) After you add new list type, you can modify prompts as needed to make your conversation more friendly. For example, you can modify `Responses\Main\ToDoMainResponses.json`:

```diff
"ToDoWelcomeMessage": {
    "replies": [
      {
+       "text": "Hi. I'm To Do bot. I can help you manage your To Do, Shopping, Grocery or Homework list."
-       "text": "Hi. I'm To Do bot. I can help you manage your To Do, Shopping or Grocery list."
      }
    ]
```

4.Redeploy your ToDo Skill.
