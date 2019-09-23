---
category: Reference
subcategory: Skills
title: Architecture
description: Under the covers of the skill implementation.
order: 1
---

# {{ page.title }}
{:.no_toc}

## In this reference
{:.no_toc}

* 
{:toc}

## Intro

Developers can compose conversational experiences by stitching together re-usable conversational capabilities, known as Skills.

Within an Enterprise, this could be creating one parent bot bringing together multiple sub-bots owned by different teams, or more broadly leveraging common capabilities provided by other developers. With this preview of Skills, developers can create a new bot (typically through the Virtual Assistant template) and add/remove Skills with one command line operation incorporating all Dispatch and Configuration changes.

Skills are themselves Bots, invoked remotely and a Skill developer template (.NET, TS) is available to facilitate creation of new Skills.

A key design goal for Skills was to maintain the consistent Activity protocol and ensure the development experience was as close to any normal V4 SDK bot as possible. To that end, a Bot simply starts a `SkillDialog` which abstracts the skill invocation mechanics.

## Invocation Flow

![Skill Invocation Flow]({{site.baseurl}}/assets/images/virtualassistant-SkillFlow.png)

### Dispatcher
{:.no_toc}

The Dispatcher plays a central role to enabling a Bot to understand how to best process a given utterance. The Dispatch through use of the [Skill CLI]({{site.baseurl}}/reference/botskills) is updated with triggering utterances for a given Skill and a new Dispatch intent is created for a given Skill. An example of a Dispatch model with a point of interest skill having been added is shown below.

![Dispatch with Skill Example]({{site.baseurl}}/assets/images/skillarchitecturedispatchexample.png)

When the user of a Virtual Assistant asks a question, the Dispatcher will process the utterance and as appropriate identify a skill intent as being the most appropriate way to process the utterance.

## SkillDialog

> When testing a Virtual Assistant using the Emulator the SkillDialog surfaces Skill invocation and slot-filling telemetry.

On start-up of a Virtual Assistant, each registered Skill results in a SkillDialog instance being created which is associated with a `SkillManifest` instance containing details about the Skill including it's endpoint, actions and slots.

All communication between a Virtual Assistant and a Skill is performed through a custom `SkillDialog`, which is started when the dispatcher identifies a Skill that maps to a users utterances. Skills are invoked through a lightweight `SkillWebSocket` or `SkillHttp` adapter, maintaining the standard Bot communication protocol and ensuring Skills can be developed using the standard Bot Framework toolkit.

The `SkillManifest` provides the endpoint for the SkillDialog to communicate with along with action and slot information. Slots are optional and a way to pass parameters to a Skill.

When a Skill wants to terminate an ongoing dialog, it sends back an Activity with `Handoff` type to signal the completion of the current dialog. 

See the [SkillAuthentication]({{site.baseurl}}/reference/skills/skillauthentication) section for information on how Bot->Skill invocation is secured.

## Skill Middleware

The `SkillMiddleware` is used by each Skill and is configured automatically if you use the Skill Template.

The middleware consumes the `skill/cancelallskilldialogs` event, when the Skill receives it it clears out the active dialog stack on that active Skill. This is useful in interruptions - i.e. if a user asks to cancel, a Virtual Assistant can send this event to the Skill and cancel the active dialog.

## Interrupting Active Skills

Skills can be interrupted through a top-level interruption (e.g. "cancel"). The user is prompted to confirm before tearing down the active Skill. This requires each utterance, even during a SkillDialog to be inspected by the Bot Dispatcher to determine if cancellation is needed before then continuing. That is not shown in the above diagram for brevity reasons.
