---
layout: tutorial
category: Solution Accelerators
subcategory: Enable proactive notifications
title: Handle notifications
order: 3
---

# Tutorial: {{page.subcategory}}
{:.no_toc}
## {{page.title}}
{:.no_toc}

The [**EventHandler**]({{site.repo}}/tree/master/samples/csharp/clients/event-handler.function-app) project is a sample Azure Function application that performs the following order of operations:
1. Listens for events from an **Event Hubs Instance**.
1. Read from a user preference store in **Azure Cosmos DB** to check a user's settings.
1. If the **SendNotificationToConversation** flag is true, send an event activity to a user's active conversation with the message.
1. If the **SendNotificationToMobileDevice** flag is true, send a notification to the user's mobile devce using **Azure Notifications Hubs**.
   - This implementation is not provided in this tutorial. Learn more on how to [send push notifications to specific users using Azure Notification Hub](https://docs.microsoft.com/en-us/azure/notification-hubs/notification-hubs-aspnet-backend-ios-apple-apns-notification) 

### Configure the Event Handler project
Update the **YOUR_EVENT_HUB_NAME** parameter of **Function1** class with your **Event Hubs Instance** name.

#### [Function1.cs]({{site.repo}}/tree/master/samples/csharp/clients/event-handler.function-app/Function1.cs)
{:.no_toc}

```diff
[FunctionName("EventHubTrigger")]
+ public static async Task Run([EventHubTrigger("YOUR_EVENT_HUB_NAME", Connection = "EventHubConnection")] EventData[] events, ILogger log)
{
    foreach (EventData eventData in events)
    {
        try
        {
            string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

            var data = JsonConvert.DeserializeObject<EventDataType>(messageBody);
            await SendEventToBot(data);
            await Task.Yield();
        }
        catch { }
    }
}
```

### Publish and configure the Function App

1. Publish the **EventHandler** project to your **Function Apps** resource
1. Navigate to the resource and select **Configuration**

#### Application Strings
{:.no_toc}
Select **+ New application setting** for each of the following:
- **DirectLineSecret**: YOUR_BOT_DIRECT_LINE_SECRET
    - *Located in the **Azure Bot Service** resource > **Channels***
- **DocumentDbEndpointUrl**: YOUR_COSMOS_DB_URI 
- **DocumentDbPrimaryKey**: YOUR_COSMOS_DB_PRIMARY_KEY 
    - *Located in the **Azure Cosmos DB account** resource > **Keys***

#### Connection Strings
{:.no_toc}
Select **+ New connection string** for the following:
- **EventHubConnection**: YOUR_EVENT_HUB_INSTANCE_CONNECTION_STRING