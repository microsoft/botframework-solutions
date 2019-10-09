---
category: Overview
title: What's in the Virtual Assistant template?
description: An outline of what the Virtual Assistant template provides
order: 2
---

# {{ page.title }}
{:.no_toc}

{{ page.description }}

## In this reference
{:.no_toc}

* 
{:toc}

## Introduction

This section of the documentation covers each capability providing an overview and code walk-through enabling you to understand what has been provided to get you started. This documentation can also be used to lift discrete pieces into your own Bot if preferred.

The Virtual Assistant Template brings together a number of best practices we've identified through the building of conversational experiences and automates integration of components that we've found to be highly beneficial to Bot Framework developers. This section covers some background to key decisions to help explain why the template works the way it does with links to detailed information where appropriate.

## Your Assistant Project

Using the template you'll end up with your own Assistant project that is organized in-line with our latest thinking on how a Bot project can be structured. You can choose to restructure this as necessary but bear in mind that the provided deployment scripts expect some files to be in a consistent location so bear this in mind.

To learn more about project structure, see the [Create Project](https://microsoft.github.io/botframework-solutions/tutorials/csharp/create-assistant/3_create_project/) documentation.

## LU File Format

The [LU](https://github.com/Microsoft/botbuilder-tools/blob/master/packages/Ludown/docs/lu-file-format.md) format is similar to MarkDown enabling easy modification and source control of your LUIS models and QnA information. Virtual Assistant uses these files at it's core to simplify deployment and provide a ongoing source control solution.

The [LuDown](https://github.com/Microsoft/botbuilder-tools/tree/master/packages/Ludown) tool is then used to convert .LU files into LUIS models which can then be published to your LUIS subscription either through the portal or the associated [LUIS](https://github.com/Microsoft/botbuilder-tools/tree/master/packages/LUIS) CLI (command line) tool. The same tool is used to create a QnA Maker JSON file which the [QnA Maker](https://github.com/Microsoft/botbuilder-tools/tree/master/packages/QnAMaker) CLI (command line) tool then uses to publish items to the QnA Maker knowledgebase.

All of the above is handled as part of the Deployment scripts detailed below.

## LUIS

Every Bot should handle a base level of conversational language understanding. Cancellation or Help for example are a basic things every Bot should handle with ease. Typically, developers need to create these base intents and provide initial training data to get started. The Virtual Assistant template provides example LU files to get you started and avoids every project having to create these each time and ensures a base level of capability out of the box.

The LU files provide the following intents across English, Chinese, French, Italian, German, Spanish. You can review these within the `Deployment\Resources` folder or [here](https://github.com/microsoft/botframework-solutions/tree/master/templates/Virtual-Assistant-Template/csharp/Sample/VirtualAssistantSample/Deployment/Resources/LU).

> Cancel, Confirm, Escalate, FinishTask, GoBack, Help, Reject, Repeat, SelectAny, SelectItem, SelectNone, ShowNext, ShowPrevious, StartOver, Stop

### LUIS Strongly Typed classes

The [LuisGen](https://github.com/microsoft/botbuilder-tools/blob/master/packages/LUISGen/src/npm/readme.md) tool enables developers to create a strongly-typed class for their LUIS models. As a result you can easily reference the intents and entities as class instance members.

You'll find a `GeneralLuis.cs` and `DispatchLuis.cs` class as part of your project within the `Services` folder. The DispatchLuis.cs will be re-generated if you add Skills to reflect the changes made.

To learn more about LuisGen, see the [LuisGen Tool](https://github.com/microsoft/botbuilder-tools/blob/master/packages/LUISGen/src/npm/readme.md) documentation and you can find examples of these classes [here](https://github.com/microsoft/botframework-solutions/tree/master/templates/Virtual-Assistant-Template/csharp/Sample/VirtualAssistantSample/Services).

## QnA Maker
A key design pattern used to good effect in the first wave of conversational experiences was to leverage Language Understanding (LUIS) and QnA Maker together. LUIS would be trained with tasks that your Bot could do for an end user and QnA Maker would be trained with more general knowledge and also provide personality chit-chat capabilities.

[QnA Maker](https://www.qnamaker.ai/) provides the ability for non-developers to curate general knowledge in the format of question and answer pairs. This knowledge can be imported from FAQ data sources, product manuals and interactively within the QnaMaker portal.

Two example QnA Maker models are provided in the [LU](https://github.com/Microsoft/botbuilder-tools/blob/master/packages/Ludown/docs/lu-file-format.md) file format within the `Deployment\Resources` folder or [here](https://github.com/microsoft/botframework-solutions/tree/master/templates/Virtual-Assistant-Template/csharp/Sample/VirtualAssistantSample/Deployment/Resources/QnA).

### Base Personality

QnAMaker provides 5 different personality types which you can find [here](https://github.com/microsoft/BotBuilder-PersonalityChat/tree/master/CSharp/Datasets). The Virtual Assistant template includes the `Professional` personality and has been converted into the [LU](https://github.com/Microsoft/botbuilder-tools/blob/master/packages/Ludown/docs/lu-file-format.md) format to ease source control and deployment.

You can review this within the `Deployment\Resources` folder or [here](https://github.com/microsoft/botframework-solutions/tree/master/templates/Virtual-Assistant-Template/csharp/Sample/VirtualAssistantSample/Deployment/Resources/QnA).

![QnA ChitChat example]({{site.baseurl}}/assets/images/qnachitchatexample.png)

## Dispatch Model

[Dispatch](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-tutorial-dispatch?view=azure-bot-service-4.0&tabs=csaddref%2Ccsbotconfig) provides an elegant solution to bringing together LUIS models and QnAMaker knowledge-bases into one experience. It does this by extracting utterances from each configured LUIS model and questions from QnA Maker and creating a central dispatch LUIS model. This enables a Bot to quickly identify which LUIS model or component should handle a given utterance and ensures QnA Maker data is considered at the top level of intent processing not just through None intent processing as has been the case previously.

This Dispatch tool also enables model evaluation which will highlight confusion and overlap across LUIS models and QnA Maker knowledgebases highlighting issues before deployment.

The Dispatch model is used at the core of each project created using the template. It's referenced within the `MainDialog` class to identify whether the target is a LUIS model or QnA. In the case of LUIS, the secondary LUIS model is invoked returning the intent and entities as usual. Dispatcher is also used for interruption detection and Skill processing whereby your Dispatch model will be updated each time you add a new Skill.

![Dispatch Example]({{site.baseurl}}/assets/images/dispatchexample.png)

## Fallback Response Handling

// TODO

## Interruption

// TODO

## Activity Processing

// TODO

## State Management

// TODO

## Introduction Card

A key issue with many conversational experiences is end-users not knowing how to get started, leading to general questions that the Bot may not be best placed to answer. First impressions matter! An introduction card offers an opportunity to introduce the Bot's capabilities to an end user and suggests a few initial questions the user can use to get started. It's also a great opportunity to surface the personality of your Bot.

A simple introduction card is provided as standard which you can adapt as needed, a returning user card is shown on subsequent interactions when a user has completed the onboarding dialog (triggered by the Get Started button on the Introduction card)

![Intro Card Example]({{site.baseurl}}/assets/images/vatemplateintrocard.png)

## Multi-Locale support

// TODO

## Language Generation

// TODO

## Telemetry

Providing insights into the user engagement of your Bot has proven to be highly valuable. This insight can help you understand the levels of user engagement, what features of the Bot they are using (intents) along with questions people are asking that the Bot isn't able to answer - highlighting gaps in the Bot's knowledge that could be addressed through new QnA Maker articles for instance.

Integration of Application Insights provides significant operational/technical insight out of the box but this can also be used to capture specific Bot related events - messages sent and received along with LUIS and QnA Maker operations. Bot level telemetry is intrinsically linked to technical and operational telemetry enabling you to inspect how a given user question was answered and vice versa.

A middleware component combined with a wrapper class around the QnA Maker and LuisRecognizer SDK classes provides an elegant way to collect a consistent set of events. These consistent events can then be used by the Application Insights tooling along with tools like PowerBI. An example PowerBI dashboard is as part of the Bot Framework Solutions github repo and works right out of the box with every Virtual Assistant template. See the [Analytics]({{site.baseurl}}/overview/analytics) section for more information.

![Analytics Example]({{site.baseurl}}/assets/images/powerbi-conversationanalytics-luisintents.png)

To learn more about Telemetry, see the [Analytics tutorial]({{site.baseurl}}/virtual-assistant/tutorials/view-analytics).

## Deployment Automation

## Middleware

// TODO

### SetLocale Middleware

// TODO

You can find this component within the `Microsoft.Bot.Builder.Solutions` nuget library or in [this location](https://github.com/microsoft/botframework-solutions/blob/master/lib/csharp/microsoft.bot.builder.solutions/microsoft.bot.builder.solutions/Middleware/SetLocaleMiddleware.cs).

### SetSpeak Middleware

// TODO

You can find this component within the `Microsoft.Bot.Builder.Solutions` nuget library or in [this location](https://github.com/microsoft/botframework-solutions/blob/master/lib/csharp/microsoft.bot.builder.solutions/microsoft.bot.builder.solutions/Middleware/SetSpeakMiddleware.cs).

### Console Output Middleware

// TODO

You can find this component within the `Microsoft.Bot.Builder.Solutions` nuget library or in [this location](https://github.com/microsoft/botframework-solutions/blob/master/lib/csharp/microsoft.bot.builder.solutions/microsoft.bot.builder.solutions/Middleware/ConsoleOutputMiddleware.cs).

### Event Debugger Middleware

// TODO

You can find this component within the `Microsoft.Bot.Builder.Solutions` nuget library or in [this location](https://github.com/microsoft/botframework-solutions/blob/master/lib/csharp/microsoft.bot.builder.solutions/microsoft.bot.builder.solutions/Middleware/EventDebuggerMiddleware.cs).

### Content Moderator Middleware.

Content Moderator is an optional component which enables detection of potential profanity and helps check for personally identifiable information (PII). This can be helpful to integrate into Bots enabling a Bot to react to profanity or if the user shares PII information. For example, a Bot can apologise and hand-off to a human or not store telemetry records if PII information is detected.

A middleware component is provided that screen texts and surfaces output through a `TextModeratorResult` on the `TurnState` object. This middleware is not enabled by default.

You can find this component within the `Microsoft.Bot.Builder.Solutions` nuget library or in [this location](https://github.com/microsoft/botframework-solutions/blob/master/lib/csharp/microsoft.bot.builder.solutions/microsoft.bot.builder.solutions/Middleware/ContentModeratorMiddleware.cs).

## Feedback

// TODO

To learn more about the Feedback capability, see the [Feedback documentation]]({{site.baseurl}}/virtual-assistant/handbook/feedback.md).

## Onboarding

// TODO

## Example Dialogs

// TODO

## Unit Testing

// TODO

## Adding Skill Support (utterance triggering)

// TODO

## Centralised Skill authentication

// TODO

## Multi Provider Auth

// TODO

## Event Processing

// TODO

## [Continuous Integration and Continuous Deployment]

// TODO