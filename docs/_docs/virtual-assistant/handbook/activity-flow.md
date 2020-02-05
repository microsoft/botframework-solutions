---
category: Virtual Assistant
subcategory: Handbook
title: Activity Handling
description: Manage routing incoming activities, including handling interruptions.
order: 1
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

## Introduction
The Virtual Assistant provides foundational logic for handling incoming user activities. It uses a combination of concepts from the Bot Builder SDK v4 and base classes that enable additional scenarios.

## Adapters and Middleware
Incoming activities are initially received through the BotAdapter implementation, processed through the configured Middleware pipeline, then routed onto the Assistant's dialog stack. The **DefaultAdapter** in the Virtual Assistant template provides a set of Middleware out of the box including the following:

- **Telemetry Middleware** - Configures Application Insights telemetry logging.
- **Transcript Logger Middleware** - Configures conversation transcript logging.
- **Show Typing Middleware** - Sends typing indicators from the bot.
- **Feedback Middleware** - Configures the [Feedback]({{site.baseurl}}/virtual-assistant/handbook/feedback) feature.
- **Set Locale Middleware** - Configures the CurrentUICulture to enable localization scenarios.
- **Event Debugger Middleware** - Enables debugging for event activities.

## Activity Handler
After the activity is processed by the Adapter and Middleware pipeline, it is received by the **ActivityHandler** implementation. The **DefaultActivityHandler** in the template implements the TeamsActivityHandler which enables Teams scenarios out of the box. By default, the **DefaultActivityHandler** passes the incoming message into the **MainDialog**. However, this logic can be customized as needed.

## Dialogs
The **DefaultActivityHandler** passes incoming activities into the **MainDialog**. The MainDialog is composed of a repeating Waterfall Dialog that contains introduction and routing logic. The following diagram shows how the activities flow through the different methods in **MainDialog**:

![]({{site.baseurl}}/assets/images/virtual-assistant-main-dialog-flow.png)

### Interruptions
Once an activity flows into MainDialog, one of the first methods that will be called is InterruptDialogAsync(). The following interruptions are configured out of the box:
- **Switching between Skills** - Switches between connected skills based on intent.
- **Cancellation** - Cancels the current dialog.
- **Help** - Sends a help message, then resumes the waiting dialog.
- **Escalation** - Shows an escalation message.
- **Log out** - Logs the user out.
- **Repeat** - Repeats the last set of activities from the bot. Useful for speech scenarios.
- **Start over** - Starts the current dialog over.
- **Stop** - Can be implemented to stop readout in speech scenarios.