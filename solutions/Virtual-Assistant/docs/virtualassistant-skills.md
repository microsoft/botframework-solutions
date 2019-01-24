# Virtual Assistant Skills

## Overview

The design goals for the Virtual Assistant Skills are to provide a way to plug-in domain functionality to a Bot (in this case a Virtual Assistant) purely through configuration whilst enabling a Skill to be developed and tested just like a normal Bot.

Therefore a Skill, looks and feels just like a regular Bot apart from some additional code to handle the different invocation pattern. The same Bot protocol is maintained between the Virtual Assistant and Skills ensuring a consistent approach and providing additional deployment options in the future - e.g. Out of Process invocation using HTTP.

This enables delivery of Skills for common scenarios such as Productivity (Calender, Email and Tasks) and Points of Interest which can then be used as-is or customised in any way as the accompanying language model, dialog and integration code is provided.

> The Skill functionality for Virtual Assistants will inform the broader Azure Bot Service skill approach moving forward.

# Available Skills

The following Skills are available at this time, these represent initial priority scenarios and work is ongoing:
- [Productivity - Calendar](./virtualassistant-skills-productivity-calendar.md)
- [Productivity - Email](./virtualassistant-skills-productivity-email.md)
- [Productivity - ToDo](./virtualassistant-skills-productivity-todo.md)
- [Points of Interest](./virtualassistant-skills-pointofinterest.md)
- Automotive - Coming Soon 

## Skill Invocation Flow

All communication between a Virtual Assistant and a Skill will be performed through a custom SkillDialog which is started when the Dispatcher identifies a Skill as the component to activate for processing of a given utterance. Skills are invoked through a lightweight BotAdapter which maintains the communication protocol and ensures that Skills can be developed/tested using the standard Bot Framework tooling.

The custom SkillDialog bootstraps the Adapter and processes appropriate middleware (currently only State) before invoking the OnTurn method on the Bot for each Activity. A skillBegin event is sent at the beginning of each Skill Dialog and the end of a Skill Dialog is marked by the sending of a `endOfConversation` event.

![Skill Invocation Flow](./media/virtualassistant-SkillFlow.png)

 ## Skill Registration

 Each Skill is registered with a Virtual Assistant through the configuration entry shown below

 - Name: The name of your Skill
  - Assembly: Skills are invoked "in process" and are dynamically loaded using Reflection thus enabling a configuration only approach
 - dispatchIntent: The name of the intent within the Dispatch model which covers your Skills LUIS capabilities
 - supportedProviders: The Supported Authentication Providers provides the ability to highlight which authentication providers this skill supports (if any). This enables the Virtual Assistant to retrieve the token related to that provider when a user asks a question.
 - luisServiceIds: The LUIS model names used by this skill. All Skills will make use of the General model along with their own LUIS model.
 - Parameters: Parameters are an optional mechanism to pass user-data across a part of the Skill invocation. For example, a Skill may request access to the users current location or timezone to better personalise the experience. This Parameters are sourced automatically from the Virtual Assistant state for a given User/Conversation and provided to the Skill.
 - Configuration: Skills are invoked in-process to the Virtual Assistant so don't have access to their respective appsettings.json file, in cases where a Skill needs configuration data it can be provided through this mechanism. LUIS Configuration settings and secrets for a web-service used by a Skill are examples of configuration.

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

In order for the Virtual Assistant to know how to process a given utternace (e.g. What tasks do I have) any Skill will be registered with the Virtual Assistant through additional configuration in the `appSettings.json` file and by adding the Skill LUIS model to the Virtual Assistant Dispatch configuration along with the subsequent evaluation steps.

This enables the Virtual Assistant to take an utterance and identify where it should be processed `locally` by the Virtual Assistant through LUIS+Code, QnAMaker or whether a `SkillDialog` be created to handle Skill invocation.

## SkillDialog

The Skill Dialog is responsible for managing the invocation for a Skill. When the Virtual Assistant identifies a Skill should pass a given utterance it creates a new `SkillDialog` instance passing the request skill configuration as a parameter. The Skill Dialog then instantiates the Skill through Reflection and invokes the OnTurn handler through use of a simple In-Process Bot Adapter which is responsible for maintaining the Bot Framework communication protocol semantics.

A new State container for a given Skill is created within the Virtual Assistant's configured state store, typically CosmosDB thus ensuring State is kept together at the Virtual Assistant level.

This dialog remains active on the Virtual Assistant `DialogStack` ensuring that subsequent utterances are routed to the Skill. When a Dialog within the Skill has finished it triggers an `EndOfConversation` event back to the SkillDialog which then tears down the SkillDialog returning control back to the user.

## Skill Interruption

The Virtual Assistant can interrupt an active Skill through a top-level interruption (e.g. cancel). This will trigger a prompt to the user that they wish to stop what they were doing before tearing down of the Skill.
