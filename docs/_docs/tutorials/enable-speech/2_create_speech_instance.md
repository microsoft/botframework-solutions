---
category: Tutorials
subcategory: Enable Speech
title: Create a Microsoft Speech instance
order: 2
---

# Tutorial: Enable Speech for your Assistant

## Create a Microsoft Speech nstance

The first step is to create a Microsoft Speech instance to perform the Speech-To-Text and Text-To-Speech capabilities for your assistant.

- Select an Azure region. Direct Line Speech Channel is a preview service limited to [these Azure regions](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/regions#voice-first-virtual-assistants). For best performance (reduced round-trip time) deploy your Virtual Assistant bot and Direct Line Speech channel to the same Azure region, and one that is closest to you. To help you decide, look up exact [geographical location](https://azure.microsoft.com/en-us/global-infrastructure/locations/) for each Azure region.
- Create a Microsoft Speech Cognitive Service instance in your Azure Subscription using the [Azure Portal](https://ms.portal.azure.com/#create/Microsoft.CognitiveServicesSpeechServices). In the *Location* field specify the selected Azure region based on the above.
- Once created, retrieve one of the speech **subscription keys** and store this ready for later in this tutorial. 