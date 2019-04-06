# Skills Overview

Skills are a type of bot that allows developers to develop and test them like a standard bot, while having the functionality to plug in to a greater Virtual Assistant solution.
Apart from minor difference to enable this special invocation pattern, a Skill looks and behaves like a regular bot. The same protocol is maintained between two bots to ensure a consistent approach.
Skills for common scenarios like productivity and navigation to be used as-is or customized however a customer prefers.
> The Skill functionality for Virtual Assistants will inform the broader Azure Bot Service skill approach moving forward.

## Table of Contents
- [Skills Overview](#skills-overview)
  - [Table of Contents](#table-of-contents)
  - [Available Skills](#available-skills)
  - [Skill Deployment](#skill-deployment)
  - [Create a New Skill](#create-a-new-skill)
  - [Skill Invocation Flow](#skill-invocation-flow)
  - [Registration](#registration)
  - [Dispatching Skills](#dispatching-skills)
  - [Using the SkillDialog](#using-the-skilldialog)
  - [Interrupting Active Skills](#interrupting-active-skills)
  - [Generating new LUIS models](#generating-new-luis-models)

## Available Skills

The following Skills are available:
- [Productivity - Calendar](./productivity-calendar.md)
- [Productivity - Email](./productivity-email.md)
- [Productivity - To Do](./productivity-todo.md)
- [Point of Interest](./pointofinterest.md)
- [Automotive](./automotive.md)
- [Experimental Skills](./experimental-skills.md)

## Skill Deployment
The Productivity and Point of Interest skills are automatically deployed and configured as part of a Virtual Assistant deployment. Automotive and Experimental skills are not added automatically. If you wish to deploy and develop/test a skill independently of the Virtual Assistant see the local mode deployment instructions within each skills documentation page.

## Create a New Skill

Use the Skill Template to [Create a New Skill](./create.md) with an out-of-the-box basic Skill and unit test project.
[Developing a new Skill](./developing-a-new-skill.md) with the best practices is key to it's success.

## Skill Invocation Flow

All communication between a Virtual Assistant and a Skill is performed through a custom `SkillDialog`, started when the dispatcher identifies a Skill that maps to a user�s utterances. Skills are invoked through a lightweight `InProcAdapter`, maintaining the communication protocol and ensuring Skills can be developed using the standard Bot Framework toolkit.

`SkillDialog` bootstraps the `InProcAdapter` and processes appropriate middleware before invoking the `OnTurn` method on the Bot for each Activity. A `skillBegin` event is sent at the beginning of each Skill Dialog and the end of a Skill Dialog is marked by the sending of a `endOfConversation` event.

![Skill Invocation Flow](../../media/virtualassistant-SkillFlow.png)

## Registration

Each Skill is registered with a Virtual Assistant through the configuration entry shown below

|Name|Description|
---|---
Name | The name of your Skill|
Assembly| Skills are invoked "in process" and are dynamically loaded using Reflection thus enabling a configuration only approach|
DispatchIntent| The name of the intent within the Dispatch model which covers your Skills LUIS capabilities|
SupportedProviders| The Supported Authentication Providers provides the ability to highlight which authentication providers this skill supports (if any). This enables the Virtual Assistant to retrieve the token related to that provider when a user asks a question.|
LuisServiceIds| The LUIS model names used by this skill. All Skills will make use of the General model along with their own LUIS model.|
Parameters| Parameters are an optional mechanism to pass user-data across a part of the Skill invocation. For example, a Skill may request access to the users current location or timezone to better personalise the experience. This Parameters are sourced automatically from the Virtual Assistant state for a given User/Conversation and provided to the Skill.|
Configuration| Skills are invoked in-process to the Virtual Assistant so don't have access to their respective appsettings.json file, in cases where a Skill needs configuration data it can be provided through this mechanism. LUIS Configuration settings and secrets for a web-service used by a Skill are examples of configuration.|

 ```
  "skills": [
    {
      "type": "skill",
      "id": "calendarSkill",
      "name": "calendarSkill",
      "assembly": "CalendarSkill.CalendarSkill, CalendarSkill, Version=1.0.0.0, Culture=neutral",
      "dispatchIntent": "l_Calendar",
      "supportedProviders": [
        "Azure Active Directory v2",
        "Google"
      ],
      "luisServiceIds": [
        "calendar",
        "general"
      ],
      "parameters": [
        "IPA.Timezone"
      ],
      "configuration": {
        "configSetting1": "",
        "configSetting2": "",
      }
    },
 ```
 
## Dispatching Skills
When a user tries to trigger a Skill, the Virtual Assistant needs to know how to process that and correctly map to a registered Skill.
The [Dispatch](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-tutorial-dispatch?view=azure-bot-service-4.0) model identifies all registered Skill LUIS models and how it should be processed locally (through LUIS and code, QnA Maker, or by invoking a Skill with `SKillDialog`).

## Using the SkillDialog
Each Skill uses a `SkillDialog` class to manage it's invocation.
The Virtual Assistant identifies a Skill to use and creates a new `SkillDialog` instance with configuration properties as a parameter. 
Through reflection, the dialog instantiates the Skill and invokes the `OnTurn` handler to begin the Skill. 
Skills require a new state container, configured in your Virtual Assistant�s configured state store, to ensure state is maintained at the highest level. 
This dialog is active on the Virtual Assistant�s `DialogStack`, ensuring that subsequent utterances are routed to your Skill. 
When an `EndOfConversation` event is sent from the Skill, it tears down the `SkillDialog` and returns control back to the user.

## Interrupting Active Skills
Skills can be interrupted through a top-level interruption (e.g. "cancel"). The user is prompted to confirm before tearing down the active Skill.

## Generating new LUIS models
Each Skill uses a different LUIS language model that needs to be represented in code. Currently, the language models available in the Virtual Assistant are:

* [`Email.cs`](https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/src/csharp/skills/emailskill/Dialogs/Shared/Resources/Email.cs)
* [`Calendar.cs`](https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/src/csharp/skills/calendarskill/Dialogs/Shared/Resources/Calendar.cs)
* [`PointOfInterest.cs`](https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/src/csharp/skills/pointofinterestskill/Dialogs/Shared/Resources/PointOfInterest.cs)
* [`ToDo.cs`](https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/src/csharp/skills/todoskill/Dialogs/Shared/Resources/ToDo.cs)
* [`Dispatch.cs`](https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/src/csharp/assistant/Dialogs/Shared/Resources/Dispatch.cs)
* [`General.cs`](https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/src/csharp/microsoft.bot.solutions/Resources/General.cs)

To generate the language model class, please use [LuisGen](https://github.com/Microsoft/botbuilder-tools/tree/master/packages/LUISGen).

After generating the new  *.cs class, make the following changes:

* `public _Entities Entities { get; set; }` to `public virtual _Entities Entities { get; set; }`
* `public (Intent intent, double score) TopIntent()` to `public virtual (Intent intent, double score) TopIntent()`

This change is to make sure we have the ability to override the `Entities` property and `TopIntent` function in the Mock luis models for test purposes. Example of a Mock luis model: [MockEmailIntent.cs](https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/src/csharp/skills/tests/emailskilltest/Flow/Fakes/MockEmailIntent.cs)