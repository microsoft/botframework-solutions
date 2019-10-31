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

1. Set up your own [Virtual Assistant Client]({{site.baseurl/virtual-assistant/samples/virtual-assistant-client/}}).

1. Download the [Event Companion app source code]({{site.baseurl}}/tree/master/samples/android/clients/EventCompanion).

## Build and run

```
TODO: If there is any configuration information that can be provided before running the app, add it here.
```

### Run
{:.no_toc}
[Build and run your app](https://developer.android.com/studio/run) to deploy to the Android Emulator or a connected device.

#### Permissions
{:.no_toc}
No special permission required by this app for now.

## Create new widgets

Numeric widgets and toggle widgets are available in Event Companion app for now.
![Event Companion app widgets]({{site.baseurl}}/assets/images/android-event-companion-widgets.jpg)

### Numeric widget
{:.no_toc}
1. Long press on the blank area of home screen, then select widgets.

2. Choose and place a numeric widget onto home screen.

3. Configure label, event and icon of the widget. It is also possible to populate a numeric widget from predefined templates.
![Numeric widget]({{site.baseurl}}/assets/images/android-event-companion-numeric-widget-configuration.jpg)

4. Click **ADD WIDGET** to finish setting up the widget.

### Toggle widget
{:.no_toc}
1. Long press on the blank area of home screen, then select widgets to show available widgets.

2. Choose and place a toggle widget onto home screen.

3. Configure label, event, unit and icon of the widget. It is also possible to populate a toggle widget from predefined templates.
![Toggle widget]({{site.baseurl}}/assets/images/android-event-companion-toggle-widget-configuration.jpg)

4. Click **ADD WIDGET** to finish setting up the widget.

## Manage widgets

All created widgets are avaible for re-configuration on the main screen of the event companion app:
![Event companion manage widgets]({{site.baseurl}}/assets/images/android-event-companion-manage-widgets.jpg)

1. Select the widget which needs to be re-configured.

2. Modify properties.

3. Click **SAVE WIDGET** to apply the changes.