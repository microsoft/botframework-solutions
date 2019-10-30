---
layout: tutorial
category: Clients and Channels
subcategory: Enable speech 
title: Build speech sample app
order: 4
---

# Tutorial: {{page.subcategory}}

## Integrating with the Speech Channel

For this tutorial we'll use a Direct-Line Speech Client testing tool to demonstrate speech working with your assistant.

1. Browse to the [Direct Line Speech Client repo](https://github.com/Azure-Samples/Cognitive-Services-Direct-Line-Speech-Client). Click [Releases](https://github.com/Azure-Samples/Cognitive-Services-Direct-Line-Speech-Client/releases) and download the [latest](https://github.com/Azure-Samples/Cognitive-Services-Direct-Line-Speech-Client/releases/latest) ZIP file.
1. Expand the ZIP file onto your local drive and open in Visual Studio / VSCode.
1. Build and run the `DLSpeechClient` app. The App will show the Settings page on first load.
![Direct Line Speech Client Configuration]({{site.baseurl}}/assets/images/dlspeechclientsettings.png)

1. Set the Subscription Key to be the `Speech Subscription Key` that you retrieved in the previous step and set the correct region.
1. It's advised to provide an example User ID to ensure multi-user scenarios work, this can be a GUID or some other user identifier of your choice.
1. Click OK to save the Settings.
1. Paste the `Channel Secret` retrieved in the previous steps into the `Bot Secret` text box.
1. Click the Microphone button in the bottom right hand corner and say something, you should now see and hear a response.

![Direct Line Speech Client Configuration]({{site.baseurl}}/assets/images/dlspeechclient.png)