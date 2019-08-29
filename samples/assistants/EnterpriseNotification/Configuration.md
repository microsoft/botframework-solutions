# Enterprise Notifications Configuration

Follow the steps below to configure the Enterprise Notifications sample introduced [here](./readme.md)
## Prerequisites

1. A deployed Virtual Assistant. Follow the [Create your Virtual Assistant tutorial](https://github.com/microsoft/botframework-solutions/tree/master/docs#tutorials) to complete this step which will create the core Azure services required. You can use this [sample project](/samples/EnterpriseNotification/VirtualAssistant) if preferred which has the extensions applied for you, otherwise the steps are included below.

2. In addition to the core Virtual Assistant Azure services, you'll need to manually create the following Azure services:

    - [Azure Event Hub](https://azure.microsoft.com/en-us/services/event-hubs/) - Create [Here](https://ms.portal.azure.com/#create/Microsoft.EventHub)
    - [Azure Function](https://azure.microsoft.com/en-us/services/functions/) - Create [Here](https://ms.portal.azure.com/#create/Microsoft.FunctionApp)
    - [Azure Notification Hub](https://azure.microsoft.com/en-us/services/notification-hubs/) - Create [Here](https://ms.portal.azure.com/#create/Microsoft.NotificationHub)
    - [Azure Cosmos DB](https://azure.microsoft.com/en-us/services/cosmos-db/) - Create [Here]()

## Event Producer

This sample includes an example [Event Producer](/samples/EnterpriseNotification/EventProducer) console application that sends an Event to the Event Hub for processing simulating creation of a notification.

- Update `appSettings.json` with the `EventHubName` and `EventHubConnectionString` which you can find by going to your EventHub resource, creating an instance and then a `Shared Access Policy`

### Azure Function - Event Handler

This sample includes an example [EventHandler Azure Function](/samples/EnterpriseNotification/EventHandler) which is triggered by Event delivery and handles Event processing.

1. Update [Function1.cs](/samples/EnterpriseNotification/EventHandler/Function1.cs) and change the `EventHubTrigger` to reflect your Event Hub name.
    ```csharp
    public static async Task Run([EventHubTrigger("YourEventHubName", Connection = "EventHubConnection")] EventData[] events, ILogger log)`
    ```
2. The Azure Functions blade in the Azure Portal provides a wide range of routes to deploy the provided code to your newly created Azure Function including Visual Studio and VSCode. Follow this to deploy the sample EventHandler project.
3. Once deployed, go to the Azure Function in Azure and choose Configuration.
4. Create a new `ConnectionString` called `EventHubConnection` property and provide the same EventHub connection string as in the previous section.
5. In the `Application Settings` section create the following settings which are used bvy the Event Handler.
    - `DirectLineSecret` - Located within the Channels section of your Azure Bot Service registration. Required to communicate with your assistant and send events.
    - `DocumentDbEndpointUrl` - Located within the CosmoDB Azure Portal blade. Required to access the User Preference store.
    - `DocumentDbPrimaryKey`- Located within the CosmoDB Azure Portal blade.

## Virtual Assistant

### ProactiveState Middleware

In order to be able to deliver messages to a conversation the end user must already have had an interaction with the assistant. As part of this interaction a `ConversationReference` needs to be persisted and used to resume the conversation.

We provide a middleware component to perform this ConversationReference storage which can be found in the Bot.Builder.Solutions package.

1. Add this line to your `Startup.cs` to register the proactive state.
```csharp
    services.AddSingleton<ProactiveState>();
```
2. Within your `DefaultAdapter.cs` add this line to the constructor
```csharp
     ProactiveState proactiveState
```
3. Within your `DefaultAdapter.cs` add this line:
```csharp
    Use(new ProactiveStateMiddleware(proactiveState));
```

### Event Handling

The following code handles the `BroadcastEvent` event type sent by the Azure function and is added to the Event Handling code. Within Virtual Assistant this is handled by `OnEventAsync` within MainDialog.cs.

The `_proactiveStateAccessor` is the state that contains a mapping between UserId and previously persisted conversation. It retrieves the proactive state from a store previously saved by enabling the `ProactiveStateMiddleware`.

Within `MainDialog.cs` add the following changes:

1. Add this variable to your `MainDialog` class.
    ```csharp
    private IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;
    ```
2. Add this line to the constructor
    ```csharp
    ProactiveState proactiveState
    ```
    and initialise the state in the constructor
    ```csharp
        _proactiveStateAccessor = proactiveState.CreateProperty<ProactiveModel>(nameof(ProactiveModel));
    ```
3. Add this event handler to your `OnEventAsync` handler to handle the `BroadcastEvent`

    ```csharp
    case "BroadcastEvent":
        var eventData = JsonConvert.DeserializeObject<EventData>(dc.Context.Activity.Value.ToString());

        var proactiveModel = await _proactiveStateAccessor.GetAsync(dc.Context, () => new ProactiveModel());

        var conversationReference = proactiveModel[MD5Util.ComputeHash(eventData.UserId)].Conversation;
        await dc.Context.Adapter.ContinueConversationAsync(_appCredentials.MicrosoftAppId, conversationReference, ContinueConversationCallback(dc.Context, eventData.Message), cancellationToken);
        break;
    ```

## Testing

With these changes in place, when an event is being sent to a user through the bot the user will get the message in the ongoing conversation in a channel. Follow the instructions below to test the end to end flow.

### Bot Framework Emulator

Event generation must generate Events with the same `UserId` as the Emulator is using so the existing conversation can be matched and notifications can be delivered.

1. Using the [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator) navigate to the Settings section and provide a Guid to represent your simulated UserId. This will ensure any conversations with your Assistant will use this UserId

    ![UserId Settings](/docs/media/emulator-userid.png)
2. Start a conversation with your assistant which will ensure a proactive state record is persisted for future use.

## Event Producer

Update `SendMessagesToEventHub` within `Program.cs` of the example [EventProducer](/samples/EnterpriseNotification/EventProducer) project to change the UserId to the one created in the previous step. This will ensure any notifications sent are routed to your active conversation.

Run the Event Producer to generate a message and observe that the message is shown within your Emulator session.

![Enterprise Notification Demo](/docs/media/enterprisenotification-demo.png)