![Bot Framework Solutions](/docs/media/bot_framework_solutions_header.png)

In Enterprise world, there is often the need of notifying employees for various things, on various channels. With the bots as part of the system, there's often the need of an approach to be able to broadcast notifications to employees through ongoing conversational channels such as Teams, as well as sending notifications directly onto employees' mobile devices. This sample is an MVP (minimum viable product) that demonstrates how to build the notification/broadcasting scenario with Virtual Assistant and various Azure resources.

# Prerequisites:

In order for this sample to work, you'll need the following azure services:

Azure Event Hub (https://azure.microsoft.com/en-us/services/event-hubs/)
Azure Function (https://azure.microsoft.com/en-us/services/functions/)
Azure Notification Hub (https://azure.microsoft.com/en-us/services/notification-hubs/)
Azure Cosmos DB (https://azure.microsoft.com/en-us/services/cosmos-db/)

# Flow:

![Enterprise Notification System Architecture](/docs/media/sample-notification-system-architecture.png)

> Azure Function - (looking for situation)

This is the service that collects various types of events from existing system and reformat them before sending over to the Event Hub for centralized handling. In this sample, we simulate this functionality by using the console application located under:

/samples/EnterpriseNotification/EventProducer

> Azure Event Hub

In this sample, Azure Event Hub is the centralized service that manages events gathered from different parts of the system and sent through the Azure Function aforementioned. If we want any event to reach user eventually, it has to flow into the Azure Event Hub first. In this sample, we demonstrate how to achieve it by creating a console application that sends an event to the Azure Event Hub under:

/samples/EnterpriseNotification/EventProducer

In Program.cs, we simply use Azure EventHub library (Microsoft.Azure.EventHubs) to post an event to an Azure Event Hub service.

> Azure Function - Notification Handler

After an event is posted to the Azure Event Hub, we use an Azure Function service to handle events. The reason we use Azure Function is as follows:
- Azure Function can easily setup triggers against different Azure services as sources, Event Hub trigger is one of those.
- Azure Function is easy to scale against Event Hub services by managing locks on partitions of the Azure Event Hub internally as part of the framework

The Event Handler code is under:

/samples/EnterpriseNotification/EventHandler

The function we created is within Function1.cs, under the static method 'Run'. In there we can specify EventTrigger info using:

```csharp
public static async Task Run([EventHubTrigger("bfvatestted-testhub", Connection = "EventHubConnection")] EventData[] events, ILogger log)`
```

The 'Connection' property is configured in AppSettings of the Azure Function.

Once an event is posted into the Event Hub, an instance of the Azure Function will be created with the list of events being passed. Then the 'Run' function will launch. 

This function performs the following tasks:
- Unpack the event
- Read from a UserPreference store to check user's profile settings
- If the user has 'SendNotificationToMobileDevice' flag to be true, then send a notification to user's mobile device with the event content.
- If the user has 'SendNotificationToConversation' flag to be true, then send a message to the bot with the event content.

We're using Cosmos DB as UserPreference store. The code will check if there's a record for the particular user existing. If yes then just read from the store, and if no then add a record with both settings to be true by default.

When sending a notification to user's mobile device, this sample doesn't include the implementation for that because it requires a lot of configuration. You can check documentation // https://docs.microsoft.com/en-us/azure/notification-hubs/notification-hubs-aspnet-backend-ios-apple-apns-notification for reference.

When sending a message to the bot to trigger a message sent to the user, the Azure Function requires a few more AppSettings to work:

"DirectLineSecret"
"DocumentDbEndpointUrl"
"DocumentDbPrimaryKey"

Once you add them into the AppSettings of the Azure Function the sample will work properly.

The message the Event Handler is sending to the bot is an event, with the name 'BroadcastEvent' and value as the event it receives from the Event Hub.

> Notification Hub

This is the service that the Event Handler uses to send out a notification to user's devices. (https://azure.microsoft.com/en-us/services/notification-hubs/). Please refer to the link above (and here: https://docs.microsoft.com/en-us/azure/notification-hubs/notification-hubs-aspnet-backend-ios-apple-apns-notification) for additional work needed to get it working.

> Virtual Assistant

This is the bot that will send the message it receives from the Event Handler back to the user. The code is under:

/samples/EnterpriseNotification/VirtualAssistant

This is the code that handles 'BroadcastEvent' event type:

``` csharp
case "BroadcastEvent":
    var eventData = JsonConvert.DeserializeObject<EventData>(dc.Context.Activity.Value.ToString());

    var proactiveModel = await _proactiveStateAccessor.GetAsync(dc.Context, () => new ProactiveModel());

    var conversationReference = proactiveModel[MD5Util.ComputeHash(eventData.UserId)].Conversation;
    await dc.Context.Adapter.ContinueConversationAsync(_appCredentials.MicrosoftAppId, conversationReference, ContinueConversationCallback(dc.Context, eventData.Message), cancellationToken);
    break;
```

The '_proactiveStateAccessor' is the state that contains a mapping between user id and previously saved user conversation. It retrieves the proactive state from a store that was previously saved by enabling ProactiveStateMiddleware in DefaultAdapter.cs:

``` csharp
Use(new ProactiveStateMiddleware(proactiveState));
```

This middleware will save user's conversation reference objects into the state so it can be used later to send message to user proactively.

With all this code in place when an event is being sent to a user through the bot the user will get the message in the ongoing conversation in a channel. Not all channels support proactive message at this moment. Webchat, directline and emulator are the ones we are certain that proactive messages can be supported. If you user other channels, it'll be up to the channel setting to determine whether that channel will route the message back to the user properly

> Adaptive Cards, Web Widget and Web Dashboards

When Notification Handler handles events emitted from Azure Event Hub, it can persist the events into a user data store. This will enable user/system administrator to look at the events later on from a Web Dashboard and we can use AdaptiveCards and Web Widget components to render them to provide a better and close to conversational experience. This part is not included in the sample implementation but should be easy to extend.