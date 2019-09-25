---
category: Reference
subcategory: Virtual Assistant
title: Events
description: Event activities enable contextual information to be shared between a user and assistant, without being visible to the user.
order: 6
---

# {{ page.title }}
{:.no_toc}

## In this reference
{:.no_toc}

* 
{:toc}

## Intro

[Event Activities](https://docs.microsoft.com/en-us/azure/bot-service/dotnet/bot-builder-dotnet-activities?view=azure-bot-service-3.0#event) are used to pass metadata between a bot and user without being visible to the user.

The data from these activities can be processed by an assistant to fulfill scenarios like providing a summary of the day ahead or filling semantic action slots on a Skill.

## From user to assistant
### Location

You can pass a user's coordinates to an assistant using the **VA.Location** example event.

**Activity payload**
```json
{ 
   "type":"event",
   "name":"VA.Location",
   "value":"{User latitude},{User longitude}"
}
```

**Using the [event debug middleware](#add-and-configure-the-event-debug-middleware)**
```
/event:{ "Name": "VA.Location", "Value": "47.639620,-122.130610" }
```

### Timezone
You can pass a user's timezone to an assistant using the **VA.Timezone** example event.

**Activity payload**
```json
{ 
   "type":"event",
   "name":"VA.Timezone",
   "value":"{[TimeZoneInfo.StandardName](https://docs.microsoft.com/en-us/dotnet/api/system.timezoneinfo.standardname?view=netcore-2.2#System_TimeZoneInfo_StandardName)}"
}
```

**Using the [event debug middleware](#add-and-configure-the-event-debug-middleware)**
```
/event:{ "Name": "VA.Timezone", "Value": "Pacific Standard Time" }
```

### Reset user
You can request to remove all user state and unlink accounts by passing the **VA.ResetUser** example event.

**Activity payload**
```json
{ 
   "type":"event",
   "name":"VA.ResetUser"
}
```

**Using the [event debug middleware](#add-and-configure-the-event-debug-middleware)**
```
/event:{ "Name": "VA.ResetUser"}
```

## From assistant to user
### Open default applications
To be tightly integrated with a user's messaging client, a Virtual Assistant needs to send events back to the client application.
The **OpenDefaultApp** example event is used in conjunction with the [Virtual Assistant Client (Android) sample)]({{ site.baseurl }}/howto/samples/vaclient_android/) to demonstrate samples of using metadata

**Activity payload**
```json
{ 
   "type":"event",
   "name":"OpenDefaultApp",
   "value":{ 
      "MusicUri":"{Music player link}",
      "MapsUri":"geo:{Latitude},{Longitude}",
      "TelephoneUri":"{Telephone number}",
      "MeetingUri":"{Microsoft Teams meeting link}"
   }
}
```

## Add and configure the event debug middleware
Native event activities are not supported on the [Bot Framework Emulator](https://aka.ms/botframework-emulator), you can work around this using the`EventDebugMiddleware` class that comes with the Virtual Assistan template.
You can send messages with a string payload following the format: `/event:{ "Name": "{Event name}", "Value": "{Event value}" }`. The middleware tranposes these values onto an event activity to be processed.
