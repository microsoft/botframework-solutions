# Parent-Child pattern of Virtual Assistants and Skills

## Overview

As you develop your Virtual Assistant, you will find that the ability to manage individual domains of a conversation (Skills) and aggregate them into a singular *parent* Assistant becomes attractive for a number of reasons:

* **End user fatigue**: As customer adoption for this technology grows, new Bots appear owned by disparate teams, increasing the cognitive load on your end users. It becomes up to them to remember the right Bot to use for a given function and they may not discover the full breadth of Bots available.
* **Monolithic Architecture**: As a Bot increases it's complexity it becomes unsustainable to house them within a single project.
* **Centralized changes**: On the cognitive-side, changes to language models, QnA knowledge bases, and dialogs are usually performed by a central team. This quickly becomes a bottleneck across an organization and highlights change-management issues over time.

## Parent-Child pattern

Adopting a Parent-Child pattern enables you to address the above issues and provides the following benefits:


* Establish a front-facing Assistant experience that your users grow familiar with. This Assistant identifies the intent best suited for a given utterance and hands off processing to a remote-hosted Skill.
* Enable different teams to own their own capabilities packaged up in a Skill which is added to the **parent** Assistant.
* Mix programming languages between your Assistant and Skills, for example a C# Assistant could call a Typescript Skill and vice-versa.
* Leverage Skills from third parties including Microsoft to quickly extend your Assistant's capabilities.

### Example: Enterprise Assistant

In the Enterprise Assistant example shown below, an enterprise customer establishes their global Assistant's brand and personality that  all end users interact with across a broad range of [Bot Framework Channels](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-channels?view=azure-bot-service-4.0).

![Enterprise Assistant Example](/docs/media/parentchildpattern-enterpriseassistant.png)

The Enterprise Assistant itself provides basic conversational capabilities and QnA and surfaces capabilities from separate HR and IT Bots align with integrating a Calendar Skill capability. Due to the nature of handing over control to skills it's important to ensure that the user can interrupt conversation with a Skill to *escape*. For example saying `cancel` would enable the user to stop an interaction with a Skill and go back to interacting with the parent-level assistant. A user may also wish to seek help or escalate to a human.

## Key Concepts

The following concepts are key to an effective Skill architecture. These are described at a generic level before moving into details of how this is solved as part of the Virtual Assistant.

### Dispatching

Taking a natural language question from a user (e.g. `What meetings do I have today`) and identifying which (if any) conversational component to hand the question to is the primary function of the Dispatching capability.

The Dispatcher capability requires knowledge of training data for downstream Skills and Q&A in order to reason over all components and make an informed decision on which component to hand control to. A key requirement is the ability to provide scoring comparison across multiple dependencies which is not otherwise possible as there is no common baseline.

### Orchestrator

Once a downstream conversational component has been identified the triggering question is passed across and a conversation with the downstream Skill is established through an Orchestration capability.

Follow-up questions from the user are routed to the Skill until the Skill indicates it is complete at which point it hands back control.

The Orchestrator is also responsible for exchanging appropriate Context from the Assistant to the Skill and vice-versa. For example, if the Assistant is already aware of the users location this can be passed to the downstream component removing the need for the user to be prompted again.

Conversely, a downstream component can provide information for the Assistant to store as part of it's context for use in subsequent interactions.

In addition, depending on the scenario the Orchestrator also handles authentication-token needs of downstream Skills maintaining authentication at the parent-level enabling tokens to be shared across Skills if needed (e.g. Office 365 across Calendar, Email and ToDo skills).

## Bot Framework Skills and Virtual Assistant

Bot Framework Skills are a new capability enabling Parent-Child / Assistant type experiences to be created. These Skills are almost identical to normal Bot Framework based bots and can be developed and tested in the same way, ensuring a consistent and familiar approach and the same Activity protocol is maintained.

The main change, is to add a different invocation approach enabling an Assistant to invoke a Skill directly (via WebSockets) and not have to go via the usual Bot Framework channel infrastructure. We provide a [Dispatcher](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-tutorial-dispatch?view=azure-bot-service-4.0&tabs=cs) capability which is wired up as part of the Virtual Assistant which takes in training data from LUIS models and QnAMaker to enable effective routing, other dispatching sources can be added through flat-file import.

The [Skill Architecture](/docs/reference/skills/architecture.md) documentation covers the role of the Dispatcher and SkillDialog in more detail.

A Skill Template is provided to enable developers to quickly create Skills and existing V4 BF SDK bots can be easily updated to enable them to be called as Skills.

A supporting Skill command line tool enables Skills to be added/removed/refreshed to a parent Bot with no code changes. This tool abstracts various steps including Dispatcher and Authentication configuration steps.

The Virtual Assistant Template (C# and Typescript) provides out of the box Skill support including Dispatcher configuration. With no additional code changes you can add Skills and top level intents such as cancellation are provided for you.

