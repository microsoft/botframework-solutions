# Virtual Assistant Template Outline

## In this reference
- [Intro](#intro)
- [Greeting Card](#greeting-card)
- [Language Understanding](#language-understanding)
- [Telemetry](#telemetry)

## Intro

The Virtual Assistant Template brings together a number of best practices we've identified through the building of conversational experiences and automates integration of components that we've found to be highly beneficial to Bot Framework developers. This section covers some background to key decisions to help explain why the template works the way it does.

## Greeting Card

A key issue with many conversational experiences is end-users not knowing how to get started, leading to general questions that the Bot may not be best placed to answer. First impressions matter! An introduction card offers an opportunity to introduce the Bot's capabilities to an end user and suggests a few initial questions the user can use to get started. It's also a great opportunity to surface the personality of your Bot.

A simple introduction card is provided as standard which you can adapt as needed, a returning user card is shown on subsequent interactions when a user has completed the onboarding dialog (triggered by the Get Started button on the Introduction card)

![Intro Card Example](/docs/media/vatemplateintrocard.png)

## Language Understanding

### Basic Language Understanding (LUIS) intents

Every Bot should handle a base level of conversational language understanding. Greetings for example are a basic thing every Bot should handle with ease. Typically, developers need to create these base intents and provide initial training data to get started. The Virtual Assistant template provides example LU files to get you started and avoids every project having to create these each time and ensures a base level of capability out of the box.

The LU files provide the following intents across English, Chinese, French, Italian, German, Spanish.

> Cancel, Confirm, Escalate, FinishTask, GoBack, Help, Reject, Repeat, SelectAny, SelectItem, SelectNone, ShowNext, ShowPrevious, StartOver, Stop

The [LU](https://github.com/Microsoft/botbuilder-tools/blob/master/packages/Ludown/docs/lu-file-format.md) format is similar to MarkDown enabling easy modification and source control. The [LuDown](https://github.com/Microsoft/botbuilder-tools/tree/master/packages/Ludown) tool is then used to convert .LU files into LUIS models which can then be published to your LUIS subscription either through the portal or the associated [LUIS](https://github.com/Microsoft/botbuilder-tools/tree/master/packages/LUIS) CLI (command line) tool.

### Dispatch Model

A key design pattern used to good effect in the first wave of conversational experiences was to leverage Language Understanding (LUIS) and QnA Maker. LUIS would be trained with tasks that your Bot could do for an end user and QnA Maker would be trained with more general knowledge.

All incoming utterances (questions) would be routed to LUIS for analysis. If the intent of a given utterance was not identified it was marked as a None intent. QnA Maker was then used to try and find an answer for the end-user.

Whilst this pattern worked well there were two key scenarios where problems could be experienced.

- If  utterances in the LUIS model and QnA Maker overlapped sometimes slightly, this could lead to strange behavior where LUIS may try to process a question when it should have been directed to QnA Maker.
- When there were two or more LUIS models a Bot would have to invoke each one and perform some form of  intent evaluation comparison to identify where to send a given utterance. As there is no common baseline score comparison across models didn't work effectively leading to a poor user experience.

The [Dispatch model](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-tutorial-dispatch?view=azure-bot-service-4.0&tabs=csaddref%2Ccsbotconfig) provides an elegant solution to this by extracting utterances from each configured LUIS model and questions from QnA Maker and creating a central dispatch LUIS model.

This enables a Bot to quickly identify which LUIS model or component should handle a given utterance and ensures QnA Maker data is considered at the top level of intent processing not just the None intent as before.

This Dispatch tool also enables evaluation which will highlight confusion and overlap across LUIS models and QnA Maker knowledgebases highlighting issues before deployment.

The Dispatch model is used at the core of each project created using the template. It's referenced within the `MainDialog` class to identify whether the target is a LUIS model or QnA. In the case of LUIS, the secondary LUIS model is invoked returning the intent and entities as usual. Dispatcher is also used for interruption detection.

![Dispatch Example](/docs/media/dispatchexample.png)


### QnA Maker

[QnA Maker](https://www.qnamaker.ai/) provides the ability for non-developers to curate general knowledge in the format of question and answer pairs. This knowledge can be imported from FAQ data sources, product manuals and interactively within the QnaMaker portal.

Two example QnA Maker models are provided in the [LU](https://github.com/Microsoft/botbuilder-tools/blob/master/packages/Ludown/docs/lu-file-format.md) file format within the QnA folder of CognitiveModels, one for FAQ and one for chit-chat. [LuDown](https://github.com/Microsoft/botbuilder-tools/tree/master/packages/Ludown) is then used as part of the deployment script to create a QnA Maker JSON file which the [QnA Maker](https://github.com/Microsoft/botbuilder-tools/tree/master/packages/QnAMaker) CLI (command line) tool then uses to publish items to the QnA Maker knowledgebase.

![QnA ChitChat example](/docs/media/qnachitchatexample.png)

### Content Moderator

Content Moderator is an optional component which enables detection of potential profanity and helps check for personally identifiable information (PII). This can be helpful to integrate into Bots enabling a Bot to react to profanity or if the user shares PII information. For example, a Bot can apologise and hand-off to a human or not store telemetry records if PII information is detected.

A middleware component is provided that screen texts and surfaces through a ```TextModeratorResult``` on the TurnState object.

## Telemetry

Providing insights into the user engagement of your Bot has proven to be highly valuable. This insight can help you understand the levels of user engagement, what features of the Bot they are using (intents) along with questions people are asking that the Bot isn't able to answer - highlighting gaps in the Bot's knowledge that could be addressed through new QnA Maker articles for instance.

Integration of Application Insights provides significant operational/technical insight out of the box but this can also be used to capture specific Bot related events - messages sent and received along with LUIS and QnA Maker operations.

Bot level telemetry is intrinsically linked to technical and operational telemetry enabling you to inspect how a given user question was answered and vice versa.

A middleware component combined with a wrapper class around the QnA Maker and LuisRecognizer SDK classes provides an elegant way to collect a consistent set of events. These consistent events can then be used by the Application Insights tooling along with tools like PowerBI.

An example PowerBI dashboard is as part of the Bot Framework Solutions github repo and works right out of the box with every Virtual Assistant template. See the [Analytics](https://github.com/Microsoft/AI/blob/master/docs/readme.md#analytics) section for more information.

![Analytics Example](/docs/media/powerbi-conversationanalytics-luisintents.png)