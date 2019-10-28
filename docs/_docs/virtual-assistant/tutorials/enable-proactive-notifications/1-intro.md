---
layout: tutorial
category: Virtual Assistant
subcategory: Enable proactive notifications 
title: Intro
order: 1
toc: true
---

# Tutorial: {{page.subcategory}}
{:.no_toc}
## {{ page.title }}
{:.no_toc}

### Purpose

Enable the proactive notifications sample on a Virtual Assistant.

![Enterprise Notifications sample architecture]({{site.baseurl}}/assets/images/enterprisenotifications-architecture.png)

### Prerequisites

#### Option: Using the Enterprise Assistant sample
{:.no_toc}
The [Enterprise Assistant sample]({{site.baseurl}}/virtual-assistant/samples/enterprise-assistant) comes with a preconfigured Virtual Assistant project and deployment scripts to create all of the required Azure resources.

#### Option: Using the core Virtual Assistant Template
{:.no_toc}

If you are using the core Virtual Assistant Template, you must create some additional Azure resources.

1. [Create a Virtual Assistant]({{site.baseurl}}/virtual-assistant/tutorials/csharp/create-assistant/1-intro/) to setup your Virtual Assistant environment.

1. Manually deploy the following Azure resources:

    1. [Create](https://ms.portal.azure.com/#create/Microsoft.EventHub) an [Azure Event Hub](https://azure.microsoft.com/en-us/services/event-hubs/) resource
    1. [Create](https://ms.portal.azure.com/#create/Microsoft.FunctionApp) an [Azure Function](https://azure.microsoft.com/en-us/services/functions/) resource
    1. **Optional**: [Create](https://ms.portal.azure.com/#create/Microsoft.NotificationHub) an [Azure Notification Hub](https://azure.microsoft.com/en-us/services/notification-hubs/) resource

### Time to Complete
XX minutes

### Scenario
Create an Azure solution that enables your Virtual Assistant to send proactive notifications to users.

