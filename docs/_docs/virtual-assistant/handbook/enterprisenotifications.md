---
category: Virtual Assistant
subcategory: Handbook
title: Enable the Enterprise Notifications solution for a Virtual Assistant
description: Steps for configuring the Enterprise Notifications sample
order: 8
---

# {{ page.title }}
{:.no_toc}

## In this topic
{:.no_toc}

* 
{:toc}

## Prerequisites

1. [Create a Virtual Assistant]({{ site.baseurl }}/tutorials/csharp/create-assistant/1_intro/) to setup your Virtual Assistant environment.

1. Manually deploy the following Azure resources:

    - [Create](https://ms.portal.azure.com/#create/Microsoft.EventHub) a [Azure Event Hub](https://azure.microsoft.com/en-us/services/event-hubs/) resource
    - [Create](https://ms.portal.azure.com/#create/Microsoft.FunctionApp) a [Azure Function](https://azure.microsoft.com/en-us/services/functions/) resource
    - [Create](https://ms.portal.azure.com/#create/Microsoft.NotificationHub) a [Azure Notification Hub](https://azure.microsoft.com/en-us/services/notification-hubs/) resource
    - [Create](https://ms.portal.azure.com/#create/Microsoft.DocumentDB) a [Azure Cosmos DB](https://azure.microsoft.com/en-us/services/cosmos-db/) resource

1. Install the [Bot Framework Emulator](https://aka.ms/botframeworkemulator) to use in testing.

## Event Producer

This sample includes an example [Event Producer]({{site.repo}}/samples/EnterpriseNotification/EventProducer) console application that sends an Event to the Event Hub for processing simulating creation of a notification.

- Update `appSettings.json` with the `EventHubName` and `EventHubConnectionString` which you can find by going to your EventHub resource, creating an instance and then a `Shared Access Policy`

### Azure Function - Event Handler
This sample includes an example [EventHandler Azure Function]({{site.repo}}/Samples/EnterpriseNotification/EventHandler) which is triggered by Event delivery and handles Event processing.


1. Update [Function1.cs]({{site.repo}}/samples/EnterpriseNotification/EventHandler/Function1.cs) and change the `EventHubTrigger` to reflect your Event Hub name.
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

## Testing and Validation

Now events can be sent to a user through your Virtual Assistant in an active conversation.

### Bot Framework Emulator

Event generation must generate Events with the same `UserId` as the Emulator is using so the existing conversation can be matched and notifications can be delivered.

![UserId Settings]({{ site.baseurl }}/assets/images/emulator-userid.png)

1. In the **Bot Framework Emulator**, navigate to **Settings** and provide a guid to represent a simulated user ID. This will ensure any conversations with your Assistant use the same user ID.

1. Begin a conversation with your Assistant to create a proactive state record for future user.

## Event Producer

1. Copy the user ID used in the **Bot Framework Emulator** into the `SendMessagesToEventHub` method within `Program.cs` of the **Event Producer**. 
This ensures any notifications sent are routed to your active conversation.


1. Run the **Event Producer** to generate a message and observe that the message is shown within your session.

![Enterprise Notification Demo]({{ site.baseurl }}/assets/images/enterprisenotification-demo.png)