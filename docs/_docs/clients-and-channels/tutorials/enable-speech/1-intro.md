---
layout: tutorial
category: Clients and Channels
subcategory: Extend to Direct Line Speech
title: Intro
order: 1
---

# Tutorial: {{page.subcategory}}

## {{ page.title }}

### Purpose

The Virtual Assistant template creates and deploys a bot enabled to work in speech-voice scenarios.

This tutorial covers the steps required to connect the [Direct Line Speech](https://docs.microsoft.com/en-us/azure/bot-service/directline-speech-bot?view=azure-bot-service-4.0) channel to your assistant and build a simple application integrated with the Speech SDK to demonstrate Speech interactions working.

### Prerequisites

- [Create a Virtual Assistant]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/csharp/1-intro) to setup your environment.

- Make sure the **Universal Windows Platform development** workload is available on your machine. Choose **Tools > Get Tools** and Features from the Visual Studio menu bar to open the Visual Studio installer. If this workload is already enabled, close the dialog box.

    ![UWP Enablement]({{site.baseurl}}/assets/images/vs-enable-uwp-workload.png)

    Otherwise, select the box next to .NET cross-platform development, and select Modify at the lower right corner of the dialog box. Installation of the new feature takes a moment.

### Time to Complete

10 minutes

### Scenario

Run an application that enables you to speak to your Virtual Assistant on the Direct Line Speech channel.



