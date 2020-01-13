---
category: Help
title: Known issues
order: 2
toc: true
---

# {{ page.title }}
{:.no_toc}

## My Microsoft App Registration could not be automatically provisioned

Some users might experience the following error when running deployment `Could not provision Microsoft App Registration automatically. Please provide the -appId and -appPassword arguments for an existing app and try again`. In this situation, [create and register an Azure AD application](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-authentication?view=azure-bot-service-4.0&tabs=csharp%2Cbot-oauth#create-and-register-an-azure-ad-application) manually.

Once created, retrieve the `Application (ClientId)` and create a new client secret on the `Certificates & secrets` pane

Run the above deployment script again but provide two new arguments `appId` and `appPassword` passing the values you've just retrieved.

> NOTE: Take special care when providing the appSecret step above as special characters (e.g. @) can cause parse issues. Ensure you wrap these parameters in single quotes.

## The Teams channel doesn't render OAuth cards.

Prior versions of the BF SDK and VA template experienced issues when using Teams whereby Authentication cards (OAuthPrompt generated) did not function as expected. This required manual changes to work around the issue which are now incorporated into the BF SDK and Virtual Assistant template. If you experience these problems please:

1. Update to Bot Framework SDK 4.4.5 or higher
2. Update your `Microsoft.Bot.Builder.Solutions` and `Microsoft.Bot.Builder.Skills` nuget packages to 4.4.4.1 or higher.

Please be aware that you **must** use [App Studio](https://docs.microsoft.com/en-us/microsoftteams/platform/get-started/get-started-app-studio) to create an Application Manifest when using Teams. Otherwise you won't be able to click any login buttons within Teams. 

It's key to ensure that under Domains and permissions in the Manifest Editor that you enter the domain `token.botframework.com` to enable clicking of the login button. 

>You cannot click the link in the Channel Page of the Bot Framework to start a conversation with your Bot through Teams.

## The message contains mention from the Teams channel

When a bot is called via mention(@) in Teams, the message will contain `<at>YOUR_BOT_NAME<\at>` and it will confuse LUIS sometimes. If you don't need the mention part, you could modify your bot's DefaultActivityHandler like below. The `RemoveRecipientMention()` function will remove mentions from `turnContext.Activity.Text`.

```
        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
+            if (turnContext.Activity.ChannelId == Channels.Msteams)
+            {
+                turnContext.Activity.RemoveRecipientMention();
+            }
```

## WebChat doesn't work with your Virtual Assistant / Skills

Virtual Assistant's created using an earlier version of the template reference an older version of the nuget packages. Update your `Bot.Builder.Skills` and `Bot.Builder.Solutions` to the latest versions (4.4.4.1) along with the Bot Framework SDK to 4.4.5 or higher. Then apply the change in the item below.

## When invoking a Skill you may experience the initial message being sent twice

We made a change to the behaviour when invoking Skills which removed the need for an additional `SkillBegin` event. This change enabled simplification of the SkillDialog logic which the latest Virtual Assistant template has incorporated. However existing assistants created using an earlier version of the template who have updated to the latest `Bot.Builder.Skills/Bot.Builder.Solutions` packages may experience a situation, whereby, when invoking a Skill the initial message is sent twice. 

This is due to a line of code which can be safely removed from your existing Assistant project. Within your `MainDialog.cs` file and the `RouteAsync` method, remove the call to `ContinueDialogAsync` by changing the existing code:

```
await dc.BeginDialogAsync(identifiedSkill.Id);

// Pass the activity we have
var result = await dc.ContinueDialogAsync();
```
To
```
var result = await dc.BeginDialogAsync(identifiedSkill.Id);
```
Ensure you have also updated to the latest `Bot.Builder.Skills/Bot.Builder.Solutions` packages .

## QnA Maker can't be entirely deployed in a region other than westus.

QnAMaker has a central Cognitive Service resource that must be deployed in `westus`, the dependent Web App, Azure Search and AppInsights resources can be installed in a broader range of regions. We have therefore fixed this QnAMaker resource to be `westus` in the ARM template (template.json) which can be found in the `deployment/resources` folder of your Virtual Assistant. When deploying in a region such as westeurope all dependent resources will be created in the requested region. This script will be updated as new regions are available.

## Telemetry Gaps / FlowAggregate errors in the PowerBI Template

If you try to use the PowerBI analytics dashboard with your Virtual Assistant / Skills and experience a `Errors in FlowAggregates` issue or experience some telemetry not being collected this likely relates to a bug experienced in the initial version of the Virtual Assistant template and Skills which has now been addressed.

1. Change `appInsights` in appSettings.config to `ApplicationInsights`

```
"ApplicationInsights": {
    "InstrumentationKey": ""
  }
```

2. Update your `BotServices.cs` file with the changes [here]({{site.baseurl}}/blob/master/templates/Virtual-Assistant-Template/csharp/Sample/VirtualAssistantSample/Services/BotServices.cs).

3. Update your `Startup.cs` file with the changes [here]({{site.baseurl}}/blob/master/templates/Virtual-Assistant-Template/csharp/Sample/VirtualAssistantSample/Startup.cs)

4. Existing data in your Application Insights may cause the error to persist. You can either drop and re-create your Application insights resource updating the appSettings.config file with the new Instrumentation key or follow these [purge instructions](https://docs.microsoft.com/en-us/rest/api/application-insights/components/purge).


## Deployment doesn't assign the newly created LUIS subscription key to the created LUIS models / LUIS Forbidden Error.

Due to a limitation with the LUIS authoring APIs the original deployment scripts weren't able to assign the newly created LUIS subscription-key to the deployed and published LUIS models. Instead, the workaround was to rely on the Starter Key meaning the Virtual Assistant and Skills work with no manual steps. 

This may cause you to also experience `Forbidden` LUIS errors when testing your Bot as you may have exhausted the quota for your starter LUIS key, changing from your starter LUIS subscription key will resolve this.

This has now been resolved in the latest deployment scripts which you can update to following [these instructions]({{site.baseurl}}/help/reference/deployment-scripts/#updating-your-deployment-scripts). If you have an existing deployment you'll have to manually perform the following steps:

1. As shown below go through **each LUIS model including Dispatch**, click Assign Resoucre and locate the appropriate subscription key and then re-publish. 

![Assign Resource]({{site.baseurl}}/assets/images/luis-assignresource.png)

2. Update the `subscriptionKey` for each LUIS model (includign Dispatch) in `cognitiveModels.json` with your new subscription key. 


## The introduction card isn't displayed when a locale is missing

There is a known issue in the Virtual Assistant when the bot doesn't pass a Locale setting at the beginning of the conversation, the Intro Card won't show up. This is due to a limitation in the current channel protocol. The StartConversation call doesn't accept Locale as a parameter.

When you're testing in Bot Emulator, you can get around this issue by setting the Locale in Emulator Settings. Emulator will pass the locale setting to the bot as the first ConversationUpdate call.

When you're testing in other environments, if it's something that you own the code, make sure you send an additional activity to the bot between the StartConversation call, and the user sends the first message:

```typescript
    directLine.postActivity({
      from   : { id: userID, name: "User", role: "user"},
      name   : 'startConversation',
      type   : 'event',
      locale : this.props.locale,
      value  : ''
    })
    .subscribe(function (id) {
      console.log('trigger "conversationUpdate" sent');
    });
```

When you're testing in an environment you don't own the code for, chances are you won't be able to see the Intro Card. Because of the current design flaw in channel protocol, we made this tradeoff so that we don't show an Intro Card with a default culture that doesn't match your actual locale. Once the StartConversation supports passing in metadata such as Locale, we will make the change immediately to support properly localized Intro Card.

## Error resolving type specified in JSON 'Microsoft.Bot.Solutions.Models.Proactive.ProactiveModel, Microsoft.Bot.Solutions...'

If you ever see this error, it's because there's a mismatch between previously saved proactive state objects and the current type definition in the running code. This is due to a schema change (mainly a move of the class which resulted in type full name change) on the `ProactiveModel` class.

To fix this issue:
- Simply locate your CosmosDB azure resource for your bot (within the same resource group), find the collection called `botstate-collection`.
- In the document list, find the one with id `ProactiveState` and delete it.
- If the bot has been running for a long time and you find it hard to find the ProactiveState document, you can also delete the entire collection if all other conversations can be deleted. After the deletion, restart the app service that hosts your bot (typically with the name like 'your bot name'+some random letters). 

Then the bot will recreate the state `-documents` when it starts if it doesn't exist,. Future converations will all be following the new schema to serialize and deserialize so everything will run smoothly.

## If Visual Studio 2019 Preview is installed, node-gyp cannot find MSBuild.exe

This is a known issue with node-gyp: [nodejs/node-gyp#1663](https://github.com/nodejs/node-gyp/issues/1663). Uninstalling Visual Studio 2019 Preview fixes the issue.

## Botskills CLI tool can't resolve trailing backslash in any of the arguments using Command Prompt as terminal

There is a known issue in the `Botskills` CLI tool during the command's execution when any of the arguments contains a **trailing backslash** using the `Command Prompt` as terminal. This is due to a parsing issue in the shell.

Example of the `connect` command with a trailing backslash in the `luisFolder` argument:
``` bash
botskills connect --botName "<YOUR_VA_NAME>" --localManifest "<YOUR_LOCAL_MANIFEST_FILE>" --luisFolder "<YOUR_LUIS_FOLDER_PATH>/" --ts
```

So, to avoid this, it's highly recommended to use `PowerShell 6` to execute the CLI tool commands. Also, you can remove the trailing backslash of the argument.

## Skill dialog telemetry is not showing up in the Power BI dashboard
In the Bot Builder SDK version 4.5.3 and below, there is a bug which causes the Activity ID and Conversation ID to be null on all telemetry logged over a web socket connection. This causes the Skill dialog telemetry to not populate properly in the [Conversational AI Power BI sample](https://aka.ms/botPowerBiTemplate). To resolve this issue, follow these steps:

1. Update to the latest Microsoft.Bot.Builder packages
    1. Add the following package source to your project: **https://botbuilder.myget.org/F/botbuilder-v4-dotnet-daily/api/v3/index.json**
    1. Update all Microsoft.Bot.Builder packages to version **4.6.0-preview-191005-1** and above
1. Add the following code to **Startup.cs**:
    ```
        // Configure telemetry
        services.AddApplicationInsightsTelemetry();
        services.AddSingleton<IBotTelemetryClient, BotTelemetryClient>();
        services.AddSingleton<ITelemetryInitializer, OperationCorrelationTelemetryInitializer>();
        services.AddSingleton<ITelemetryInitializer, TelemetryBotIdInitializer>();
        services.AddSingleton<TelemetryInitializerMiddleware>();
        services.AddSingleton<TelemetryLoggerMiddleware>();
    ```
1. Update your **DefaultAdapter.cs** and **DefaultWebsocketAdapter.cs** with the following:
    ```
      public DefaultAdapter(
            BotSettings settings,
            TemplateEngine templateEngine,
            ConversationState conversationState,
            ICredentialProvider credentialProvider,
            TelemetryInitializerMiddleware telemetryMiddleware,
            IBotTelemetryClient telemetryClient)
            : base(credentialProvider)
        {
            ...
            Use(telemetryMiddleware);
            // Uncomment the following line for local development without Azure Storage
            // Use(new TranscriptLoggerMiddleware(new MemoryTranscriptStore()));
            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new ShowTypingMiddleware());
            Use(new FeedbackMiddleware(conversationState, telemetryClient));
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
        }
    ```

For more information, refer to the following resources:
- [Bot Builder SDK issue](https://github.com/microsoft/botbuilder-dotnet/issues/2474)
- [Bot Builder SDK pull request](https://github.com/microsoft/botbuilder-dotnet/pull/2580)

## Dialogs are not ending when an error is raised in the conversation
There is a known issue in the dialogs of the Virtual Assistant and the Skill in which the executed conversation is not ending when an error is raised, this is happening in C# and in TypeScript as well.

To resolve this issue, it's necessary to add a `try/catch` in the `MainDialog` of the bots, to handle any error during the conversation:

[MainDialog.ts](https://github.com/microsoft/botframework-solutions/blob/master/templates/Virtual-Assistant-Template/typescript/samples/sample-assistant/src/dialogs/mainDialog.ts)
```typescript
protected async onContinueDialog(dc: DialogContext): Promise<DialogTurnResult> {
    try {
        …
    } catch (error) {
        …
        return await dc.endDialog();
    }
}
```

[MainDialog.cs](https://github.com/microsoft/botframework-solutions/blob/master/templates/Virtual-Assistant-Template/csharp/Sample/VirtualAssistantSample/Dialogs/MainDialog.cs)
```C#
protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
    try {
        …
    } catch (Exception ex) {
        …
        return await innerDc.EndDialogAsync().ConfigureAwait(false);
    }
}
```

For more information, check the following issues:
* [#1589](https://github.com/microsoft/botframework-solutions/issues/1589) - `OnTurnError function inside DefaultAdapter doesn't end the current dialog`
* [#2766](https://github.com/microsoft/botframework-solutions/issues/2766) - `OnTurnError is not getting called in VA`
