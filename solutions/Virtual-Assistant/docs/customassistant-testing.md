# Custom Assistant Testing

## Overview

A Custom Assistant can be tested just like any other Bot Framework Bot, the Bot Framework Emulator and WebChat canvases being the most commonly tools. 

## Bot Framework Emulator

The Bot Framework Emulator can be used by opening the .bot file provided within the Project directory. You must have completed the [deployment steps](./customassistant-createcustomassistant.md) first and should ensure you have the [latest](https://github.com/Microsoft/BotFramework-Emulator/releases
) v4 emulator installed.

> Authentication scenarios cannot be fully tested within the Emulator at this time. The Web Test Harness provides a workaround for this.

## Web Test Harness

The Web Test Harness makes use of the Bot Framework WebChat control to provide an additional test canvas. The Web Test harness is configured against an Identity Provider (e.g. Active Directory) to enable the user to signin and retrieve a unique identifer which is used to ensure all messages sent during testing use this identifier enabling testing of the [Linked Accounts](./customassistant-linkedaccounts) feature. When using the Linked Accounts feature ensure that you signin to the same account, then accounts you link will be automatically made available to you when testing through the Web Test harness removing the need for Authentication prompts which aren't practical in voice scenarios.

Your Custom Assistant needs to be deployed to Azure and have the [Direct-Line channel configured](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-channel-connect-directline?view=azure-bot-service-3.0) as a pre-requisite for use of the Test Harness. Update the appSettings.json file file with the Direct-Line Secret.

See the Authentication Configuration section of the [Linked Accounts](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-channel-connect-directline?view=azure-bot-service-3.0) documentation for how to configure the Authentication section.

## Direct-Line Sample

A simple Console App is provided to demonstrate the base communication interaction required with a Custom Assistant and highlights how a device can interact with a Custom Assistant. The Sample enables you to conduct a conversation with a Custom Assistant and demonstrates how responses can be processed including Adaptive Cards along with retrieving the "Speak" property which is the Speech friendly variation of the response.

Examples are also provided on how events can be sent (device activation for example) as well as receiving responses to perform an action locally (e.g. change the navigation system or radio station).

Your Custom Assistant needs to be deployed to Azure and have the [Direct-Line channel configured](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-channel-connect-directline?view=azure-bot-service-3.0) as a pre-requisite for use of the Sample. Update the code to reflect the Direct-Line secret.

## Additional Platforms
> We plan to offer additional test harnesses and integration samples for Linux and Android moving forward.