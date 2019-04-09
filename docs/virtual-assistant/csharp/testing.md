# Testing the Virtual Assistant

Your Virtual Assistant can be tested just like any other Bot Framework Bot; the most common tools are the [Bot Framework Emulator](https://aka.ms/botframework-emulator) and [Web Chat](https://aka.ms/botframework-webchat). 

## Table of Contents
- [Unit Testing](#unit-testing)
- [Client Testing](#client-testing)

## Unit Testing

Take advantage of the Test project that is available when you [Create a New Skill](../../skills/csharp/create.md). 
Follow along with the Flow tests to see a basic usage of how to mock activities from a user and validate the bot responses. 
If you'd like to take this further, you can explore the tests of a published skill for a deep dive on APIs, mocking LUIS, and more.

## Client Testing


### Bot Framework Emulator

The Bot Framework Emulator can be used by opening the .bot file provided within the Project directory. You must have completed the [deployment steps](./gettingstarted.md) first and should ensure you have the [latest emulator](https://aka.ms/botframework-emulator) installed.

> Authentication scenarios cannot be fully tested within the Emulator at this time. The Web Test Harness provides a workaround for this.

### Direct Line Configuration

For device integration and use of the test harnesses below you need to publish your assistant to your Azure subscription and then configure the [Direct Line](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-channel-connect-directline?view=azure-bot-service-3.0) channel.

- Start with deploying your assistant to Azure
- Then use the following CLI tool shown below, the key will be shown in the `key` field. This will not be accessible again so ensure you keep this securely and for the steps below.

```shell
az bot directline create -g YOUR_RESOURCE_GROUP_NAME --name YOUR_BOT_NAME
```

### Direct Line Sample

A simple Console App is provided to demonstrate the base communication interaction required with a Virtual Assistant and highlights how a device can interact with a Virtual Assistant. The Sample enables you to conduct a conversation with a Virtual Assistant and demonstrates how responses can be processed including Adaptive Cards along with retrieving the `Speak` property which is the Speech friendly variation of the response.

Examples are also provided on how events can be sent (device activation for example) as well as receiving responses to perform an action locally (e.g. change the navigation system or radio station).

Update the code to reflect the Direct Line secret you created previously.

### Web Chat Test Harness

The Web Chat test harness makes use of the [Bot Framework Web Chat](https://github.com/Microsoft/BotFramework-WebChat) to provide an additional test canvas. 
The Web Chat test harness is configured against an Identity Provider (e.g. Azure Active Directory) to enable the user to sign in and retrieve a unique identifier. 
This will ensure all messages sent during testing use this identifier, enabling testing of the [Linked Accounts](./linkedaccounts.md) feature.
You must use sign in to your Linked Accounts app with the same identity. 
The account you link will be automatically made available to you when testing through the Web Chat test harness, removing the need for authentication prompts.

See [Authentication Configuration](./linkedaccounts.md#authentication-configuration) for how to configure authentication in the application. 
Update the `AzureAd` section in `appsettings.development.config` with the above authentication information along with the Direct Line secret created previously.

When opening the Assistant-WebTest project for the first time you will be assigned a unique port number for local debugging - you can check this by right clicking the Assistant-WebTest project in Visual Studio, choosing **Properties** and reviewing the App URL in the **Debug** section. 
Ensure this is entered into the Reply URLs section of your Authentication configuration (e.g. `https://localhost:44320/signin-oidc`).

### Additional Platforms

We plan to offer additional test harnesses and integration samples for Linux and Android moving forward.
