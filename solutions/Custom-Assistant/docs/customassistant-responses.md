# Custom Assistant Responses

## Overview

Your Custom Assistant can respond in a variety of ways depending on the scenario and the users active device or conversation canvas. Through the use of the Bot Framework Activity schema both Text and Speak variations of a response are returned enabling the device to make the most appropriate choice.

The same Activity schema supports the attachment of User Experience elements (Adaptive Cards) which can be rendered across a broad range of devices and platforms enabling UX support of responses where appropriate. Where Azure Bot Service Channels (e.g. WebChat, Teams) are being used the Azure Bot Service automatically transforms messages to and from the target canvas meaning Developers don't have to worry about canvas specifics.

In [device integration](./customassistant-deviceintegration.md) scenarios you receive the Activity and Attachments enabling integration into the native experience.  

## Activity schema

The Activity schema for the Azure Bot Service can be found [here](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-activities-entities?view=azure-bot-service-4.0). The Activity schema is used for all messages including [Events](./customassistant-events.md).

## Messages and Events

Messages are a specific Type of Activity set through the `ActivityType` property and relate to Messages to and from a user that should be shown/spoken. Events are a different `ActivityType` enabling messages to be *whispered* between the client and Bot and provide an elegant mecahnism for the client to trigger events within the Custom Assistant and vice versa to perform an operation on the device. More information is in the [events](./customassistant-events.md) section. 


## Adaptive Cards

Adaptive Cards provide the ability for your Custom Assistant to return User Experience elements (e.g. Cards, Images, Buttons) alongside text base responses. If the device or conversation canvas has a screen these Adaptive Cards can be rendered across a broad range of devices and platforms providing supporting User Experience where appropriate.
>TODO Add Adaptive Card links
>TODO Add Adaptive card example

