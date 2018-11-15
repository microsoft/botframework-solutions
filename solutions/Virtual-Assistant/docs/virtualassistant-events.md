# Virtual Assistant Event Architecture

## Overview

Activities are sent to and from a Bot and are typically set to an ActivityType of Message. Additional [Activity types](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-activities-entities?view=azure-bot-service-4.0&tabs=cs#event) are available including [Event](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-activities-entities?view=azure-bot-service-4.0&tabs=cs#event) which enables transmission of messages outside of the usual conversation canvas the user can see.

## Inbound Events
Events in the context of the Virtual Assistant enable the client application hosting the assistant (in a web-browser or on a device such as a car or speaker) to exchange information about the user (location, timezone, etc.) or the device (turned on, cruise control enabled, navigation destination changed, etc.).

These events are then processed by the Virtual Assistant which can react immediately (for example to suggest a task that could be completed along a route or provide a summary of the day ahead when the engine is turned on) or store the information in a user state store for use by the assistant or a skill in the future.

At the time of writing the following events are supported out of the box and persist values into a User State concept. These are used by the Skills currently in-place.

- Name: `IPA.Location`, Value: `latitude`,`latitude`
- Name: `IPA.Timezone`, Value: TimeZoneInfo Identifier (e.g. Pacific Standard Time)

In addition to these, a `ResetUser` event is available which provides a way to request that all user state information and linked accounts are removed demonstrating how a Forget-Me type experience could be initiated, no Value is required for this event.

- Name: `IPA.ResetUser`

> The mechanism for sharing User information between the Virtual Assistant and Skills is expected to change in a future release. 
> An automatic event processing and storage mechanism is being evaluated for a future release.

## Outbound Events
The same Event pattern enables the Virtual Assistant and Skills to send Events back to the client application hosting the assistant (web-page, or a device such as a car or speaker).

For example, the Virtual Assistant or a skill could send an event to a car requesting a new destination be set on the Navigation System, Change the temperature in the car, etc.  

The client application receives the event through the same [Activity](https://github.com/Microsoft/BotBuilder/blob/hub/specs/botframework-activity/botframework-activity.md) mechanism used for messages and has extensibility points for additional metadata to be attached.

## Event Debug Middleware

The Bot Framework Emulator doesn't provide the ability to send Events to a Virtual Assistant Bot which complicates scenarios that require prompts to be sent either to trigger a certain event or provide information that integration with a device would normally do automatically.

The `EventDebugMiddleware` component provides a workaround to this for use during development and testing. It detects manual events through identification of a message prefixed with `/event:` and expects the rest of the payload to follow this format `{ "Name": "EventName", "Value": "EventValue" }`. The EventName and EventValue is then transposed onto the Activity and passed on to the Bot for processing.

For example this message would result in an Activity being received by the Bot with a `ActivityType` of `Event`, ` Name` of `IPA.Location` and `Value` of a latitude, longitude pair
```
/event:{ "Name": "IPA.Location", "Value": "34.05222222222222,-118.24277777777778" }
```
This example would result in an Activity being received by the Bot with a `ActivityType` of `Event`, `Name` of `IPA.Timezone` and `Value` of `Pacific Standard Time`.
```
/event:{ "Name": "IPA.Timezone", "Value": "Pacific Standard Time" }
```
This event as detailed above clears down all state including linked accounts enabling you to test authentication and onboarding scenarios.
```
/event:{Name:"IPA.ResetUser"}
```

## Event Prompt

In some cases a Dialog may need to await arrival of an event in order to proceed. For example, Skills request an authentication token from the Virtual Assistant when needed and this is done through a exchange of events. When a user makes a request (e.g. what's in my calendar) the dialog will first request and then wait for a token event before continuing.

Another case would be where a dialog requires the user to perform an operation (e.g. pressing a button on the device).
