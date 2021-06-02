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

#### [Startup.cs]({{site.repo}}/tree/master/samples/csharp/assistants/enterprise-assistant/VirtualAssistantSample/Startup.cs)
{:.no_toc}

```diff
 public void ConfigureServices(IServiceCollection services)
{
...
+ services.AddSingleton<ProactiveState>();
...
}
```

#### [DefaultAdapter.cs]({{site.repo}}/tree/master/samples/csharp/assistants/enterprise-assistant/VirtualAssistantSample/Adapters/DefaultAdapter.cs)
{:.no_toc}

```diff
public DefaultAdapter(
            BotSettings settings,
            TemplateEngine templateEngine,
            ConversationState conversationState,
            ICredentialProvider credentialProvider,
            TelemetryInitializerMiddleware telemetryMiddleware,
            IBotTelemetryClient telemetryClient,
+           ProactiveState proactiveState)
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
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
+           Use(new ProactiveStateMiddleware(proactiveState));
        }
```

#### Handle the **BroadcastEvent** activity

The Event Handler sends a **BroadcastEvent** activity that must be handled by the Virtual Assistant.
The **_proactiveStateAccessor** contains the mapping between a user id and a previous conversation.

Update the **MainDialog** class with the below changes to the constructor and **OnEventActivityAsync** method.

#### DefaultActivityHandler.cs
{:.no_toc}

```diff
public class DefaultActivityHandler<T> : TeamsActivityHandler
    where T : Dialog
{
    private readonly Dialog _dialog;
    private readonly BotState _conversationState;
    private readonly BotState _userState;
    private IStatePropertyAccessor<DialogState> _dialogStateAccessor;
    private IStatePropertyAccessor<UserProfileState> _userProfileState;
    private LocaleTemplateManager _templateManager;
+   private MicrosoftAppCredentials _appCredentials;
+   private IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;

    public DefaultActivityHandler(
        IServiceProvider serviceProvider,
        IBotTelemetryClient telemetryClient,
+       MicrosoftAppCredentials appCredentials,
+       ProactiveState proactiveState,
        T dialog)
    {
        _dialog = dialog;
        _conversationState = serviceProvider.GetService<ConversationState>();
        _userState = serviceProvider.GetService<UserState>();
        _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
        _userProfileState = _userState.CreateProperty<UserProfileState>(nameof(UserProfileState));
        _templateManager = serviceProvider.GetService<LocaleTemplateManager>();
+       _appCredentials = appCredentials;
+       _proactiveStateAccessor = proactiveState.CreateProperty<ProactiveModel>(nameof(ProactiveModel));
    ...

    protected override async Task OnEventActivityAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
    {
    ...
+   case "BroadcastEvent":
+       {
+           var eventData = JsonConvert.DeserializeObject<EventData>(innerDc.Context.Activity.Value.ToString());
+           var proactiveModel = await _proactiveStateAccessor.GetAsync(innerDc.Context, () => new ProactiveModel());
+           var hashedUserId = MD5Util.ComputeHash(eventData.UserId);
+           var conversationReference = proactiveModel[hashedUserId].Conversation;
+
+           await innerDc.Context.Adapter.ContinueConversationAsync(_appCredentials.MicrosoftAppId, conversationReference, ContinueConversationCallback(innerDc.Context, eventData.Message), cancellationToken);
+           break;
+       }
    ...
+    /// <summary>
+    /// Continue the conversation callback.
+    /// </summary>
+    /// <param name="context">Turn context.</param>
+    /// <param name="message">Activity text.</param>
+    /// <returns>Bot Callback Handler.</returns>
+    private BotCallbackHandler ContinueConversationCallback(ITurnContext context, string message)
+    {
+        return async (turnContext, cancellationToken) =>
+        {
+            var activity = turnContext.Activity.CreateReply(message);
+            EnsureActivity(activity);
+            await turnContext.SendActivityAsync(activity);
+        };
+    }
+
+    /// <summary>
+    /// This method is required for proactive notifications to work in Web Chat.
+    /// </summary>
+    /// <param name="activity">Proactive Activity.</param>
+    private void EnsureActivity(Activity activity)
+    {
+        if (activity != null)
+        {
+            if (activity.From != null)
+            {
+                activity.From.Name = "User";
+                activity.From.Properties["role"] = "user";
+            }
+
+            if (activity.Recipient != null)
+            {
+                activity.Recipient.Id = "1";
+                activity.Recipient.Name = "Bot";
+                activity.Recipient.Properties["role"] = "bot";
+            }
+        }
+    }
   ...
}
```

#### EventData.cs

Add a new class named **EventData** with the following properties.

```diff
+    public class EventData
+    {
+        public string UserId { get; set; }

+        public string Message { get; set; }
+    }
```

#### NOTE

After you've made the above changes to your bot, please be sure to deploy the latest bot code to it's hosted Azure Web App. The Proactive Notification will only work on an Azure bot over the Direct Line channel (a locally-hosted bot is unable to receive the notifications).
