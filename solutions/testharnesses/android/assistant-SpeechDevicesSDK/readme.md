# Speech Devices SDK Integration with a Virtual Assistant

## Overview

This sample demonstrates how to integrate the [Speech Devices SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-devices-sdk-qsg) with a bot hosted on the [Microsoft Bot Framework Direct Line](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-direct-line?view=azure-bot-service-4.0&tabs=cscreatebot%2Ccsclientapp%2Ccsrunclient). This is built using an implementation of the [Virtual Assistant](https://github.com/Microsoft/AI/tree/master/solutions/Virtual-Assistant
) and sends related events to the bot on conversation startup.

* The client uses the Speech Devices SDK to demonstrate speech to text (both keyword & continuous recognition listening modes) and text to speech.
* The client is connected via websocket to a Bot Framework Direct Line bot, and will parse bot responses into TTS.
* The conversation is rendered similar to the web chat experience, new messages will be displayed after the speech synthesizer finishes playing the corresponding activity.
* User can configured common settings in the application to configure the client to work with their own bot.
* If a user is participating in the Neural TTS Preview, they can provide their keys in configuration and toggle it under settings.

## Setup

For basic setup of your DDK and running an Android application, visit the documentation for the [Speech Devices SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-devices-sdk-qsg) with a bot hosted on the [Microsoft Bot Framework Direct Line](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-direct-line?view=azure-bot-service-4.0&tabs=cscreatebot%2Ccsclientapp%2Ccsrunclient). This is built using an implementation of the [Virtual Assistant](https://github.com/Microsoft/AI/tree/master/solutions/Virtual-Assistant
). This will explain what software to install, how to deploy an Android application on your DDK device, and install a custom wake word. You can deploy our sample application in the same method.

The settings screen of the sample application allows user to make quick configuration changes without needing to dive into the code. The default values for these can be set at `/res/values/configuration.xml`.
You will also need to provide connection settings to the Speech Service and can find details to get a key at [Speech Service documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started). 
If these settings are missing, the application will raise an alert and not start.

| Key       | Description  |
| ------------- |:-------------:| 
| Default_directline_secret | The Direct Line secret for your hosted bot. |
| Default_from_user_id | ID attached to the user activities sent to bot. |
| Default_from_bot_id | ID of your hosted bot, this is used to filter activities as they are passed through the web socket.|
| Default_locale | Bot's locale to determine responses *(you can update your device's language settings for the application to show a fully localized environment)* |
| Default_inputhint | The input hint that determines what recognizer mode is enabled upon conversation startup. If the conversation starts with a greeting from the bot, it is recommended to leave this as ignoringInput and let the recognizer handle this as activities are parsed. <br> *Valid values: ignoringInput, acceptingInput, expectingInput* |
| Default_latitude | Mock latitude that can be sent in a location event to the bot. |
| Default_longitude | Mock longitude that can be sent in a location event to the bot. |
| Default_devicegeometry | Device microphone configuration. <br> *Valid values: Circular6+1, Linear4* |
| Default_selectedgeometry | Software microphone configuration. <br> *Valid values: Circular6+1, Circular3+1, Linear4, Linear2* |
| Default_gender | Gender of your bot's TTS voice. <br> *Valid values: male, female* |
| Default_neural | Boolean to determine if Neural TTS is enabled for your demo. <br> *Valid values: true, false* |

### Using the Speech Recognizer

The application will switch between different recognition modes based on the input hints returned by the bot.

| Input Hint | Speech Recognizer | Description | Sample Utterance | 
| ------------- |:-------------:| -----:| -----:|
| Ignoring input      | No recognizer enabled | The bot is not expecting any response from the user and the microphone is closed. This may be used when a bot has multiple activities to send concurrently. | n/a |
| Accepting input      | Keyword recognizer | The bot is passively awaiting a response from the user, it will open the microphone but not react unless the configured keyword has been uttered. | *"Assistant, send an email to Mark"* |
| Expecting input      | Continuous recognizer | The bot is awaiting a response from the user and will leave the microphone open. This is useful for multi-turn dialogs where a user will have a natural conversation with their bot.| *"Send an email to Mark"* |

For more details on how to use input hints, visit [Add input hints to your messages](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-add-input-hints?view=azure-bot-service-4.0&tabs=cs).

### Customizing your wake word
You can use a provided wake word (Assistant, Computer or Machine) or create your own, either way you will need to configure your Roobo to work with that wake word.
- Create your own custom wake word following the instructions at [Create a custom wake word using Speech service](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-devices-sdk-create-kws)
- To install the necessary files on your Roobo device, follow Step 5 of [Get started with the Speech Devices SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-devices-sdk-qsg) to install your custom word.
- Update the Keyword variable in Configuration.java with your wake word.

## Virtual Assistant

### Localization

Under **Settings**, update the locale to connect to a multi-language bot. This sets the locale for the Direct Line conversation & the Text to Speech voice. The client is configured for English, Chinese, French, Italian, Spanish, & German. 
Visit [Language and region support for Speech Service API](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support) for other supported languages.

### Event Architecture
When a new conversation is started, the client sends multiple events to the Virtual Assistant on startup.
1. The `IPA.TimeZone` event with the Android device's time zone.
2. The `IPA.Location` event using mocked coordinates in `configuration.xml`. If you make any changes to the coordinates mid-conversation (under **Settings**), the client will send an updated event when the conversation is resumed.

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## License
Copyright (c) Microsoft Corporation. All rights reserved.
