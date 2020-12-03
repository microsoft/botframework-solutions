---
category: Overview
title: What's in the Virtual Assistant template?
description: The Virtual Assistant Template brings together many best practices identified through the building of conversational experiences and automates integration of components that we've found to be highly beneficial to Bot Framework developers. This section covers some background to key decisions to help explain why the template works the way it does with links to detailed information where appropriate.
order: 3
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

## Your Assistant project

Using the template you'll end up with your Assistant project that is organized in-line with the recommended thinking on how a Bot project should be structured. You are free to restructure this as necessary but bear in mind that the provided deployment scripts expect some files to be in a consistent location.

To learn more about project structure, see the [Create Project]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/csharp/3-create-project/) documentation.

## Azure resource deployment

The comprehensive experience requires the following Azure resources to function properly, detailed [here]({{site.baseurl}}/virtual-assistant/handbook/deployment-scripts/).

- Azure Bot Service
- Azure Blob Storage
- Azure Cosmos DB
- Azure App Service Plan
- Azure Application Insights
- Bot Web App
- Language Understanding (LUIS)
- QnA Maker
- QnA Maker Web App
- QnA Maker Azure Search Service
- Content Moderator

To enable you to get started quickly, we have provided an ARM template and set of PowerShell scripts (supported cross-platform) to provide these resources along with the required LUIS models, QnAMaker knowledgebases, Dispatcher and publishing into Azure. In addition the ability to refresh the LUIS and QNA resources with any changes from your LU files.

All of the steps provided by our scripts are documented [here]({{site.baseurl}}/virtual-assistant/tutorials/deploy-assistant/web/1-intro/) if you wish to review or perform manually.

You can find the ARM template (template.json) in your **Deployment\Resources** folder or [here]({{site.repo}}/tree/master/templates/csharp/VA/VA/Deployment/Resources). The PowerShell scripts can be found in your **Deployment\Scripts** folder or [here]({{site.repo}}/tree/master/templates/csharp/VA/VA/Deployment/Scripts).

## Language

### Language Understanding
#### .lu file format
{:.no_toc}

The [LU](https://docs.microsoft.com/en-us/azure/bot-service/file-format/bot-builder-lu-file-format?view=azure-bot-service-4.0) format is similar to Markdown enabling easy modification and source control of your LUIS models and QnA information. Virtual Assistant uses these files at its core to simplify deployment and provide an ongoing source control solution.

The [BF Command Line Interface](https://github.com/microsoft/botframework-cli) tool replaces the collection of standalone tools used to manage Bot Framework bots and related services.

The [@microsoft/bf-luis-cli](https://github.com/microsoft/botframework-cli/blob/main/packages/luis/README.md) package contains the command [bf luis:convert](https://github.com/microsoft/botframework-cli/blob/main/packages/luis/README.md#bf-luisconvert) which converts .lu file(s) to a LUIS application JSON model or vice versa which can then be published to your LUIS subscription either through the portal or using [bf luis:application:publish](https://github.com/microsoft/botframework-cli/blob/main/packages/luis/README.md#bf-luisapplicationpublish).

The [@microsoft/bf-qnamaker](https://github.com/microsoft/botframework-cli/blob/main/packages/qnamaker/README.md) package contains the command [bf qnamaker:convert](https://github.com/microsoft/botframework-cli/blob/main/packages/qnamaker/README.md#bf-qnamakerconvert) which converts .qna file(s) to QnA application JSON models or vice versa which can then be published using [bf qnamaker:kb:publish](https://github.com/microsoft/botframework-cli/blob/main/packages/qnamaker/README.md#bf-qnamakerkbpublish).


All of the above is handled as part of the Deployment scripts detailed below.

#### LUIS
{:.no_toc}

Every Bot should handle a base level of conversational language understanding. Cancellation or Help, for example, is a basic thing every Bot should handle with ease. Typically, developers need to create these base intents and provide initial training data to get started. The Virtual Assistant template provides example LU files to get you started and avoids every project having to create these each time and ensures a base level of capability out of the box.

The LU files provide the following intents across English, Chinese, French, Italian, German, Spanish. 
> Cancel, Confirm, Escalate, ExtractName, FinishTask, GoBack, Help, Logout, None, ReadAloud, Reject, Repeat, SelectAny, SelectItem, SelectNone, ShowNext, ShowPrevious, StartOver, Stop

You can review these within the [**Deployment\Resources\LU**]({{site.repo}}/tree/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Deployment/Resources/LU) directory.

##### LUIS strongly-typed classes
{:.no_toc}

The [@microsoft/bf-luis-cli](https://github.com/microsoft/botframework-cli/blob/main/packages/luis/README.md) package contains the command [bf luis:generate:cs](https://github.com/microsoft/botframework-cli/tree/main/packages/luis#bf-luisgeneratecs) which generates a strongly types C# source code from an exported (json) LUIS model. As a result, you can easily reference the intents and entities as class instance members.

You'll find a **GeneralLuis.cs** and **DispatchLuis.cs** class as part of your project within the [**Services**]({{site.repo}}/tree/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Services) folder. The DispatchLuis.cs will be re-generated if you add Skills to reflect the changes made.

#### QnA Maker
{:.no_toc}

A key design pattern used to good effect in the first wave of conversational experiences was to leverage Language Understanding (LUIS) and QnA Maker together. LUIS would be trained with tasks that your Bot could do for an end-user and QnA Maker would be trained with more general knowledge and also provide personality chit-chat capabilities.

[QnA Maker](https://www.qnamaker.ai/) provides the ability for non-developers to curate general knowledge in the format of question and answer pairs. This knowledge can be imported from FAQ data sources, product manuals and interactively within the QnaMaker portal.

Two example QnA Maker models localized to English, Chinese, French, Italian, German, Spanish are provided in the [LU](https://docs.microsoft.com/en-us/azure/bot-service/file-format/bot-builder-lu-file-format?view=azure-bot-service-4.0) file format within the **Deployment\Resources\QnA** folder or [here]({{site.repo}}/tree/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Deployment/Resources/QnA).

##### Base Personality
{:.no_toc}

QnAMaker provides 5 different personality types which you can find [here](https://github.com/microsoft/BotBuilder-PersonalityChat/tree/master/CSharp/Datasets). The Virtual Assistant template includes the **Professional** personality and has been converted into the [LU](https://docs.microsoft.com/en-us/azure/bot-service/file-format/bot-builder-lu-file-format?view=azure-bot-service-4.0) file format to ease source control and deployment.

You can review this within the [**Deployment\Resources\QnA**]({{site.repo}}/tree/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Deployment/Resources/QnA) directory.

![QnA ChitChat example]({{site.baseurl}}/assets/images/qnachitchatexample.png)

#### Dispatch Model
{:.no_toc}

[Dispatch](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-tutorial-dispatch?view=azure-bot-service-4.0&tabs=csaddref%2Ccsbotconfig) provides an elegant solution bringing together LUIS models and QnAMaker knowledge-bases into one experience. It does this by extracting utterances from each configured LUIS model and questions from QnA Maker and creating a central dispatch LUIS model. This enables a Bot to quickly identify which LUIS model or component should handle a given utterance and ensures QnA Maker data is considered at the top level of intent processing not just through None intent processing as has been the case previously.

This Dispatch tool also enables model evaluation which will highlight confusion and overlap across LUIS models and QnA Maker knowledgebases highlighting issues before deployment.

The Dispatch model is used at the core of each project created using the template. It's referenced within the **MainDialog** class to identify whether the target is a LUIS model or QnA. In the case of LUIS, the secondary LUIS model is invoked returning the intent and entities as usual. Dispatcher is also used for interruption detection and Skill processing whereby your Dispatch model will be updated each time you add a new Skill.

![Dispatch Example]({{site.baseurl}}/assets/images/dispatchexample.png)


### Multi-locale support

Most conversational experiences need to serve users in a variety of languages which introduces additional complexity around ensuring:
- The users desired language is identified on each incoming message
- The appropriate language variant of Dispatch, LUIS, and QnAMaker is used to process the user's question
- Responses to the user are selected from the right locale response file (language generation).

The Virtual Assistant addresses all of the above capabilities and assists with the deployment considerations for multi-language Dispatch, LUIS, and QNAMaker resources. Localized responses for built-in capabilities are also provided.

To learn more about how multi-locale support is added, see the [localization documentation]({{site.baseurl}}/virtual-assistant/handbook/localization/).

### Language generation and responses

The Virtual Assistant has transitioned to use the new [Language Generation](https://github.com/Microsoft/BotBuilder-Samples/tree/main/experimental/language-generation#readme) capability to provide a more natural conversational experience by being able to define multiple response variations and leverage context/memory to adapt these dynamically to end-users.

Language Generation (LG) leverages a new [LG file format](https://docs.microsoft.com/en-us/azure/bot-service/file-format/bot-builder-lg-file-format?view=azure-bot-service-4.0) which follows the same Markdown approach as the LU file format mentioned earlier. This enables easy editing of responses by a broad range of roles.

LG also enables Adaptive Card responses to be defined alongside responses further simplifying management and localization of responses.

LG files for your Virtual Assistant can be found in your **responses** folder or [here]({{site.repo}}/tree/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Responses) and the Template Engine code can be found in your **Startup.cs** file.

An example of LG in use can be found [here]({{site.repo}}/blob/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Dialogs/MainDialog.cs#L244) and throughout the Virtual Assistant.

## Dialogs

Beyond the core **MainDialog** dialog two further dialogs are provided firstly to deliver core scenarios but also to provide examples to get you started. These are all wired up to provided LUIS intents so work out of the box across multiple languages.

### Main Dialog
{:.no_toc}

The [**MainDialog**]({{site.repo}}/tree/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Dialogs/MainDialog.cs) class as discussed earlier in this section is the core part of the Activity processing stack and is where all activities are processed. This is also where the Help intent is handled which returns a response as defined within the Language Generation responses. Events are also handled as part of this dialog.

#### Introduction card

A key issue with many conversational experiences is end-users not knowing how to get started, leading to general questions that the Bot may not be best placed to answer. First impressions matter! An introduction card offers an opportunity to introduce the Bot's capabilities to an end-user and suggests a few initial questions the user can use to get started. It's also a great opportunity to surface the personality of your Bot.

A simple introduction card is provided as standard which you can adapt as needed, a returning user card is shown on subsequent interactions when a user has completed the onboarding dialog (triggered by the Get Started button on the Introduction card)

![Intro Card Example]({{site.baseurl}}/assets/images/vatemplateintrocard.png)

### Onboarding Dialog
{:.no_toc}

The [**OnboardingDialog**]({{site.repo}}/blob/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Dialogs/OnboardingDialog.cs) provides an example introduction Dialog experience for users starting their first conversation. It prompts for some information which is then stored in State for future use by your assistant. This dialog demonstrates how you can use prompts and state.

## Managing state

CosmosDB is used as the default state store through the SDK provided `CosmosDbPartitionedStorage` storage provider. This provides a production-grade, scalable storage layer for your Bots state along with fast disaster recovery capabilities and regional replication where required. Features like automatic time-to-live provide additional benefits around clean-up of old conversations.

Within **Startup.cs** you can optionally choose to disable use of CosmosDbPartitionedStorage and switch to MemoryStorage often used for development operations but ensure this is reverted ahead of production deployment.

```csharp
 // Configure storage
 // Uncomment the following line for local development without Cosmos Db
 // services.AddSingleton<IStorage, MemoryStorage>();
 services.AddSingleton<IStorage>(new CosmosDbPartitionedStorage(settings.CosmosDb));
```

Deployment can be customized to omit deployment of CosmosDB and is covered in the [deployment documentation]({{site.baseurl}}/virtual-assistant/handbook/deployment-scripts/).

## Handling activities
### Activity processing
{:.no_toc}

1. Activities are first processed within your Bot through the DefaultActivityHandler.cs class found in the **Bots** folder. **OnTurnAsync** is executed and **MainDialog** processing is started.

1. The **MainDialog** dialog provided in the template derives from a base class called [ComponentDialog](https://github.com/microsoft/botbuilder-dotnet/blob/main/libraries/Microsoft.Bot.Builder.Dialogs/ComponentDialog.cs) which can be found in the  **Microsoft.Bot.Builder.Dialogs** NuGet library.

1. The **InterruptDialogAsync** handler within **MainDialog** is executed which in-turn calls LUIS to evaluate the **General** LUIS model for top intent processing. If interruption is required it's processed at this point.

1. Processing returns back to ComponentDialog which will end the dialog if interruption has been requested.

1. If the Activity is a message and there is an active dialog, the activity is forwarded on. If there is no Active dialog then RouteStepAsync on MainDialog is invoked to perform "Turn 0" processing.

1. **RouteStepAsync** within MainDialog invokes the Dispatch model to identify whether it should hand the utterance to:
    - A dialog (mapped to a LUIS intent)
    - QnAMaker (Chitchat or QnA)
    - A Skill (mapped to a Dispatcher skill intent)


### Middleware

Middleware is simply a class that sits between the adapter and your bot logic, added to your adapter's middleware collection during initialization. Every activity coming into or out of your Assistant flows through your middleware.

A number of middleware components have been provided to address some key scenarios and are included in the **Microsoft.Bot.Solutions** NuGet library or in [this location]({{site.repo}}/blob/master/sdk/csharp/libraries/microsoft.bot.solutions/Middleware).

#### Set Locale Middleware
{:.no_toc}

In multi-locale scenarios, it's key to understand the user's locale so you can select the appropriate language LUIS Models and responses for a given user. Most channels populate the **Locale** property on an incoming Message activity but there are many cases where this may not be present thus it's important to stamp a default locale on activities where this is missing so downstream components 

You can find this component within the **Microsoft.Bot.Solutions** NuGet library or in [this location]({{site.repo}}/blob/master/sdk/csharp/libraries/microsoft.bot.solutions/Middleware/SetLocaleMiddleware.cs).

#### Set Speak Middleware
{:.no_toc}

For Speech scenario's providing a fully formed SSML fragment is required in order to be able to control the voice, tone and more advanced capabilities such as pronunciation. Setting the **Speak** property on the Activity to a Speech representation should be performed as part of the Language Generation step but in cases where this is omitted we can transpose the Activity.Text property into Speak to ensure all responses have Speech variations.

The [**Set Speak Middleware**]({{site.repo}}/blob/master/sdk/csharp/libraries/microsoft.bot.solutions/Middleware/SetSpeakMiddleware.cs) provides these capabilities and only executes when the Direct-Line Speech channel is used.  An example SSML fragment is shown below:

```json
<speak version='1.0' xmlns="https://www.w3.org/2001/10/synthesis" xmlns:mstts="https://www.w3.org/2001/mstts" xml:lang="en-US">
<voice name='en-US-JessaNeural'>
<mstts:express-as type="cheerful"> 
You have the following event on your calendar: Sync Meeting at 4PM with 2 people at Conference Room 1.
</mstts:express-as></voice></speak>
```

#### Console Output Middleware
{:.no_toc}

The [**Console Output Middleware**]({{site.repo}}/blob/master/sdk/csharp/libraries/microsoft.bot.solutions/Middleware/ConsoleOutputMiddleware.cs) is a simple component for debugging that outputs incoming and outcoming activities to the console enabling you to easily see the Text/Speak responses flowing through your Bot.

#### Event Debugger Middleware
{:.no_toc}

Event Activities can be used to pass metadata between an assistant and user without being visible to the user. These events can enable a device or application to communicate an event to an assistant (e.g. being switched on) or enable an assistant to convey an action to a device to perform such as opening a deep link to an application or changing the temperature.

It can be hard to generate these activities for testing purposes as the Bot Framework Emulator doesn't provide the ability to send Activities. The [**Event Debugger Middleware**]({{site.repo}}/blob/master/sdk/csharp/libraries/microsoft.bot.solutions/Middleware/EventDebuggerMiddleware.cs) provides an elegant workaround enabling you to send messages following a specific format which are then transposed into an Event activity processed by your Assistant

For example sending this message with the middleware registered: **/event:{ "Name": "{Event name}", "Value": "{Event value}" }** would generate an Activity of type event being created with the appropriate Value.

#### Content Moderator Middleware
{:.no_toc}

Content Moderator is an optional component that enables the detection of potential profanity and helps check for personally identifiable information (PII). This can be helpful to integrate into Bots enabling a Bot to react to profanity or if the user shares PII information. For example, a Bot can apologize and hand-off to a human or not store telemetry records if PII information is detected.

[**Content Moderator Middleware**]({{site.repo}}/blob/master/sdk/csharp/libraries/microsoft.bot.solutions/Middleware/ContentModeratorMiddleware.cs) is provided that screen texts and surfaces output through a **TextModeratorResult** on the **TurnState** object. This middleware is not enabled by default.

In order to enable this you need to provision the Content Moderator Azure resource. Once created, make note of the key and endpoint provided. In the DefaultAdapter.cs class, add a new line Use(new ContentModeratorMiddleware({KEY}, {REGION})) to the list of enabled middleware.

### Interruptions
{:.no_toc}

The **MainDialog** class provided in the template derives from a base class called [ComponentDialog](https://github.com/microsoft/botbuilder-dotnet/blob/main/libraries/Microsoft.Bot.Builder.Dialogs/ComponentDialog.cs) which can be found in the  **Microsoft.Bot.Builder.Dialogs** NuGet library.

This MainDialog as part of the **OnContinueDialogAsync** handler invokes on the **InterruptDialogAsync**. This handler enables interruption logic to be processed before any utterance is processed, by default Cancel, Escalate, Help, Logout, Repeat, StartOver, Stop are handled as part of this handler enabling top-level intent processing even when you have an active dialog.

You can review this logic within [**MainDialog.cs**]({{site.repo}}/blob/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Dialogs/MainDialog.cs#L201).

### Fallback responses
{:.no_toc}

In situations where an utterance from a user isn't understood by Dispatch (and therefore LUIS, QnAMaker, and Skills) the typical approach is to send a confused message back to the user. However, this behavior can easily be overridden to call some form of fallback capability where you could use another knowledge source like a Search engine to see if there is a highly scored response that could help satisfy the user's request.

### Managing global exceptions
{:.no_toc}

Whilst exceptions are typically handled at source it's important to have a global exception handler for unexpected situations which is defined as part of the Adapter definition within [**DefaultAdapter.cs**]({{site.repo}}/blob/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Adapters/DefaultAdapter.cs).

The provided Exception handler passes information on the Exception as a Trace activity enabling it to be shown within the Bot Framework Emulator if it's being used otherwise these are suppressed. A general Error message is then shown to the user and the exception is logged through Application Insights.

```csharp
public DefaultAdapter(
    BotSettings settings,
    ICredentialProvider credentialProvider,
    IChannelProvider channelProvider,
    AuthenticationConfiguration authConfig,
    LocaleTemplateManager templateEngine,
    ConversationState conversationState,
    TelemetryInitializerMiddleware telemetryMiddleware,
    IBotTelemetryClient telemetryClient,
    ILogger<BotFrameworkHttpAdapter> logger,
    SkillsConfiguration skillsConfig = null,
    SkillHttpClient skillClient = null)
    : base(credentialProvider, authConfig, channelProvider, logger: logger)
{
    ...
    OnTurnError = HandleTurnErrorAsync;
    ...
}

private async Task HandleTurnErrorAsync(ITurnContext turnContext, Exception exception)
{
    // Log any leaked exception from the application.
    _logger.LogError(exception, $"[OnTurnError] unhandled error : {exception.Message}");

    await SendErrorMessageAsync(turnContext, exception);
    ...    
}

private async Task SendErrorMessageAsync(ITurnContext turnContext, Exception exception)
{
    try
    {
        _telemetryClient.TrackException(exception);

        // Send a message to the user.
        await turnContext.SendActivityAsync(_templateEngine.GenerateActivityForLocale("ErrorMessage"));

        // Send a trace activity, which will be displayed in the Bot Framework Emulator.
        // Note: we return the entire exception in the value property to help the developer;
        // this should not be done in production.
        await turnContext.TraceActivityAsync("OnTurnError Trace", exception.ToString(), "https://www.botframework.com/schemas/error", "TurnError");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Exception caught in SendErrorMessageAsync : {ex}");
    }
}
```

## Skill support 

The Virtual Assistant integrates Skill support for your assistant, enabling you to easily register skills through the execution of the `botskills` command-line tool. The ability to trigger skills based on utterances relies heavily on the Dispatcher which is automatically provisioned as part of your assistant deployment.

Within `MainDialog`, any dispatch intent that has been identified is matched against registered skills. If a skill is matched then Skill invocation is started with subsequent messages being routed to the Skill until the skill conversation is ended.

### Multi-provider authentication

For some assistant scenarios you may have a capability or Skill that supports multiple authentication types, the Calendar Skill for examples supports both Microsoft and Google accounts. If a user has linked their assistant to both of these there is a scenario where you need to clarify which account the user wants to use, to support this scenario the Multi Provider Auth will wrap an additional prompt around an authentication request.

The Multi Provider Authentication also provides the Skill authentication protocol whereby a Skill can request a token centrally from the Virtual Assistant rather than prompting for its own authentication.

## Channels

### Speech

The Virtual Assistant has all of the pre-requisites required for a high-quality speech experience out of the box when using Direct Line Speech. This includes ensuring all responses have speech friendly responses, middleware for SSML and configuration of the Streaming Extensions adapter. The [Enabling speech tutorial]({{site.baseurl}}/clients-and-channels/tutorials/enable-speech/1-intro/) includes further configuration steps to provision Speech and get starting with a test tool quickly.

### Microsoft Teams

The Virtual Assistant is configured out-of-the-box to integrate with [Microsoft Teams]({{site.baseurl}}/clients-and-channels/tutorials/enable-teams/1-intro/).

## Telemetry

Providing insights into the user engagement of your Bot has proven to be highly valuable. This insight can help you understand the levels of user engagement, what features of the Bot they are using (intents) along with questions people are asking that the Bot isn't able to answer - highlighting gaps in the Bot's knowledge that could be addressed through new QnA Maker articles for instance.

Integration of Application Insights provides significant operational/technical insight out of the box but this can also be used to capture specific Bot related events - messages sent and received along with LUIS and QnA Maker operations. Bot level telemetry is intrinsically linked to technical and operational telemetry enabling you to inspect how a given user question was answered and vice versa.

A middleware component combined with a wrapper class around the QnA Maker and LuisRecognizer SDK classes provides an elegant way to collect a consistent set of events. These consistent events can then be used by the Application Insights tooling along with tools like PowerBI. An example PowerBI dashboard is as part of the Bot Framework Solutions Github repo and works right out of the box with every Virtual Assistant template. See the [Analytics]({{site.baseurl}}/solution-accelerators/tutorials/view-analytics/1-intro/) section for more information.

![Analytics Example]({{site.baseurl}}/assets/images/analytics/virtual-assistant-analytics-powerbi-1.png)

To learn more about Telemetry, see the [Analytics tutorial]({{site.baseurl}}/solution-accelerators/tutorials/view-analytics/1-intro/).


## Testing

Unit testing of dialogs is an important capability for any project. A number of examples unit tests are provided as part of the Virtual Assistant and cover all capabilities provided. These can be used as a baseline to build your own additional tests.

You can find these tests in a companion project to your assistant or [here]({{site.repo}}/tree/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample.Tests).


## Continuous integration and deployment

A [Azure DevOps](https://azure.microsoft.com/en-us/solutions/devops/) [YAML](https://docs.microsoft.com/en-us/azure/devops/pipelines/yaml-schema?view=azure-devops&tabs=schema) file for Continuous Integration is included within the `pipeline` folder of your assistant and provides all the steps required to build your assistant project and generate code coverage results. This can be imported into your Azure DevOps environment to create a build.

In addition, documentation to create a [release pipeline]({{site.baseurl}}/solution-accelerators/tutorials/enable-continuous-deployment/1-intro/) is also provided enabling you to continuously deploy updates to your project to your Azure test environment and also update Dispatch, LUIS and QnAMaker resources with any changes to the LU files within source control.
