---
layout: tutorial
category: Virtual Assistant
subcategory: Enable proactive notifications
title: Handle the incoming notification
order: 3
toc: true
---

# Tutorial: {{page.subcategory}}
{:.no_toc}
## {{page.title}}
{:.no_toc}

### Configure the Event Handler
Update the **Function1** class in the **EventHandler** project with your Event Hub.

#### [Function1.cs]()
{:.no_toc}

```diff
[FunctionName("EventHubTrigger")]
+ public static async Task Run([EventHubTrigger("YOUR_EVENT_HUB_NAME", Connection = "YOUR_EVENT_HUB_INSTANCE_CONNECTION_STRING")] EventData[] events, ILogger log)
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
### Azure Function resource
This sample includes an example [EventHandler Azure Function]({{site.repo}}/Samples/EnterpriseNotification/EventHandler) which is triggered by Event delivery and handles Event processing.


1. Update [Function1.cs]({{site.repo}}/samples/EnterpriseNotification/EventHandler/Function1.cs) and change the `EventHubTrigger` to reflect your Event Hub name.
1. The Azure Functions blade in the Azure Portal provides a wide range of routes to deploy the provided code to your newly created Azure Function including Visual Studio and VSCode. Follow this to deploy the sample EventHandler project.
1. Once deployed, go to the Azure Function in Azure and choose Configuration.
1. Create a new `ConnectionString` called `EventHubConnection` property and provide the same EventHub connection string as in the previous section.
1. In the `Application Settings` section create the following settings which are used bvy the Event Handler.
    - `DirectLineSecret` - Located within the Channels section of your Azure Bot Service registration. Required to communicate with your assistant and send events.
    - `DocumentDbEndpointUrl` - Located within the CosmoDB Azure Portal blade. Required to access the User Preference store.
    - `DocumentDbPrimaryKey`- Located within the CosmoDB Azure Portal blade.
