---
category: Virtual Assistant
subcategory: Samples
title: Enterprise Assistant
order: 1
---

# {{ page.title }}
## Contents
{:.no_toc}

* 
{:toc}

## Introduction

There are many scenarios where an enterprise-focused Assistant needs to push notifications or messages to employees.
These messages may need to be delivered on a variety of [channels](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-channels?view=azure-bot-service-4.0) and customized by each employees.
It's important to consider the range of channels you wish to offer to customers and whether they provide a persistent conversation over time and the channel itself supports proactive message delivery. Microsoft Teams is an example of a persistent channel enabling conversations to occur over a longer period of time and across a range of devices. This contrasts with WebChat which is only available for the life of the browser window.

In addition to conversational canvases mobile devices are another key end user channel and these same notifications/messages should be delivered as appropriate to these devices.

We provide this sample to demonstrate how to build a notification/broadcasting scenario using a Virtual Assistant and various Azure resources.
Each customer scenario will vary significantly, so this is an MVP (minimum viable product) to get started with.

## Sample Capabilities

This sample demonstrates the following capabilities: 

1. A console application that shows how a supporting system can create an event for a specific event and send for processing.
2. An Azure function that handles events and routes them to a User via a Bot (Virtual Assistant). In this same handler mobile application push notification can be added as additional custom steps. 
3. A User Preference store that enables user preferences for notification delivery to be stored and is used by the Azure Function.
3. The extensions to a Bot required to display an event to a user and also store ConversationReference objects enabling proactive message delivery.

## Sample Architecture

![Enterprise Notifications sample architecture]({{ site.baseurl }}/assets/images/enterprisenotifications-architecture.png)

### Event Producer

Azure Functions are used to collect events from upstream systems and convert them into a canonical event schema before handing over to the Event Hub for centralized handling. In this sample, we simulate this event generation functionality for ease of testing by using the console application located [here](/samples/EnterpriseNotification/EventProducer).

### Azure Event Hub

In this sample, the Azure Event Hub is the centralized service that manages events gathered from different parts of the system and sent through the Azure Function aforementioned. For any event to reach an end user, it has to flow into the Azure Event Hub first.

### Azure Functions - Event Handler

After an event is posted to the Azure Event Hub, an Azure Function service is triggered to process them. The background to the use of Azure Functions is as follows:

- Azure Functions natively support triggers against a variety of Azure services, Event Hub trigger is one of these.
- Azure Functions scales against Event Hub services by managing locks on partitions of the Azure Event Hub internally as part of the framework.

#### Notification Handler (Trigger)

The triggering element of the Azure function is handled as part of the [EventHandler](/samples/EnterpriseNotification/EventHandler). The `Run` method within [Function1.cs]({{site.repo}}/samples/EnterpriseNotification/EventHandler/Function1.cs) is automatically invoked once an event is available.

#### Notification Handler (Run)

Following the trigger the following steps are performed as part of the same [EventHandler]({{site.repo}}/samples/EnterpriseNotification/EventHandler) example:

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

The assistant is responsible for surfacing the message received from the Event Handler back to the user. An example project is located [here]({{site.repo}}/samples/EnterpriseNotification/VirtualAssistant) which has a small number of extensions compared to a normal Virtual Assistant. 

### Adaptive Cards and Web Dashboards

When Notification Handler handles events emitted from Azure Event Hub, it can persist the events into a user data store. 

This would enable user/system administrator to look at the events later on from a Web Dashboard where AdaptiveCards and other Web components can be used to render them to provide companion experiences to the assistant. This part is not included in the sample implementation at the time.


## Next Steps

<div class="card-group">
    <div class="card">
        <div class="card-body">
            <h4 class="card-title">Set up Enterprise Notifications for a Virtual Assistant</h4>
            <p class="card-text">Steps for configuring the Enterprise Notifications sample.</p>
        </div>
        <div class="card-footer" style="display: flex; justify-content: center;">
            <a href="{{site.baseurl}}/howto/samples/enterprisenotifications" class="btn btn-primary">Get Started</a>
        </div>
    </div>
</div>
