# Virtual Assistant Skills Overview

## Overview

A key design goal for the Virtual Assistant Skills are to develop the Skill like a standard bot while having the functionality to plug-in to a Virtual Assistant. Apart from minor differences to enable this unique invocation pattern, a Skill looks and feels like a regular bot. The same protocol is maintained between the two bots to ensure a consistent approach and provide additional deployment options in the future (e.g. “out of process” invocation). Skills for scenarios like productivity and navigation are provided to be used as-is or customized in any way a customer prefers.

> The Skill functionality for Virtual Assistants will inform the broader Azure Bot Service skill approach moving forward.

# Available Skills

The following Skills are available at this time, these represent initial priority scenarios and work is ongoing:
- [Productivity - Calendar](./productivity-calendar.md)
- [Productivity - Email](./productivity-email.md)
- [Productivity - To Do](./productivity-todo.md)
- [Point of Interest](./pointofinterest.md)
- [Automotive](./automotive.md)
- [Experimental Skills](./experimental-skills.md)

## Skill Invocation Flow

All communication between a Virtual Assistant and a Skill is performed through a custom `SkillDialog`, started when the dispatcher identifies a Skill that maps to a user’s utterances. Skills are invoked through a lightweight `BotAdapter`, maintaining the communication protocol and ensuring Skills can be developed using the standard Bot Framework toolkit.

`SkillDialog` bootstraps the `BotAdapter` and processes appropriate middleware before invoking the `OnTurn` method on the Bot for each Activity. A `skillBegin` event is sent at the beginning of each Skill Dialog and the end of a Skill Dialog is marked by the sending of a `endOfConversation` event.

![Skill Invocation Flow](../media/virtualassistant-SkillFlow.png)

## Skill Registration

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
 
## Dispatching


The Virtual Assistant needs to know how to process a given user utterance and map to a registered Skill. The [Dispatch](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-tutorial-dispatch?view=azure-bot-service-4.0) model identifies all registered Skill LUIS models and how it should be processed locally, through LUIS and code, QnA Maker, or by invoking a Skill with `SKillDialog`.

## Skill Dialog

The Skill Dialog manages the invocation of it’s Skill. When identified by the Virtual Assistant, it creates a new `SkillDialog` instance with configuration properties as a parameter. Through reflection, the dialog instantiates the skill and invokes the `OnTurn` handler to begin the Skill. Skills require a new state container, configured in your Virtual Assistant’s configured state store, to ensure state is maintained at the highest level. This dialog is active on the Virtual Assistant’s `DialogStack`, ensuring that subsequent utterances are routed to your Skill. When an `EndOfConversation` event is sent from the Skill, it tears down the `SkillDialog` and returns control back to the user.
## Skill Interruption

The Virtual Assistant can interrupt an active Skill through a top-level interruption (e.g. "cancel"). A prompt is triggered to the user to confirm before tearing down your Skill.