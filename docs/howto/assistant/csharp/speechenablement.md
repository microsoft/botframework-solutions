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

## Create a Microsoft Speech instance

The first step is to create a Microsoft Speech instance to perform the Speech-To-Text and Text-To-Speech capabilities for your assistant.

- Create a Microsoft Speech Cognitive Service instance in your Azure Subscription using the [Azure Portal](https://ms.portal.azure.com/#create/Microsoft.CognitiveServicesSpeechServices)
- At this time the Direct Line Speech Channel is currently [only available](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-channel-connect-directlinespeech?view=azure-bot-service-4.0#known-issues) in `westus2` so we recommend you use this region to reduce latency.
- Once created, retrieve one of the speech **subscription keys** and store this ready for later in this tutorial. 

## Add the Speech Channel to your Assistant

The first step is to add the Direct-Line Speech Channel to your deployed Assistant.

1. Go to the Azure Portal and locate the Web App Bot created for your Assistant which is most easily found by opening the Resource Group.
2. Click `Channels` on the left-hand navigation and select `Direct Line Speech`
3. Review the Channel introduction page and when ready, click `Save` to add the Channel to your Assistant.
4. Retrieve the **Channel secret** key which will be used by your application to connect to your Bot through the Direct Line Speech Channel and store this ready for later in this tutorial.

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

## Testing Speech Interactions

1. Run the application created in the previous step.
2. Click `Enable Microphone` to ensure the Application has permission to access.
2. Click `Talk to your Bot` and say `Hello`, your should here a spoken Response from your Virtual Assistant.
3. You can now interact with your Assistant (including Skills) through Speech. *Note that follow-up questions asked by the Assistant will require you to click the Button each time as this sample application doesn't automatically open the microphone for questions*.

    ![Simple Speech App](/docs/media/simplespeechapp.png)

## Changing the Voice

Now let's change the default voice (`Jessa24kRUS`) configured within your Virtual Assistant to a higher quality [Neural voice](https://azure.microsoft.com/en-us/blog/microsoft-s-new-neural-text-to-speech-service-helps-machines-speak-like-people/).

1. Open your Assistant Solution in Visual Studio.
2. Open `DefaultWebSocketAdapter.cs` located within your `Adapters` folder.
3. Select the Voice you would like to use from [this list](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support#neural-voices), for example `Microsoft Server Speech Text to Speech Voice (en-US, JessaNeural)`
3. Update the following line to specify the new voice:
    ```
    Use(new SetSpeakMiddleware(settings.DefaultLocale ?? "en-us"));
    ```
    To
    ```
    Use(new SetSpeakMiddleware(settings.DefaultLocale ?? "en-us", "Microsoft Server Speech Text to Speech Voice (en-US, JessaNeural)"));
    ```
4. Build your Assistant and re-publish your Assistant to Azure so the changes are available to the Speech Channel.
5. Repeat the tests and listen to the voice difference.

## Next Steps

This tutorial is based on example applications provided by the Speech SDK which you can refer to for more information along with other programming languages.

- [C# UWP](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/quickstart-virtual-assistant-csharp-uwp)
- [Java (Windows, macOS, Linux)](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/quickstart-virtual-assistant-java-jre)
- [Java (Android)](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/quickstart-virtual-assistant-java-android)

In addition, we provide an example [Android based Virtual Assistant Client](/solutions/android/VirtualAssistantClient/readme.md) which provides an example client application that interfaces with the Virtual Assistant through the Speech Channel and renders Adaptive Cards.



