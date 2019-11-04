---
layout: tutorial
category: Solution Accelerators
subcategory: Enable proactive notifications
title: Set up the Virtual Assistant
order: 4
toc: true
---

# Tutorial: {{page.subcategory}}
{:.no_toc}
## {{page.title}}
{:.no_toc}
### Option: Using the Enterprise Assistant sample
{:.no_toc}
The Enterprise Assistant sample is already configured with the **Proactive State Middleware** necessary event handling. Continue to the next step.

### Option: Using the core Virtual Assistant Template
{:.no_toc}

#### Add the Proactive State Middleware
{:.no_toc}

For messages to be delivered to a user's conversation, a **ConversationReference** needs to be persisted in the Virtual Assistant and used to resume the existing conversation.

Update both the **Startup** and **DefaultAdapter** classes with references to **ProactiveState** and **ProactiveStateMiddleware**.

#### [Startup.cs]({{site.repo}})
{:.no_toc}

```diff
 public void ConfigureServices(IServiceCollection services)
{
...
+ services.AddSingleton<ProactiveState>();
...
}
```

#### [DefaultAdapter.cs]({{site.repo}})
{:.no_toc}

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

#### Handle the **BroadcastEvent** activity

The Event Handler sends a **BroadcastEvent** activity that must be handled by the Virtual Assistant.
The **_proactiveStateAccessor** contains the mapping between a user id and a previous conversation.

Update the **MainDialog** class with the below changes to the constructor and **OnEventActivityAsync** method.

#### MainDialog.cs
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