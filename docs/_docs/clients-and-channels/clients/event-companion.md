---
category: Clients and Channels
subcategory: Clients
title:  Event Companion (Android)
description: The **Event Companion** app enables you to create widgets that will responds to custom events sent from your Virtual Assistant
order: 2
toc: true
---
# {{ page.title }}
{:.no_toc}
{{page.description}}

## Architecture
![Virtual Assistant Client (Android) overview diagram]({{site.baseurl}}/assets/images/android-virtual-assistant-client-architecture.png)

## Prerequisites
1. Set up your own [Virtual Assistant Client]({{site.baseurl}}/clients-and-channels/clients/virtual-assistant-client).

1. Download the [Event Companion app source code]({{site.repo}}/tree/master/samples/android/clients/EventCompanion).

## Build and run
### Run
{:.no_toc}
[Build and run your app](https://developer.android.com/studio/run) to deploy to the Android Emulator or a connected device.

## Create new widgets

Create sample numeric and toggle widgets.

Numeric widgets and toggle widgets are available in Event Companion app for now.
![Event Companion app widgets]({{site.baseurl}}/assets/images/android-event-companion-widgets.jpg)

### Numeric widget
{:.no_toc}
1. Long press on a blank area of the home screen, then select **Widgets**.

1. Select a numeric widget and drag onto the the home screen.

1. Configure:
- **Label**: Widget label
- **Event**: The name value of an event activity
- **Icon**: Widget icon
Predefined templates are available to populate a numeric widget for common scenarios.
![Numeric widget]({{site.baseurl}}/assets/images/android-event-companion-numeric-widget-configuration.jpg)

1. Click **Add Widget** to finish placing on the home screen.

### Toggle widget
{:.no_toc}
1. Long press on a blank area of the home screen, then select **Widgets**.

1. Select a toggle widget and drag onto the the home screen.

1. Configure:
- **Label**: Widget label
- **Event**: The name value of an event activity
- **Icon**: Widget icon
Predefined templates are available to populate a toggle widget for common scenarios.
![Toggle widget]({{site.baseurl}}/assets/images/android-event-companion-toggle-widget-configuration.jpg)

1. Click **Add Widget** to finish setting up the widget.

## Manage widgets
All created widgets can be reconfigured from the main screen of the **Event Companion** app.
![Event companion manage widgets]({{site.baseurl}}/assets/images/android-event-companion-manage-widgets.jpg)

1. Select the widget which needs to be reconfigured.

1. Modify properties.

1. Select **Save Widget** to apply the changes.