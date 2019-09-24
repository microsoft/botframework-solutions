---
category: Reference
subcategory: Skills
title: To Do Skill
description: Add task management capabilities to your Assistant. Powered by Microsoft Graph.
order: 10
---

# {{ page.title }}
{:.no_toc}

## In this reference
{:.no_toc}

* 
{:toc}

## Overview
The To Do Skill provides task related capabilities to a Virtual Assistant.

## Supported scenarios

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

## Language Understanding (LUIS)

LUIS models for the Skill are provided in `.lu` file format as part of the Skill. Further languages are being prioritized.

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
|AddToDo| Matches queries to add To Do items to a list |
|ShowToDo| Matches queries to show To Do items or lists |
|MarkToDo| Matches queries to toggle a To Do item |
|DeleteToDo| Matches queries to delete a To Do item |

### Entities

|Name|Description|
|-|-|
|ContainsAll| Simple entity matching a query specifying "all" |
|FoodOfGrocery| List entity matching grocery items |
|ListType| Simple entity matching lists like "grocery", "shopping", etc. |
|ShopContent| Pattern.any entity|
|ShopVerb| List entity matching verbs like "buy", "purchase", etc. |
|TaskContentML| Simple entity matching complex items on a To Do list |
|TaskContentPattern| Pattern.any |
|number| Prebuilt entity|
|ordinal| Prebuilt entity|

## Configuration
### Deployment
Learn how to [provision your Azure resources]({{site.baseurl}}/tutorials/csharp/create-skill/4_provision_your_azure_resources/) in the Create a Skill tutorial.

### Supported content providers
> Office 365 and Outlook.com through the Microsoft Graph is supported at this time.

### Authentication connection settings
If you plan to use the skill as part of a Virtual Assistant the process of registering a skill with your Virtual Assistant will create the supporting authentication connection information automatically for your Virtual Assistant. This skill uses the following authentication scopes which are registered automatically:
- `Notes.ReadWrite` 
- `User.Read`
- `User.ReadBasic.All`
- `Tasks.ReadWrite`

**However**, if you wish to use the Skill directly without using a Virtual Assistant please use the following steps to manually configure Authentication for the Calendar Skill. This is **not** required when using the Skill with a Virtual Assistant.

Follow the general instructions [here]({{site.baseurl}}/howto/skills/manualauthsteps) to configure this using the scopes shown above.

### Add customized to do lists
If you want to add your customized list types, for example, your homework list or movie list, please follow these steps:

1. Add your list type to `appsettings.json`

	```json
	"customizeListTypes": [
	  "Homework",
	  "Movie"
	]
	```
2. Add your list type name and its synonym in `Responses/Shared/ToDoString.resx`

	Name | Value 
	---- | ----- 
	Homework | Homework 
	HomeworkSynonym | homework, home work 

3. Modify your LUIS file. Modify `Deployment/Resources/LU/en/todo.lu` so that your LUIS app can tell these new ListType entities. You can provide more utterance to make your LUIS model perform better.

	```diff
	## AddToDo
	+ - add {TaskContent=History} to my {ListType=homework} list
	+ - add {TaskContent=Math} to my {ListType=homework}
	```

	(Optional) If you want to support multiple languages, please modify corresponding `.resx` files and `.lu` files, such as `Deployment/Resources/LU/zh/todo.lu`.

	```diff
	## AddToDo
	+ - 在{ListType=作业}列表里加上{TaskContent=数学}
	```

	(Optional) After you add new list type, you can modify prompts as needed to make your conversation more friendly. For example, you can modify `Responses/Main/ToDoMainResponses.json`:

	```diff
	"ToDoWelcomeMessage": {
	    "replies": [
	      {
	+       "text": "Hi. I'm To Do bot. I can help you manage your To Do, Shopping, Grocery or Homework list."
	-       "text": "Hi. I'm To Do bot. I can help you manage your To Do, Shopping or Grocery list."
	      }
	    ]
	```

4. Redeploy your To Do Skill.
