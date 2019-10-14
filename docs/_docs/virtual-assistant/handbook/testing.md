---
category: Virtual Assistant
subcategory: Handbook
title: Testing
description: Testing a Virtual Assistnat
order: 6
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

## Introduction
Your Virtual Assistant can be tested just like any other Bot Framework Bot; the most common tools are the [Bot Framework Emulator](https://aka.ms/botframework-emulator) and [Web Chat](https://aka.ms/botframework-webchat).

## Unit Testing

Take advantage of the Test project that is available when you [Create a New Skill]({{site.baseurl}}/tutorials/csharp/create-skill/1_intro).
Follow along with the Flow tests to see a basic usage of how to mock activities from a user and validate the bot responses.
If you'd like to take this further, you can explore the tests of a published skill for a deep dive on APIs, mocking LUIS, and more.

## Client Testing

### Bot Framework Emulator

The Bot Framework Emulator can be used by opening the .bot file provided within the Project directory. You must have completed the [deployment steps]({{site.baseurl}}/tutorials/csharp/create-assistant/4_provision_your_azure_resources) first and should ensure you have the [latest emulator](https://aka.ms/botframework-emulator) installed.

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