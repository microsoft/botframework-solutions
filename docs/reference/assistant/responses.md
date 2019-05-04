# Virtual Assistant Responses
Your Virtual Assistant can respond in a variety of ways depending on the scenario and the users active device or conversation canvas. Through use of the Bot Framework Activity schema, both `Text` and `Speak` variations of a response are returned enabling the device to make the most appropriate choice.

The same Activity schema supports the attachment of User Experience elements through use of [Adaptive Cards](https://www.adaptivecards.io) which can be rendered across a broad range of devices and platforms enabling visual support of responses where appropriate. Where Azure Bot Service Channels (e.g. WebChat, Teams) are being used, the Azure Bot Service automatically transforms messages to and from the target canvas meaning Developers don't have to worry about differences across channel capabilities.

In [device integration](./deviceintegration.md) scenarios you receive messages adhering to the Activity schema which may include Attachments thus enabling integration into the native experience. 


## Table of Contents
- [Activity schema](#activity-schema)
- [Messages and Events](#messages-and-events)
- [Adaptive Cards](#adaptive-cards)
- [Input Hints](#input-hints)

## Activity schema

The [Bot Framework Activity schema](https://github.com/Microsoft/BotBuilder/blob/master/specs/botframework-activity/botframework-activity.md) for the Azure Bot Service is an application-level representation of conversational actions made by humans and bots. This schema is used for all messages, including [Events](../README.md#virtual-assistant-bot).

## Messages and Events

Messages are a specific Type of Activity set through the `ActivityType` property and relate to Messages to and from a user that should be shown/spoken. Events are a different `ActivityType` enabling messages to be *whispered* between the client and Bot and provide an elegant mechanism for the client to trigger events within the Virtual Assistant and vice versa to perform an operation on the device. More information is in the [events](../README.md) section.

## Adaptive Cards

[Adaptive Cards](https://adaptivecards.io) provide the ability for your Virtual Assistant to return User Experience elements (e.g. Cards, Images, Buttons) alongside text base responses. If the device or conversation canvas has a screen these Adaptive Cards can be rendered across a broad range of devices and platforms providing supporting User Experience where appropriate.

Sample Adaptive Cards are available on the Adaptive Cards website with the [Calendar](https://adaptivecards.io/samples/WeatherLarge.html) example demonstrating the possibilities.

## Input Hints

Speech scenarios require indication from the Virtual Assistant whether further input is required so the client or device can automatically open the microphone. The `inputHint` field on the [Activity](https://github.com/Microsoft/BotBuilder/blob/hub/specs/botframework-activity/botframework-activity.md) provides the mechanism to enable this.

There are three types of Input Hint to consider within your client application.
- Accepting Input: The Virtual Assistant is ready for input but not awaiting a response. This will typically cause the client to close the microphone.
- Expecting Input: The Virtual Assistant is actively awaiting a response and this should cause the client to open the microphone.
- Ignoring Input: The Virtual Assistant is not ready to receive input which would cause the client to not offer the ability to accept input.
