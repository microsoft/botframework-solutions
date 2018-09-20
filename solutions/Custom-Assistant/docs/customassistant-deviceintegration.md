# Custom Assistant Device Integration

## Overview

A key scenario for Custom Assistants is the integration into a device experience. Through the Azure Bot Service, Custom Assistants can be surfaced through a wide variety of channels including Web-Chat, Skype, Teams, FaceBook Messenger and Slack but Direct-Line provides a REST API enabing direct integration.

This REST API and associated SDKs enables seamless integration with a broad range of devices and enables the sending and receiving of messages and events. 

Custom Assistant scenarios can be any combination of text and voice with devices optionally have a screen to show supporting information. All of these combinations are supported and the Custom Assitant and associated Skills enable adaptation through the Activity schema and Adapative Cards.

The Activity schema enables responses to made up of Text and Speak representations of a response enabling a device to choose the most appropriate payload depending on the context of the device and user. The Speak representation typically being a more succint summary of the more verbose Text response.

In addition, the Activity schema supports the inclusion of Attachments which enables the return of User Experience elements (Cards, Buttons, Images) to support a response. Adaptive Cards provide a cross-platform mechanism for rendering UX and have proven highly impactful in our early scenarios.

A device can take the Adaptive Card response in JSON format and render to the target device platform and show a visual response in support of a spoken response.

# Events

Events provides a powerful way for a device to send information from the Device (as a result of a button press, or a device start) but also to receive actions that the Custom Assistant wishes to perform (e.g. change the navigation destination, control a device feature, etc.). See the [Events section](./customassistant-events.md) for more information.

## DirectLine SDK

The DirectLine REST API is documented [here](..) and SDKs are available [here](..)
> Add URLs and platform/OS support

## Adaptive Card SDK

The Adaptive Card documentation including information on available platform Renderes is available [here](..). If your platform isn't shown it's possible to plugin your own custom Renderer. 

## Speech SDK

The Unified Speech Cognitive Services provide a broad range of Speech-To-Text, Text-To-Speech, Translation and Custom Voice capabilities which can easily be plugged into your Custom Assistant.

> Add URLs, SDK support, languages