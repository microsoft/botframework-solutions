---
layout: tutorial
category: Virtual Assistant
subcategory: Enable proactive notifications
title: Send a sample notification
order: 2
---

# Tutorial: {{page.subcategory}}

## {{page.title}}
### Create an Event Hubs Instance resource
{:.no_toc}
1. In your **Event Hub Namespace** resource, navigate to **Entities** > **Event Hubs**
1. Select **+ Event Hub**, provide a name and select **Create**
    1. Make note of the **Event Hub Name** for later
1. In your **Event Hubs Instance** resource, navigate to **Settings** > **Shared access policies**
1. Select **+ Add**, provide a name and select **Create**
    1. Make note of the **Connection string-primary key** for later

### Update the Event Producer project
{:.no_toc}

Update the [**appSettings.json**]()
```json
{
  "EventHubName": "YOUR_EVENT_HUB_INSTANCE_NAME",
  "EventHubConnectionString": "YOUR_EVENT_HUB_INSTANCE_CONNECTION_STRING",
  "UserId":  "YOUR_USER_ID"
}
```