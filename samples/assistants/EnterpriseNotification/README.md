![Bot Framework Solutions](/docs/media/bot_framework_solutions_header.png)

# Enterprise Notifications

## Overview

A common scenario for Enterprise Assistant scenarios is the need to push proactive notifications or messages to employees as part of a more advanced conversational experience.

These messages can be delivered to end-users through a range of channels and customised by each employee. It's important to consider the range of channels you wish to offer to customers and whether they provide a persistent conversation over time and the channel itself supports proactive message delivery. Microsoft Teams is an example of a persistent channel enabling conversations to occur over a longer period of time and across a range of devices. This contrasts with WebChat which is only available for the life of the browser window.

In addition to conversational canvases mobile devices are another key end user channel and these same notifications/messages should be delivered as appropriate to these devices.

Typically, each customer scenario varies significantly hence we provide this as an MVP (minimum viable product) which demonstrates how to build the notification/broadcasting scenario with Virtual Assistant and various supporting Azure resources.

## Sample Capabilities

This sample demonstrates the following capabilities: 

1. A console application that shows how a supporting system can create an event for a specific event and send for processing.
2. An Azure function that handles events and routes them to a User via a Bot (Virtual Assistant). In this same handler mobile application push notification can be added as additional custom steps. 
3. A User Preference store that enables user preferences for notification delivery to be stored and is used by the Azure Function.
3. The extensions to a Bot required to display an event to a user and also store ConversationReference objects enabling proactive message delivery.

## Sample Configuration

Configuration of the sample is covered in this [configuration page](Configuration.md) and covers how to configure all of the components detailed below.

## Flow

The following diagram depicts the proposed notification Architecture which is used by this sample:

![Enterprise Notification System Architecture](/docs/media/sample-notification-system-architecture.png)

### Event Producer

Azure Functions are used to collect events from upstream systems and convert them into a canonical event schema before handing over to the Event Hub for centralized handling. In this sample, we simulate this event generation functionality for ease of testing by using the console application located [here](/samples/EnterpriseNotification/EventProducer).

### Azure Event Hub

In this sample, the Azure Event Hub is the centralized service that manages events gathered from different parts of the system and sent through the Azure Function aforementioned. For any event to reach an end user, it has to flow into the Azure Event Hub first.

### Azure Functions - Event Handler

After an event is posted to the Azure Event Hub, an Azure Function service is triggered to process them. The background to the use of Azure Functions is as follows:

- Azure Functions natively support triggers against a variety of Azure services, Event Hub trigger is one of these.
- Azure Functions scales against Event Hub services by managing locks on partitions of the Azure Event Hub internally as part of the framework.

#### Notification Handler (Trigger)

The triggering element of the Azure function is handled as part of the [EventHandler](/samples/EnterpriseNotification/EventHandler). The `Run` method within [Function1.cs](/samples/EnterpriseNotification/EventHandler/Function1.cs) is automatically invoked once an event is available.

#### Notification Handler (Run)

Following the trigger the following steps are performed as part of the same [EventHandler](/samples/EnterpriseNotification/EventHandler) example:

- Unpack the event
- Read from a UserPreference store to check user's profile settings
- If the user has 'SendNotificationToMobileDevice' flag to be true, then send a notification to user's mobile device with the event content.
- If the user has 'SendNotificationToConversation' flag to be true, then send a message to the bot with the event content.

This sample uses CosmosDB as the UserPreference store but can be modified to reflect an existing store you may already have in place. The code will check if there's a record for the particular user. If not, it will then add a record with default settings of both destinations set to true.

This sample doesn't include the implementation for sending a notification to mobile devices as this requires additional configuration. You can refer to [this documentation](https://docs.microsoft.com/en-us/azure/notification-hubs/notification-hubs-aspnet-backend-ios-apple-apns-notification) for more information.

The message the Event Handler sends to the bot is an Activity of type `event`, with the name `BroadcastEvent` and value is set to the data received rom the Event Hub.

### Notification Hub

[Notification Hubs](https://azure.microsoft.com/en-us/services/notification-hubs) provide the capability to delivery notifications to end user devices. Please refer to [this documentation](https://docs.microsoft.com/en-us/azure/notification-hubs/notification-hubs-aspnet-backend-ios-apple-apns-notification) for additional steps to perform this integration as needed.

### Virtual Assistant

The assistant is responsible for surfacing the message received from the Event Handler back to the user. An example project is located [here](/samples/EnterpriseNotification/VirtualAssistant) which has a small number of extensions compared to a normal Virtual Assistant. 

### Adaptive Cards and Web Dashboards

When Notification Handler handles events emitted from Azure Event Hub, it can persist the events into a user data store. 

This would enable user/system administrator to look at the events later on from a Web Dashboard where AdaptiveCards and other Web components can be used to render them to provide companion experiences to the assistant. This part is not included in the sample implementation at the time.