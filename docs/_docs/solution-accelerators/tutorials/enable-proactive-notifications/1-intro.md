---
layout: tutorial
category: Solution Accelerators
subcategory: Enable proactive notifications 
title: Intro
order: 1
---

# Tutorial: {{page.subcategory}}
{:.no_toc}
## {{ page.title }}
{:.no_toc}

### Purpose

Enable the proactive notifications sample on a Virtual Assistant, which demonstrates the following capabilities:
- A console application that sends a sample event to an **Event Hubs Instance**
- An **Azure Function** that handles notification events and routes them to the Virtual Assistant.
- A user preference store in **Azure Cosmos DB** used by the function app to look up notification settings.
- A **Virtual Assistant** project that handles incoming notification events.

![Proactive Notifications sample architecture]({{site.baseurl}}/assets/images/ProactiveNotificationsDrawing.PNG)

### Prerequisites
#### Option: Using the Enterprise Assistant sample
{:.no_toc}
The [Enterprise Assistant sample]({{site.baseurl}}/solution-accelerators/assistants/enterprise-assistant) comes with a preconfigured Virtual Assistant project and deployment scripts to create all of the required Azure resources.

#### Option: Using the core Virtual Assistant Template
{:.no_toc}

If you are using the core Virtual Assistant Template, you must create some additional Azure resources.

1. [Create a Virtual Assistant]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/csharp/1-intro/) to setup your Virtual Assistant environment.

1. Manually deploy the following Azure resources:
    - [Create](https://ms.portal.azure.com/#create/Microsoft.EventHub) an [Event Hub](https://azure.microsoft.com/en-us/services/event-hubs/) resource
    - [Create](https://ms.portal.azure.com/#create/Microsoft.FunctionApp) an [Azure Function](https://azure.microsoft.com/en-us/services/functions/) resource
    - **Optional**: [Create](https://ms.portal.azure.com/#create/Microsoft.NotificationHub) a [Notification Hub](https://azure.microsoft.com/en-us/services/notification-hubs/) resource
         - This implementation is not provided in this tutorial. Learn more on how to [send push notifications to specific users using Azure Notification Hub](https://docs.microsoft.com/en-us/azure/notification-hubs/notification-hubs-aspnet-backend-ios-apple-apns-notification) 

### Time to Complete
20 minutes

### Scenario
Create an Azure solution that enables your Virtual Assistant to send proactive notifications to users.

