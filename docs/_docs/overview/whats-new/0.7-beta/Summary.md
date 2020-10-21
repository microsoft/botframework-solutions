---
category: Overview
subcategory: What's New
language: 0_7_release
title: Summary
date: 2019-11-26
order: 1
toc: true
---

# Beta Release 0.7
## {{ page.title }} - {{ page.date | date: "%D" }}

## Intro
This document describes the new features available in the **0.7-beta release** version of the Virtual Assistant across the following categories:
- Virtual Assistant Core
- Skills	
- Assistant Solution Accelerators	
- Clients and Channels

## Virtual Assistant Core
Virtual Assistant Core has many new features that have been added to follow the support of the Bot Framework and other core components. In this section we will cover the new features or supported components that make up the Virtual Assistant Core implementation.

### Bot Framework 4.6 Support
{:.no_toc}

Virtual Assistant is updated to Bot Framework 4.6. Some new capabilities of the SDK are highlighted in this document, but more details of Bot Framework 4.6 can be found [here](https://github.com/microsoft/botframework-sdk).

### Language Generation (LG) Support
{:.no_toc}

Microsoft has added support of the new [Language Generation (LG)](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-concept-language-generation?view=azure-bot-service-4.0&tabs=csharp) features that have been added to the [Bot Framework 4.6](https://github.com/microsoft/botframework-sdk) allowing for richer dialogs that have more dynamic, natural responses. Microsoft has also incorporated LG into the Virtual Assistant and the Skills.

### Context Switching
{:.no_toc}

Context switching enables the user to switch to a different skill by evaluating top-level intents as an interruption.

### Speech Support
{:.no_toc}

Microsoft has added speech support to Virtual Assistant enabling Speech-first experiences without any custom-code. This includes configuration settings for WebSockets and SSML middleware. A tutorial is included in the documentation on how to configure the Direct Line Speech channel.

### Teams Channel Support
{:.no_toc}

Microsoft has worked closely with the Microsoft Teams organization to incorporate the [Microsoft Teams channel]({{site.baseurl}}/clients-and-channels/tutorials/enable-teams/1-intro/) as a supported channel for Virtual Assistant. As Virtual Assistant moves forward you will continue to see enhancements to allow for smoother Teams integration as a part of Virtual Assistant. An example Manifest is provided as part of the Enterprise Assistant to simplify addition of your assistant to Teams.

### Multi-Turn QnA Maker Support
{:.no_toc}

Microsoft has added support to Virtual Assistant to allow for the support of Multi-Turn QnA Maker as a solution for more advanced FAQ experiences. Much of this comes with the support of [Bot Framework 4.6](https://github.com/microsoft/botframework-sdk). More details you can review the [Multi-turn QnA Maker documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/QnAMaker/how-to/multiturn-conversation).


## Skills
Microsoft continues to grow its skills library and enhance skills that are provided. In this section we will cover the new skills and enhancements to skills that have been provided. Experimental skills should be considered samples, they typically have a simple LUIS language model and are not localized.

### Improved Conversation Flows and Capabilities
{:.no_toc}

Microsoft has also improved the conversations in many of the existing skills that were previously announced that allow for better customer experiences with skills such as Calendar, POI, and others.

### Hospitality Skill (Experimental)
{:.no_toc}

Microsoft has released a new experimental skill focused on Hospitality scenarios allowing for experiences such as managing reservations, check out, and amenity requests. You can find more details on scenarios that this supports in the [Hospitality Skill documentation]({{site.baseurl}}/skills/samples/hospitality).

### IT Service Management (ITSM) Skill (Experimental)
{:.no_toc}

Microsoft has released a new experimental skill focused on popular IT Service Management scenarios. The IT Service Management skill provides a basic skill that provides ticket and knowledge base related capabilities and supports [ServiceNow](http://www.servicenow.com/). More details around the ITSM skill can be found [here]({{site.baseurl}}/skills/samples/itsm/).

### Music Skill (Experimental)
{:.no_toc}

Microsoft has released a new experimental skill focused on demonstrating artists and playlist lookup for the popular music service [Spotify](https://developer.spotify.com/documentation/web-api/libraries/). Playback information is then signalled back to the device through Events enabling native device playback. More details around the Music skill can be found [here]({{site.baseurl}}/skills/samples/music/).


## Assistant Solution Accelerators
Microsoft has introduced the concept of Assistant Samples during this time. As we continue to grow our Virtual Assistant capabilities, we are looking to provide our customers with samples of implementations that bring together the skills and channels that many of our customers are looking to build. In this section we will provide details to some of the Assistant Samples that we are introducing.

### Base Virtual Assistant
{:.no_toc}

Microsoft has not necessarily released a new capability as much as the concept of an Empty Core assistant that allows for customers to build from a completely empty solution that does not incorporate any pre-installed skills. This has always been the basis of the Virtual Assistant, but we want to ensure that this is the base that customers can start with if they want to assemble a solution that one of the other examples does not fit easily to their solution. You can get more details around this sample [here]({{site.baseurl}}/overview/virtual-assistant-template).

### Enterprise Assistant Sample
{:.no_toc}

Microsoft has assembled a typical configuration of a Virtual Assistant that is common from what we have seen working with our customer base implementation of a Virtual Assistant that is targeted at the Enterprise community. You can get more details around this sample [here]({{site.baseurl}}/solution-accelerators/assistants/enterprise-assistant/).

### Hospitality Assistant Sample
{:.no_toc}

Microsoft has assembled a typical configuration of a Virtual Assistant that is common from what we have seen working with our customer base implementation of a Virtual Assistant that is targeted at the Hospitality community. You can get more details around this sample [here]({{site.baseurl}}/solution-accelerators/assistants/hospitality-assistant/).

### Automotive Assistant Sample
{:.no_toc}

Microsoft has assembled a typical configuration of a Virtual Assistant that is common from what we have seen working with our customer base implementation of a Virtual Assistant that is targeted at the Automotive community. You can get more details around this sample are coming soon.

## Clients and Channels
Microsoft continues to work to bring more ways to allow users to connect to their Virtual Assistant through a conversational canvas of their choice. This allows developers to write their conversational experiences once and then allow them to be consumed through the key channels and clients that their users demand. In this section we will provide details to what has be added in this area.

### Android Virtual Assistant Client
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

### Teams Channel Support
{:.no_toc}

Microsoft has added Teams Channel support to our [Bot Framework 4.6](https://github.com/microsoft/botframework-sdk) channel and is now supported for Out of the Box use with Virtual Assistant.


## Summary
Microsoft is committed to bringing our customers the ability to bring their own unique Virtual Assistant experiences to their users by providing the tools and control that is required in a world of many Virtual Agents. We look forward to how you take these enhancements forward to enable your customers in the future. As always you can provide feature requests and/or bug reports at [https://github.com/microsoft/botframework-solutions/issues](https://github.com/microsoft/botframework-solutions/issues).


## Release notes
For more information on the changes in this release and version compatibility, please refer to the [v0.7-beta release notes](https://github.com/microsoft/botframework-solutions/releases/tag/v0.7-beta). 