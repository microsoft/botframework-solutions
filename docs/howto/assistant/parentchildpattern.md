# Parent-Child pattern for more advanced assistants

## Overview

As customers progress on the journey of developing conversational experiences either for internal or external use, the ability to split out functionality into discrete conversational components (*domains*) and aggregate these together into one *parent* assistant quickly becomes attractive for a number of reasons:

1. **End user fatigue**: As excitment grows around the potential, new bots appear owned by disparate teams which increase the cognitive load on end users who then have to remember the right Bot to use for a given question and may not even discover the full breadth of bots available.
2. **Monolithic Architecture**: As new capabilities are added to a Bot they are all housed within one project. This is fine to start but as complexity increases this becomes unsustainable.
3. **Centralised changes**: Related to number 2, changes to language models, qna and dialogs are typically performed by one central team which quickly becomes a bottleneck across the organisation and generates change-management issues.

## Parent-Child

Adopting a Parent-Child pattern enables you to address these issues and provide the following benefits:

- Establish one *front door* assistant experience for your users to use. This assistant experience identifies the skill(s) best suited for a given question and hand-off processing to a seperate conversational component (skill).
- Enable different teams to *own* their own capabilities packaged up in a conversational component (skill) which is added to the *parent* assistant. These skills can combine any combination of dialogs and QnA.
- Mix programming languages between your Assistant and Skills, for example a C# Assistant could call a Typescript Skill and vice-versa.
- Leverage conversational components (skills) from 3rd parties including Microsoft to quickly extend your assistant capabilities.

## An Example

Consider the Enterprise Assistant example shown below, in this scenario an Enterprise establishes their global assistant brand and personality which all end-users interact with across a broad range of Channels (Teams, WebChat, Speech and Mobile Applications.).

![Enterprise Assistant Example](/docs/media/parentchildpattern-enterpriseassistant.png)

The Enterprise Assistant itself provides basic conversational capabilities and QnA and surfaces capabilities from seperate HR and IT Bots align with integrating a Calendar Skill capability.

## Capabilities required 

### Dispatcher

Taking a natural langauge question from a user (e.g. `What meetings do I have today`) and identifying which (if any) conversational component to hand the question to is the primary function of the Dispatcher.

The Dispatcher requires knowledge of training data for downstream Skills and Q&A in order to reason over all components and make an informed decision on which component to hand control to. A key requirement is the ability to provide scoring comparison across multiple dependencies which is not otherwise possible as there is no common baseline.

### Orchestrator

Once a downstream conversational component has been identified the triggering question is passed across and a conversation with the down-stream skill is established through an Orchestration capability.

Follow-up questions from the user are routed to the skill until the Skill indicates it is complete at which point it hands back control.

The Orchestrator is also responsible for exchanging appropriate Context from the Assistant to the Skill and vice-versa. For example, if the Assistant is already aware of the users location this can be passed to the down-stream component removing the need for the user to be prompted again.

Conversely, a down-stream component can provide information for the Assistant to store as part of it's context for use in subsequent interactions.

In addition, depending on the scenario the Orchestrator also handles authentication-token needs of down-stream skills maintaining authentication at the parent-level enabling tokens to be shared across Skills if needed (e.g. Office 365 across Calendar, Email and ToDo skills).

### Top-Level Intents

Due to the nature of handing over control it's important to ensure that the user can interrupt conversation with a Skill to *escape*. For example saying `cancel` would enable the user to stop an interaction with a Skill and go back to interacting with the parent-level assistant.

## Bot Framework Skills and Virtual Assistant

Bot Framework Skills are a new capability enabling Parent-Child / Assistant type experiences to be created. These skills are almost identical to normal Bot Framework based bots and can be developed and tested in the same way, ensuring a consistent and familiar approach and the same Activity protocol is maintained.

The main change is to add a different invocation approach enabling an Assistant to invoke a Bot directly (via WebSockets) and not have to go via the usual Bot Framework channel infrastructure.

A Skill Template is provided to enable developers to quickly create Bots and existing V4 BF SDK bots can be easily update to enable them to be called as Skills.

A supporting skills command line tool enables Skills to be added/removed/refreshed to a parent Bot with no code changes. This tool abstracts various steps including Dispatcher and Authentication configuration steps.

The Virtual Assistant Template (C# and Typescript) provides out of the box Skill support including Dispatcher configuration. With no additional code changes you can add Skills and top level intents such as cancellation are provided for you.

