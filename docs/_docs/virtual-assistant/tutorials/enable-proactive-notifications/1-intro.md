---
layout: tutorial
category: Virtual Assistant
subcategory: Enable proactive notifications 
title: Intro
order: 1
---

# Tutorial: {{page.subcategory}}

## {{ page.title }}

### Purpose


### Prerequisites

#### Option A: Using the Enterprise Assistant sample

The [Enterprise Assistant sample]({{site.repo}}) comes with a preconfigured Virtual Assistant project and deployment scripts to create all of the required Azure resources.

#### Option B: Using the core Virtual Assistant Template

If you are using the core Virtual Assistant Template, you must create some additional Azure resources.

1. [Create a Virtual Assistant]({{ site.baseurl }}/virtual-assistant/tutorials/csharp/create-assistant/1-intro/) to setup your Virtual Assistant environment.

1. Manually deploy the following Azure resources:

    - [Create](https://ms.portal.azure.com/#create/Microsoft.EventHub) a [Azure Event Hub](https://azure.microsoft.com/en-us/services/event-hubs/) resource
    - [Create](https://ms.portal.azure.com/#create/Microsoft.FunctionApp) a [Azure Function](https://azure.microsoft.com/en-us/services/functions/) resource
    - [Create](https://ms.portal.azure.com/#create/Microsoft.NotificationHub) a [Azure Notification Hub](https://azure.microsoft.com/en-us/services/notification-hubs/) resource
    - [Create](https://ms.portal.azure.com/#create/Microsoft.DocumentDB) a [Azure Cosmos DB](https://azure.microsoft.com/en-us/services/cosmos-db/) resource

### Time to Complete

XX minutes

### Scenario

Create an Azure solution that enables your Virtual Assistant to send proactive notifications to users.

