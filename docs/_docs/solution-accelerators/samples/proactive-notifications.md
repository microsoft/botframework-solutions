---
category: Solution Accelerators
subcategory: Samples
title: Proactive Notifications
order: 2
toc: true
---
# {{ page.title }}
{:.no_toc}

There are many scenarios where a Virtual Assistant needs to push activities to users. It is important to consider the range of channels you may offer to users and whether they provide a persistent conversation over time and the channel itself supports proactive message delivery. Microsoft Teams is an example of a persistent channel enabling conversations to occur over a longer period and across a range of devices. This contrasts with Web Chat which is only available for the life of the browser window. 

In addition to these common channels, mobile devices are another key end-user channel and these same notifications/messages should be delivered as appropriate to these devices. 

This sample demonstrates how to build a notification broadcast solution using a Virtual Assistant and related Azure resources. Each implementation will vary significantly, so this is available as a minimum viable product (MVP) to get started. 

This sample includes proactive notifications, enabling scenarios such as: 

- Send notifications to your users that the Virtual Assistant would like to start a conversation, thus allowing the user to trigger when they are ready to have this discussion (e.g., a user receives a notification "your training is due", allowing them to initiate the conversation about what training is required) 

- Initiate a proactive dialog with your users through an open channel such as Microsoft Teams (e.g., "Benefits enrollment just opened; would you like to know more about benefits?") 

![Proactive Notifications sample architecture]({{site.baseurl}}/assets/images/ProactiveNotificationsDrawing.PNG)

### Event Producer
{:.no_toc}

Azure Functions are used to collect events from upstream systems and convert them into a canonical event schema before handing over to the Event Hub for centralized handling. In this sample, we simulate this event generation functionality for ease of testing by using the console application located [here](/samples/EnterpriseNotification/EventProducer).

### Azure Event Hub
{:.no_toc}

In this sample, the Azure Event Hub is the centralized service that manages events gathered from different parts of the system and sent through the Azure Function aforementioned. For any event to reach an end user, it has to flow into the Azure Event Hub first.

### Azure Functions - Event Handler
{:.no_toc}

After an event is posted to the Azure Event Hub, an Azure Function service is triggered to process them. The background to the use of Azure Functions is as follows:

- Azure Functions natively support triggers against a variety of Azure services, Event Hub trigger is one of these.
- Azure Functions scales against Event Hub services by managing locks on partitions of the Azure Event Hub internally as part of the framework.

#### Notification Handler (Trigger)
{:.no_toc}

The triggering element of the Azure function is handled as part of the [EventHandler](/samples/EnterpriseNotification/EventHandler). The **Run** method within [Function1.cs]({{site.repo}}/samples/EnterpriseNotification/EventHandler/Function1.cs) is automatically invoked once an event is available.

#### Notification Handler (Run)
{:.no_toc}

Following the trigger the following steps are performed as part of the same [EventHandler]({{site.repo}}/samples/EnterpriseNotification/EventHandler) example:

- Unpack the event
- Read from a UserPreference store to check user's profile settings
- If the user has 'SendNotificationToMobileDevice' flag to be true, then send a notification to user's mobile device with the event content.
- If the user has 'SendNotificationToConversation' flag to be true, then send a message to the bot with the event content.

This sample uses CosmosDB as the UserPreference store but can be modified to reflect an existing store you may already have in place. The code will check if there's a record for the particular user. If not, it will then add a record with default settings of both destinations set to true.

This sample doesn't include the implementation for sending a notification to mobile devices as this requires additional configuration. You can refer to [this documentation](https://docs.microsoft.com/en-us/azure/notification-hubs/notification-hubs-aspnet-backend-ios-apple-apns-notification) for more information.

The message the Event Handler sends to the bot is an event Activity, with the name **BroadcastEvent** and value is set to the data received rom the Event Hub.

### Notification Hub
{:.no_toc}

[Notification Hubs](https://azure.microsoft.com/en-us/services/notification-hubs) provide the capability to delivery notifications to end user devices. Please refer to [this documentation](https://docs.microsoft.com/en-us/azure/notification-hubs/notification-hubs-aspnet-backend-ios-apple-apns-notification) for additional steps to perform this integration as needed.


## Deploy
Test
