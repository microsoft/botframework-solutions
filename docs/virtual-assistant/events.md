# Handling Events With Your Virtual Assistant

When a user communicates with their Virtual Assistant, they typically send message Activities.
In more advanced scenarios, messaging clients may need to send event Activities to provide additional metadata about the user (location, timezone, etc.).

## Table of Contents
- [Inbound Events](#inbound-events)
- [Outbound Events](#outbound-events)
- [Event Debugger Middleware](#event-debug-middleware)
- [Event Prompt](#event-prompt)

## Inbound Events
When a user sends an event, this is processed by their Virtual Assistant to immediately react (like providing a summary of the day ahead when starting their car) or store information in the user state store for use later. or store the information in a user state store for use by the assistant or a skill in the future.

The following events are supported out of the box and persist values into user state. These are used by the available Skills.

- Name: `IPA.Location`, Value: `latitude`,`latitude`
- Name: `IPA.Timezone`, Value: [TimeZoneInfo](https://docs.microsoft.com/en-us/dotnet/api/system.timezoneinfo?view=netcore-2.2) identifier (e.g. Pacific Standard Time)

In addition to these, a `ResetUser` event is available which provides a way to request that all user state information and linked accounts are removed demonstrating how a Forget-Me type experience could be initiated, no Value is required for this event.

- Name: `IPA.ResetUser`

> The mechanism for sharing User information between the Virtual Assistant and Skills is expected to change. 
> An automatic event processing and storage mechanism is being evaluated for a future release.

## Outbound Events
In order to interact with a messaging client, your Virtual Assistant and Skills need to send events back to the client application.
For example, the Virtual Assistant could send an event to a vehicle to set a new destination on the navigation system, adjust the temperature, queue up new music, etc.car, etc. The client application receives the event through the same Activity.

## Event Debug Middleware

In order to test events with the [Bot Framework Emulator](https://aka.ms/botframework-emulator) (event management is unsupported), you can send events to your Virtual Assistant using the `EventDebugMiddleware` component.
This provides a workaround where messages are sent with a payload like `/event:{ "Name": "EventName", "Value": "EventValue" }`. 
The EventName and EventValue is transposed onto the Activity and passed on to the Bot for processing.

For example this message would result in an Activity being received by the Bot with a `ActivityType` of `Event`, ` Name` of `IPA.Location` and `Value` of a latitude, longitude pair
```
/event:{ "Name": "IPA.Location", "Value": "47.639620,-122.130610" }
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

In some instances, a dialog may need to wait for an event in order to proceed.
For example, authenticated Skills request a token from the Virtual Assistant - this is done through an exchange of events.
When a user makes a request (e.g. "what's in my calendar") the dialog first requests and then waits for a token event before proceeding.
Another instance may be when a dialog requires a user to perform a physical operation (e.g. if they press a button on their device).