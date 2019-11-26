---
layout: tutorial
category: Solution Accelerators
subcategory: Enable proactive notifications
title: Produce a notification event
order: 2
---

# Tutorial: {{page.subcategory}}
{:.no_toc}
## {{page.title}}
{:.no_toc}

The [**EventProducer**]({{site.repo}}/tree/master/samples/csharp/clients/event-producer.event-hub) project is a console application that sends a sample message to your **Event Hubs Instance**.

### Create an Event Hubs Instance resource
1. In your **Event Hub Namespace** resource, navigate to **Entities** > **Event Hubs**
1. Select **+ Event Hub**, provide a name and select **Create**
    - Make note of the **Event Hub Name** for later
1. In your **Event Hubs Instance** resource, navigate to **Settings** > **Shared access policies**
1. Select **+ Add**, provide a name and select **Create**
    - Make note of the **Connection string-primary key** for later

### Configure the Event Producer project
Update the **appSettings.json** with the values collected in the last step, as well as a random user id that you will use to test against later.

#### [appSettings.json]({{site.repo}}/tree/master/samples/csharp/clients/event-producer.event-hub/appsettings.json)
{:.no_toc}

```json
{
  "EventHubName": "YOUR_EVENT_HUB_INSTANCE_NAME",
  "EventHubConnectionString": "YOUR_EVENT_HUB_INSTANCE_CONNECTION_STRING",
  "UserId":  "YOUR_USER_ID"
}
```