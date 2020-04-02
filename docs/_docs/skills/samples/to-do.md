---
category: Skills
subcategory: Samples
title: To Do Skill
description: Add task management capabilities to your Assistant. Powered by Microsoft Graph.
order: 4
toc: true
---

# {{ page.title }}
{:.no_toc}

{{ page.description }}

## Supported scenarios
{:.toc}

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
  - *Remove salad vegetables from my grocery list*
  - *Remove my to do to pick up Tom at 6 AM*
  - *Remove all tasks*
- Mark Tasks as Complete
  - *Mark the task get some food as complete*
  - *Task completed reserve a restaurant*
  - *Check off bananas on my grocery list*

## Language Understanding
{:.toc}

LUIS models for the Skill are provided in **.lu** file format as part of the Skill. Further languages are being prioritized.

|Supported Languages |
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
|AddToDo| Matches queries to add To Do items to a list |
|ShowToDo| Matches queries to show To Do items or lists |
|MarkToDo| Matches queries to toggle a To Do item |
|DeleteToDo| Matches queries to delete a To Do item |

### Entities
{:.no_toc}

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
{:.toc}

### Deployment
{:.no_toc}

Learn how to [provision your Azure resources]({{site.baseurl}}/skills/tutorials/create-skill/csharp/4-provision-your-azure-resources/) in the Create a Skill tutorial.

### Supported content providers
{:.no_toc}

> Office 365 and Outlook.com through the Microsoft Graph is supported at this time.

### Authentication connection settings
{:.no_toc}

This skill uses the following authentication scopes:
- **Notes.ReadWrite** 
- **User.Read**
- **User.ReadBasic.All**
- **Tasks.ReadWrite**
- **Mail.Send**

You must use [these steps]({{site.baseurl}}/{{site.data.urls.SkillManualAuth}}) to manually configure Authentication for the ToDo Skill. Due to a change in the Skill architecture this is not currently automated.

> Ensure you configure all of the scopes detailed above.

### Add customized to do lists
{:.no_toc}

If you want to add your customized list types, for example, your homework list or movie list, please follow these steps:

1. Add your list type to **appsettings.json**

	```json
	"customizeListTypes": [
	  "Homework",
	  "Movie"
	]
	```
1. Add your list type name and its synonym in `Responses/Shared/ToDoString.resx`

	Name | Value 
	---- | ----- 
	Homework | Homework 
	HomeworkSynonym | homework, home work 

1. Modify your LUIS file. Modify `Deployment/Resources/LU/en/todo.lu` so that your LUIS app can tell these new ListType entities. You can provide more utterance to make your LUIS model perform better.

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

1. Redeploy your To Do Skill.

## Download a transcript
{:.toc}

<a class="btn btn-primary" href="{{site.baseurl}}/assets/transcripts/skills-todo.transcript">Download</a>