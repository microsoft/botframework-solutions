---
layout: tutorial
category: Virtual Assistant
subcategory: Enable proactive notifications
title: Set up the event producer
order: 2
toc: true
---

# Tutorial: {{page.subcategory}}
{:.no_toc}
## {{page.title}}
{:.no_toc}

### Create an Event Hubs Instance resource
1. In your **Event Hub Namespace** resource, navigate to **Entities** > **Event Hubs**
1. Select **+ Event Hub**, provide a name and select **Create**
    1. Make note of the **Event Hub Name** for later
1. In your **Event Hubs Instance** resource, navigate to **Settings** > **Shared access policies**
1. Select **+ Add**, provide a name and select **Create**
    1. Make note of the **Connection string-primary key** for later

### Update the Event Producer project

Update the **appSettings.json** of the **EventProducer** project with your  **Event Hubs Instance** name, connection string, and a random user id that you will reference later.

#### [appSettings.json]({{site.repo}})
{:.no_toc}

```json
{
  "EventHubName": "YOUR_EVENT_HUB_INSTANCE_NAME",
  "EventHubConnectionString": "YOUR_EVENT_HUB_INSTANCE_CONNECTION_STRING",
  "UserId":  "YOUR_USER_ID"
}
```