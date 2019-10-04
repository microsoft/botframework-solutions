---
layout: tutorial
category: Virtual Assistant
subcategory: Enable speech 
title: Build speech sample app
order: 4
---

# Tutorial: {{page.subcategory}}

## Integrating with the Speech Channel

For this tutorial we'll take a pre-built C# sample to get you up and running quickly.

1. Locate the [assistant-SimpleSpeechApp](https://github.com/microsoft/botframework-solutions/tree/master/solutions/testharnesses/csharp/assistant-SimpleSpeechApp) example application found in the [botframework-solutions github repo](https://github.com/microsoft/botframework-solutions/) and open in Visual Studio / VSCode.
2. Open `MainPage.xaml.cs` which you can find in your Solution by expanding `MainPage.xaml` in Solution Explorer.
3. At the top of the file you will find the following configuration properties. Update these, using the `Channel Secret` and `Speech Subscription key` that you retrieved in the previous steps. The region provided is for Direct Line Speech which should be left as `westus2` at this time.

    ```
    private const string channelSecret = "YourChannelSecret";
    private const string speechSubscriptionKey = "YourSpeechSubscriptionKey";
    ```
4. Build your application.