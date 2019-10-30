---
layout: tutorial
category: Clients and Channels
subcategory: Enable speech 
title: Change the voice
order: 5
---

# Tutorial: {{page.subcategory}}

## Changing the Voice

Now let's change the default voice (`Jessa24kRUS`) configured within your Virtual Assistant to a higher quality [Neural voice](https://azure.microsoft.com/en-us/blog/microsoft-s-new-neural-text-to-speech-service-helps-machines-speak-like-people/). Note that Neural voices will only work with speech subscription keys created for certain locations (regions). See the last column in the [Standard and neural voices](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/regions#standard-and-neural-voices) table for region availability. If your bot is configured for Neural voice and your speech subscription key is for a region not enabled for Neural voices, Direct Line Speech channel will terminate the connection with the client with an Internal Server Error (code 500). 

To switch to Neural voice: 

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