# Skill Architecture

## Overview

Developers can compose conversational experiences by stitching together re-usable conversational capabilities, known as Skills.

Within an Enterprise, this could be creating one parent bot bringing together multiple sub-bots owned by different teams, or more broadly leveraging common capabilities provided by other developers. With this preview of Skills, developers can create a new bot (typically through the Virtual Assistant template) and add/remove Skills with one command line operation incorporating all Dispatch and Configuration changes.

Skills are themselves Bots, invoked remotely and a Skill developer template (.NET, TS) is available to facilitate creation of new Skills.

A key design goal for Skills was to maintain the consistent Activity protocol and ensure the development experience was as close to any normal V4 SDK bot as possible. To that end, a Bot simply starts a `SkilllDialog` which abstracts the skill invocation mechanics.

## Invocation Flow

![Skill Invocation Flow](/media/virtualassistant-SkillFlow.png)

### Dispatcher

The [Dispatcher](//reference/assistant/dispatcher.md) plays a central role to enabling a Bot to understand how to best process a given utterance. The Dispatch through use of the [Skill CLI](/docs/reference/assistant/skillcli.md) is updated with triggering utterances for a given Skill and a new Dispatch intent is created for a given Skill. An example of a Dispatch model with a point of interest skill having been added is shown below.

![Dispatch with Skill Example](/media/skillarchitecturedispatchexample.png)

When the user of a Virtual Assistant asks a question, the Dispatcher will process the utterance and as appropriate identify a skill intent as being the most appropriate way to process the utterance.

## SkillDialog

> When testing a Virtual Assistant using the Emulator the SkillDialog surfaces Skill invocation and slot-filling telemetry.

On start-up of a Virtual Assistant, each registered Skill results in a SkillDialog instance being created which is associated with a `SkillManifest` instance containing details about the Skill including it's endpoint, actions and slots.

All communication between a Virtual Assistant and a Skill is performed through a custom `SkillDialog`, which is started when the dispatcher identifies a Skill that maps to a users utterances. Skills are invoked through a lightweight `SkillWebSocket` or `SkillHttp` adapter, maintaining the standard Bot communication protocol and ensuring Skills can be developed using the standard Bot Framework toolkit.

The `SkillManifest` provides the endpoint for the SkillDialog to communicate with along with action and slot information. Slots are optional and a way to pass parameters to a Skill.

 A `skill/begin` event is sent at the beginning of each Skill Dialog and the end of a Skill Dialog is marked by the sending of a `endOfConversation` event. This event contains a `SkillContext` object that contains matched Slot information, if the Virtual Assistant has populated matching data into it's SkillContext object then it's retrieved and passed across to the Skill.

 For example, if there is a `Location` data item in Virtual Assistant SkillContext object and the Skill being invoked has a `Location` slot it will be matched and passed.

An example of a `skill/begin` event is shown below:

 ```json
{
    "type": "event",
    "channelId": "test",
    "from": {
        "id": "user1",
        "name": "User1"
    },
    "conversation": {
        "id": "Conversation1"
    },
    "recipient": {
        "id": "bot",
        "name": "Bot"
    },
    "value": {
        "param1": "TEST",
        "param2": "TEST2"
    },
    "name": "skill/begin"
}
 ```

This dialog remains active on the Virtual Assistant's `DialogStack`, ensuring that subsequent utterances are routed to your Skill.

When an `EndOfConversation` event is sent from the Skill, it tears down the `SkillDialog` and returns control back to the Virtual Assistant.

See the [SkillAuthentication](/docs/reference/assistant/skillauthentication.md) section for information on how Bot->Skill invocation is secured.

## Skill Middleware

The `SkillMiddleware` is used by each Skill and is configured automatically if you use the Skill Template.

The middleware consumes the `skill/begin` event and populates SkillContext on the Skill side making slots available.

## Interrupting Active Skills

Skills can be interrupted through a top-level interruption (e.g. "cancel"). The user is prompted to confirm before tearing down the active Skill. This requires each utterance, even during a SkillDialog to be inspected by the Bot Dispatcher to determine if cancellation is needed before then continuing. That is not shown in the above diagram for brevity reasons.
