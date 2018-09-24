# Custom Assistant Skills

## Overview

The design goals for the Custom Assistant Skills are to provide a way to plug-in domain functionality to a Bot (in this case a Custom Assistant) purely through configuration whilst enabling a Skill to be developed and tested just like a normal Bot.

Therefore a Skill, looks and feels just like a regular Bot apart from some additional code to handle the different invocation pattern. The same Bot protocol is maintained between the Custom Assistant and Skills ensuring a consistent approach and providing additional deployment options in the future - e.g. Out of Process invocation using HTTP.

This enables delivery of Skills for common scenarios such as Productivity (Calender, Email and Tasks) and Points of Interest which can then be used as-is or customised in any way as the accompanying language model, dialog and integration code is provided.

> The Skill functionality for Custom Assistants will inform the broader Azure Bot Service skill approach moving forward.

# Available Skills

The following Skills are available at this time, these represent initial priority scenarios and work is ongoing:
- [Productivity - Calendar](./customassistant-skills-productivity-calendar.md)
- [Productivity - Email](./customassistant-skills-productivity-email.md)
- [Productivity - Tasks](./customassistant-skills-productivity-tasks.md)
- [Points of Interest](./customassistant-skills-pointofinterest.md)
- Automotive - Coming Soon 

## Skill Invocation Flow

All communication between a Custom Assistant and a Skill will be performed through a custom SkillDialog which is started when the Dispatcher identifies a Skill as the component to activate for processing of a given utterance. Skills are invoked through a lightweight BotAdapter which maintains the communication protocol and ensures that Skills can be developed/tested using the standard Bot Framework tooling.

The custom SkillDialog bootstraps the Adapter and processes appropriate middleware (currently only State) before invoking the OnTurn method on the Bot for each Activity. A skillBegin event is sent at the beginning of each Skill Dialog and the end of a Skill Dialog is marked by the sending of a `endOfConversation` event.

![Skill Invocation Flow](./media/customassistant-SkillFlow.png)

 ## Skill Registration

 Each Skill is registred with a Custom Assistant through the configuration entry shown below

 - Name: The name of your Skill
 - DispatcherModelName: The name of the LUIS model added to your Skill, used to help match a given dispatch target to a Skill
 - Assembly: Skills are invoked "in process" and are dynamically loaded using Reflection thus enabling a configuration only approach
 - AuthConnectionName: The Authentication Connection Name the Skill will use when requesting User Tokens. Authentication connection names are configured on the Custom Assistant Bot Settings in the Azure Portal
 - Parameters (Optional) Parameters are a mechanism to pass user-data across a part of the Skill invocation. For example, a Skill may request access to the users current location or timezone to better personalise the experience. This Parameters are sourced automatically from the Custom Assistant state for a given User/Conversation and provided to the Skill.
 - Configuration (Optional) Skills are invoked in-process to the Custom Assistant so don't have access to their respective appsettings.json file, in cases where a Skill needs configuration data it can be provided through this mechanism. LUIS Configuration settings and secrets for a web-service used by a Skill are examples of configuration. 

 ```
  "Skills": [
    {
      "Name": "Demo",
      "DispatcherModelName": "DemoSkill",
      "Description": "The Demo Skill implements basic Graph profile info.",
      "Assembly": "DemoSkill.DemoSkill, DemoSkill, Version=1.0.0.0, Culture=neutral",
      "AuthConnectionName": "AzureADConnection",
      "Parameters": 
      [
        "IPA.Location",
        "IPA.Timezone"
      ],
      "Configuration": {
        "ServiceKeyA": "YOUR_KEY_HERE",
		    "LuisAppId": "YOUR_LUIS_APP_ID",
        "LuisSubscriptionKey": "YOUR_LUIS_SUBSCRIPTION_KEY",
        "LuisEndpoint": "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/"
      }
    }
  ]
 ```
 
## Dispatching

In order for the Custom Assistant to know how to process a given utternace (e.g. What tasks do I have) any Skill will be registered with the Custom Assistant through additional configuration in the `appSettings.json` file and by adding the Skill LUIS model to the Custom Assistant Dispatch configuration along with the subsequent evaluation steps.

This enables the Custom Assistant to take an utterance and identify where it should be processed `locally` by the Custom Assistant through LUIS+Code, QnAMaker or whether a `SkillDialog` be created to handle Skill invocation.

## SkillDialog

The Skill Dialog is responsible for managing the invocation for a Skill. When the Custom Assistant identifies a Skill should pass a given utterance it creates a new `SkillDialog` instance passing the request skill configuration as a parameter. The Skill Dialog then instantiates the Skill through Reflection and invokes the OnTurn handler through use of a simple In-Process Bot Adapter which is responsible for maintaining the Bot Framework communication protocol semantics.

A new State container for a given Skill is created within the Custom Assistant's configured state store, typically CosmosDB thus ensuring State is kept together at the Custom Assistant level.

This dialog remains active on the Custom Assistant `DialogStack` ensuring that subsequent utterances are routed to the Skill. When a Dialog within the Skill has finished it triggers an `EndOfConversation` event back to the SkillDialog which then tears down the SkillDialog returning control back to the user.

## Skill Interruption

The Custom Assistant can interrupt an active Skill through a top-level interruption (e.g. cancel). This will trigger a prompt to the user that they wish to stop what they were doing before tearing down of the Skill.
