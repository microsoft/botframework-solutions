# Virtual Assistant Device Integration

A key scenario for Virtual Assistants is the integration into a device experience. Through the Azure Bot Service, Virtual Assistants can be surfaced through a wide variety of channels including Web-Chat, Skype, Teams, FaceBook Messenger and Slack but Direct-Line provides a REST API enabling direct integration.

This REST API and associated SDKs enables seamless integration with a broad range of devices and enables the sending and receiving of messages and events. 

Virtual Assistant scenarios can be any combination of text and voice with devices optionally have a screen to show supporting information. All of these combinations are supported and the Virtual Assistant and associated Skills enable adaptation through the Activity schema and Adaptive Cards.

The Activity schema enables responses to made up of Text and Speak representations of a response enabling a device to choose the most appropriate payload depending on the context of the device and user. The Speak representation typically being a more succinct summary of the more verbose Text response.

In addition, the Activity schema supports the inclusion of Attachments which enables the return of User Experience elements (Cards, Buttons, Images) to support a response. Adaptive Cards provide a cross-platform mechanism for rendering UX and have proven highly impactful in our early scenarios.

A device can take the Adaptive Card response in JSON format and render to the target device platform and show a visual response in support of a spoken response.

## Events

Events provides a powerful way for a device to send information from the Device (as a result of a button press, or a device start) but also to receive actions that the Virtual Assistant wishes to perform (e.g. change the navigation destination, control a device feature, etc.). See the [Events](../README.md#virtual-assistant-bot) section for more information.

## Direct Line SDK

The [Direct Line REST API](https://docs.microsoft.com/en-us/azure/bot-service/rest-api/bot-framework-rest-direct-line-3-0-api-reference?view=azure-bot-service-3.0) documentation includes links to SDKs.

## Adaptive Card SDK

You can [render Adaptive Cards](https://docs.microsoft.com/en-us/adaptive-cards/rendering-cards/getting-started) within your own application. If your platform isn't available, it's possible to plug in your own custom renderer for your particular platform.

## Speech SDK

The [Unified Speech Cognitive Services](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/overview) provide a broad range of Speech-To-Text, Text-To-Speech, Translation and Custom Voice capabilities which can easily be plugged into your Virtual Assistant. 
The [Speech Software Development Kit (SDK)](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-sdk-reference) is available for reference, making it easier to develop speech-enabled software. Different Speech SDK functions have a variety of [language support](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/supported-languages). In addition, we have integration with the [Speech Devices SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-devices-sdk-qsg) enabling custom wake word detection along with linear/circular microphone arrays.