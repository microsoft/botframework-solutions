---
category: Tutorials
subcategory: Enable Speech
language: csharp javascript
title: Intro
order: 1
---

# Speech enabling your Assistant

**APPLIES TO:** ✅ SDK v4

## In this tutorial
- [Intro](#intro)
- [Create a Microsoft Speech instance](#Create-a-Microsoft-Speech-instance)
- [Add the Speech Channel to your Assistant](#Add-the-Speech-Channel-to-your-Assistantl)
- [Integrating with the Speech Channel](#Integrating-with-the-Speech-Channel)
- [Testing Speech Interactions](#Testing-Speech-Interactions)
- [Next Steps](#Next-Steps)

## Intro

### Purpose

The Virtual Assistant template creates and deploys an Assistant with all speech enablement steps provided out of the box.

This tutorial covers the steps required to connect the [Direct Line Speech channel](https://docs.microsoft.com/en-us/azure/bot-service/directline-speech-bot?view=azure-bot-service-4.0) to your assistant and build a simple application integrated with the Speech SDK to demonstrate Speech interactions working.

### Prerequisites

- [Create a Virtual Assistant](/docs/tutorials/csharp/virtualassistant.md) to setup your environment.

- Make sure the `Universal Windows Platform development` workload is available on your machine. Choose **Tools > Get Tools** and Features from the Visual Studio menu bar to open the Visual Studio installer. If this workload is already enabled, close the dialog box.

    ![UWP Enablement](/docs/media/vs-enable-uwp-workload.png)

    Otherwise, select the box next to .NET cross-platform development, and select Modify at the lower right corner of the dialog box. Installation of the new feature takes a moment.

### Time to Complete

10 minutes

### Scenario

Create a simple application that enables you to speak to your newly created Virtual Assistant.



