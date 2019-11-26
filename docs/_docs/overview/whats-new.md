---
category: Overview
title: What's new?
order: 1
toc: true
---

# {{ page.title }}
{:.no_toc}

![Virtual Assistant diagram]({{site.baseurl}}/assets/images/virtualassistant-diagram.jpg)

Customers and partners have increasing need to deliver advanced conversational assistant experiences tailored to their brand, personalized to their users, and made available across a broad range of canvases and devices. Continuing Microsoft's open-sourced approach towards the Bot Framework SDK, the open-source Virtual Assistant solution provides you with a set of core foundational capabilities and full control over the end user experience and data.

At it's core is the [Virtual Assistant]({{site.baseurl}}/overview/virtual-assistant-template) (available in C# and TypeScript) is a project template with the best practices to developing a bot on the Microsoft Azure platform.

For more details on Virtual Assistant review [What is the Virtual Assistant Solution?]({{site.baseurl}}/overview/virtual-assistant-solution).

## Virtual Assistant components

There are four major components within the Virtual Assistant: 

- Virtual Assistant Core
- Skills
- Assistant Solution Accelerators
- Clients and Channels

The following content will cover the new items of each of these components.

## What's new since Build 2019?
In this section, we will cover the new features to each of the Components of Virtual Assistant since the Build 2019 Conference. These new features are targeted for Ignite 2019 timeframe.

### Virtual Assistant Core

Virtual Assistant Core has many new features that have been added to follow the support of the Bot Framework and other core components. In this section we will cover the new features or supported components that make up the Virtual Assistant Core implementation.

#### Bot Framework 4.6 Support
{:.no_toc}

Virtual Assistant is updated to Bot Framework 4.6.  Some new capabilities of the SDK are highlighted in this document, but more details of Bot Framework 4.6 can be found [here](https://github.com/microsoft/botframework#Bot-Framework-SDK-v4).

#### Language Generation (LG) Support
{:.no_toc}

Microsoft has added support of the new [Language Generation (LG)](https://github.com/Microsoft/BotBuilder-Samples/tree/master/experimental/language-generation) features that have been added to the [Bot Framework 4.6](https://github.com/microsoft/botframework#Bot-Framework-SDK-v4) allowing for richer dialogs that have more dynamic, natural responses.  Microsoft has also incorporated LG into the Virtual Assistant Skills.

#### Context Switching
{:.no_toc}

Context switching enables a Developer to allow the user to switch to a different dialog/skill through by allowing top-level intents to be evaluated within each of the waterfall dialogs.

#### Speech Support
{:.no_toc}

Microsoft has added speech support to Virtual Assistant enabling Speech-first experiences without any custom-code. This includes configuration settings for WebSockets and SSML middleware. A tutorial is included in the documentation on how to configure the Direct Line Speech channel.

#### Teams Channel Support
{:.no_toc}

Microsoft has worked closely with the Microsoft Teams organization to incorporate the Microsoft Teams channel as a supported channel for Virtual Assistant. As Virtual Assistant moves forward you will continue to see enhancements to allow for smoother Teams integration as a part of Virtual Assistant. An example Manifest is provided as part of the Enterprise Assistant to simplify addition of your assistant to Teams.

#### Multi-Turn QnA Maker Support
{:.no_toc}

Microsoft has added support to Virtual Assistant to allow for the support of Multi-Turn QnA Maker as a solution for more advanced FAQ experiences. Much of this comes with the support of [Bot Framework 4.6](https://github.com/microsoft/botframework#Bot-Framework-SDK-v4). More details you can review the [Multi-turn QnA Maker documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/QnAMaker/how-to/multiturn-conversation).


### Skills

Microsoft continues to grow its skills library and enhance skills that are provided. In this section we will cover the new skills and enhancements to skills that have been provided. Experimental skills should be considered samples, they typically have a simple LUIS language model and are not localized.

#### Improved Conversation Flows and Capabilities
{:.no_toc}

Microsoft has also improved the conversations in many of the existing skills that were previously announced that allow for better customer experiences with skills such as Calendar, POI, and others.

#### Hospitality Skill (Experimental)
{:.no_toc}

Microsoft has released a new experimental skill focused on Hospitality scenarios allowing for experiences such as managing reservations, check out, and amenity requests. You can find more details on scenarios that this supports in the [Hospitality Skill documentation]({{site.baseurl}}/skills/samples/hospitality).

#### IT Service Management (ITSM) Skill (Experimental)
{:.no_toc}

Microsoft has released a new experimental skill focused on popular IT Service Management scenarios. The IT Service Management skill provides a basic skill that provides ticket and knowledge base related capabilities and supports [ServiceNow](http://www.servicenow.com/). More details around the ITSM skill can be found [here]({{site.baseurl}}/skills/samples/itsm/).

#### Music Skill (Experimental)
{:.no_toc}

Microsoft has released a new experimental skill focused on demonstrating artists and playlist lookup for the popular music service [Spotify](https://developer.spotify.com/documentation/web-api/libraries/). Playback information is then signalled back to the device through Events enabling native device playback. More details around the Music skill can be found [here]({{site.baseurl}}/skills/samples/music/).


### Assistant Solution Accelerators

Microsoft has introduced the concept of Assistant Samples during this time. As we continue to grow our Virtual Assistant capabilities, we are looking to provide our customers with samples of implementations that bring together the skills and channels that many of our customers are looking to build. In this section we will provide details to some of the Assistant Samples that we are introducing.

#### Base Virtual Assistant
{:.no_toc}

Microsoft has not necessarily released a new capability as much as the concept of an Empty Core assistant that allows for customers to build from a completely empty solution that does not incorporate any pre-installed skills. This has always been the basis of the Virtual Assistant, but we want to ensure that this is the base that customers can start with if they want to assemble a solution that one of the other examples does not fit easily to their solution.

#### Enterprise Assistant Sample
{:.no_toc}

Microsoft has assembled a typical configuration of a Virtual Assistant that is common from what we have seen working with our customer base implementation of a Virtual Assistant that is targeted at the Enterprise community. You can get more details around this sample [here]({{site.baseurl}}/solution-accelerators/assistants/enterprise-assistant/).

#### Hospitality Assistant Sample
{:.no_toc}

Microsoft has assembled a typical configuration of a Virtual Assistant that is common from what we have seen working with our customer base implementation of a Virtual Assistant that is targeted at the Hospitality community. You can get more details around this sample [here]({{site.baseurl}}/solution-accelerators/assistants/hospitality-assistant/).

#### Automotive Assistant Sample
{:.no_toc}

Microsoft has assembled a typical configuration of a Virtual Assistant that is common from what we have seen working with our customer base implementation of a Virtual Assistant that is targeted at the Automotive community. You can get more details around this sample are coming soon.

### Clients and Channels

Microsoft continues to work to bring more ways to allow users to connect to their Virtual Assistant through a conversational canvas of their choice. This allows developers to write their conversational experiences once and then allow them to be consumed through the key channels and clients that their users demand. In this section we will provide details to what has be added in this area.

#### Android Virtual Assistant Client
{:.no_toc}

Microsoft understands the need to be able to have devices such as phones, tablets, and other general IOT devices (Cars, Alarm Clocks, etc.) as interfaces to interact with their users. Microsoft has created a base Android application for users that demonstrates the following capabilities:
- Render Adaptive Cards
- Provide OOB support to Direct Line Speech
- Run as Service on an Android Device
- Open and Close the Mic on the Device
- Consume Events and Engage with the local operating system for Android OS Events (Navigation, Phone Dialer, etc.)
- Run as Default Assistant
- Provide Threaded Conversation Views
- Provide Widgets that will allow for customized Launchers to leverage
- Configuration options to allow user to set bot endpoints
- Configuration options to allow for customization of colors
- Light and Dark Mode support

This sample application can be used to quickly test your Virtual Assistant or any BF Bot on Android Devices (8.x and greater). More details can be found [here]({{site.baseurl}}/clients-and-channels/clients/virtual-assistant-client/).

#### Teams Channel Support
{:.no_toc}

Microsoft has added Teams Channel support to our [Bot Framework 4.6](https://github.com/microsoft/botframework#Bot-Framework-SDK-v4) channel and is now supported for Out of the Box use with Virtual Assistant.


## Summary

Microsoft is committed to bringing our customers the ability to bring their own unique Virtual Assistant experiences to their users by bringing the tools and control that is required in a world of many Virtual Agents. In just the time since Build 2019 Microsoft continues to make great strides in improving these tools and capabilities for their customers. We look forward to how you take these enhancements forward to enable your customers / users in the future. As always you can provide feedback and/or bug reports at [https://github.com/microsoft/botframework-solutions/issues](https://github.com/microsoft/botframework-solutions/issues).