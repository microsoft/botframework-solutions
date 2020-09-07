---
category: Virtual Assistant
subcategory: Handbook
title: Testing
description: Your Virtual Assistant can be tested just like any other Bot Framework Bot; the most common tools are the [Bot Framework Emulator](https://aka.ms/botframework-emulator) and [Web Chat](https://aka.ms/botframework-webchat).
order: 6
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}


## Unit Testing

Take advantage of the Test projects that are availables in the samples present in the repository:

| Language | Bot | Create your bot |Test folder |
|----------|-----|-----------------|------------|
|C#|Virtual Assistant|[Create a New Virtual Assistant]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/csharp/1-intro)|[Link](https://github.com/microsoft/botframework-solutions/tree/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample.Tests)|
|C#|Skill|[Create a New Skill]({{site.baseurl}}/skills/tutorials/create-skill/csharp/1-intro)|[Link](https://github.com/microsoft/botframework-solutions/tree/master/samples/csharp/skill/SkillSample.Tests)|
|TypeScript|Virtual Assistant|[Create a New Virtual Assistant]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/typescript/1-intro)|[Link](https://github.com/microsoft/botframework-solutions/tree/master/templates/typescript/samples/sample-assistant/test)|
|TypeScript|Skill|[Create a New Skill]({{site.baseurl}}/skills/tutorials/create-skill/typescript/1-intro)|[Link](https://github.com/microsoft/botframework-solutions/tree/master/templates/typescript/samples/sample-skill/test)|

Follow along with the Flow tests to see a basic usage of how to mock activities from a user and validate the bot responses.

## Client Testing

### Bot Framework Emulator
{:.no_toc}

Before testing the bot using Bot Framework Emulator, you must have completed the `Deployment Steps` and should ensure you have the [latest emulator](https://aka.ms/botframework-emulator) installed.

| Language | Bot | Deployment Steps |
|----------|-----|------------------|
|C#|Virtual Assistant|[Deploy your Virtual Assistant]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/csharp/4-provision-your-azure-resources/)
|C#|Skill|[Deploy your Skill]({{site.baseurl}}/skills/tutorials/create-skill/csharp/4-provision-your-azure-resources/)|
|TypeScript|Virtual Assistant|[Deploy your Virtual Assistant]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/typescript/4-provision-you-azure-resources/)|
|TypeScript|Skill|[Deploy your Skill]({{site.baseurl}}/skills/tutorials/create-skill/typescript/4-provision-your-azure-resources/)|

As soon as you have the `appsettings.json` file populated with the deployed resources, you should enter the following parameters in the Bot Framework Emulator:
- Bot name: name of the bot that you are creating
- Endpoint URL: endpoint where your bot will receive the messages that matches with the "Messaging endpoint" of the Web App Bot resoruce after deployment (e.g. `https://bf-skill.azurewebsites.net/api/messages`)
- Microsoft App ID: microsoftAppId's value of the `appsettings.json` file, that represents the identity of the Bot Service.
- Microsoft App password: microsoftAppPassword's value of the appsettings.json file, that represents the password of the identity of the Bot Service.

Execute `Save and connect` and test your bot!

> Authentication scenarios cannot be fully tested within the Emulator at this time. The Web Test Harness provides a workaround for this.

For further documentation, see [Debug with the emulator](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-debug-emulator?view=azure-bot-service-4.0&tabs=csharp).

### Direct Line Configuration
{:.no_toc}

For device integration and use of the test harnesses below you need to publish your assistant to your Azure subscription and then configure the [Direct Line](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-channel-connect-directline?view=azure-bot-service-3.0) channel.

- Start with deploying your assistant to Azure
- Then use the following CLI tool shown below, the key will be shown in the `key` field. This will not be accessible again so ensure you keep this securely and for the steps below.

```shell
az bot directline create -g YOUR_RESOURCE_GROUP_NAME --name YOUR_BOT_NAME
```

### Direct Line Sample
{:.no_toc}

A simple Console App is provided to demonstrate the base communication interaction required with a Virtual Assistant and highlights how a device can interact with a Virtual Assistant. The Sample enables you to conduct a conversation with a Virtual Assistant and demonstrates how responses can be processed including Adaptive Cards along with retrieving the **Speak** property which is the Speech friendly variation of the response.

Examples are also provided on how events can be sent (device activation for example) as well as receiving responses to perform an action locally (e.g. change the navigation system or radio station).

Update the code to reflect the Direct Line secret you created previously.