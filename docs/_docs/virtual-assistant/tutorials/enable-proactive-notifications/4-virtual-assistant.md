---
layout: tutorial
category: Virtual Assistant
subcategory: Enable proactive notifications
title: Configure a Virtual Assistant
order: 4
---

# Tutorial: {{page.subcategory}}

## {{page.title}}

### Option A: Using the Enterprise Assistant sample

The Enterprise Assistant sample is configured with the **Proactive State Middleware** and the **BroadcastEvent** handling. You may continue to the next step.

### Option B: Using the core Virtual Assistant Template

#### ProactiveState Middleware
{:.no_toc}

In order to be able to deliver messages to a conversation the end user must already have had an interaction with the assistant. As part of this interaction a `ConversationReference` needs to be persisted and used to resume the conversation.

We provide a middleware component to perform this ConversationReference storage which can be found in the Bot.Builder.Solutions package.

1. Add this line to your `Startup.cs` to register the proactive state.

##### Startup.cs
```diff
 public void ConfigureServices(IServiceCollection services)
{
...
+ services.AddSingleton<ProactiveState>();
...
}
```

##### DefaultAdapter.cs
```diff
public DefaultAdapter(
            BotSettings settings,
            TemplateEngine templateEngine,
            ConversationState conversationState,
            ICredentialProvider credentialProvider,
            TelemetryInitializerMiddleware telemetryMiddleware,
            IBotTelemetryClient telemetryClient,
+            ProactiveState proactiveState)
            : base(credentialProvider)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"{exception.Message}"));
                await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"{exception.StackTrace}"));
                await turnContext.SendActivityAsync(templateEngine.EvaluateTemplate("errorMessage"));
                telemetryClient.TrackException(exception);
            };

            Use(telemetryMiddleware);

            // Uncomment the following line for local development without Azure Storage
            // Use(new TranscriptLoggerMiddleware(new MemoryTranscriptStore()));
            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new ShowTypingMiddleware());
            Use(new FeedbackMiddleware(conversationState, telemetryClient));
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
+            Use(new ProactiveStateMiddleware(proactiveState));
        }
```

#### Handle the **BroadcastEvent** Activity
The following code handles the `BroadcastEvent` event type sent by the Azure function and is added to the Event Handling code. Within Virtual Assistant this is handled by `OnEventAsync` within MainDialog.cs.

The `_proactiveStateAccessor` is the state that contains a mapping between UserId and previously persisted conversation. It retrieves the proactive state from a store previously saved by enabling the `ProactiveStateMiddleware`.


##### MainDialog.cs
{:.no_toc}

```diff
public class MainDialog : RouterDialog
{
    private BotServices _services;
    private BotSettings _settings;
    private TemplateEngine _templateEngine;
    private ILanguageGenerator _langGenerator;
    private TextActivityGenerator _activityGenerator;
    private OnboardingDialog _onboardingDialog;
    private IStatePropertyAccessor<SkillContext> _skillContext;
    private IStatePropertyAccessor<OnboardingState> _onboardingState;
    private IStatePropertyAccessor<List<Activity>> _previousResponseAccessor;
+    private MicrosoftAppCredentials _appCredentials;
+    private IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;

    public MainDialog(
        IServiceProvider serviceProvider,
        IBotTelemetryClient telemetryClient,
+        MicrosoftAppCredentials appCredentials,
+        ProactiveState proactiveState)
        : base(nameof(MainDialog), telemetryClient)
    {
        _services = serviceProvider.GetService<BotServices>();
        _settings = serviceProvider.GetService<BotSettings>();
        _templateEngine = serviceProvider.GetService<TemplateEngine>();
        _langGenerator = serviceProvider.GetService<ILanguageGenerator>();
        _activityGenerator = serviceProvider.GetService<TextActivityGenerator>();
        _previousResponseAccessor = serviceProvider.GetService<IStatePropertyAccessor<List<Activity>>>();
        TelemetryClient = telemetryClient;
+        _appCredentials = appCredentials;
+        _proactiveStateAccessor = proactiveState.CreateProperty<ProactiveModel>(nameof(ProactiveModel));
    ...

    protected override async Task OnEventActivityAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
    {
    ...
+        case "BroadcastEvent":
+            var eventData = JsonConvert.DeserializeObject<EventData>(dc.Context.Activity.Value.ToString());
+
+            var proactiveModel = await _proactiveStateAccessor.GetAsync(dc.Context, () => new ProactiveModel());
+
+            var conversationReference = proactiveModel[MD5Util.ComputeHash(eventData.UserId)].Conversation;
+            await dc.Context.Adapter.ContinueConversationAsync(_appCredentials.MicrosoftAppId, conversationReference, ContinueConversationCallback(dc.Context, eventData.Message), cancellationToken);
+            break;
    ...
    }
}
```