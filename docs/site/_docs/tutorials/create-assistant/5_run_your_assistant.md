---
category: Tutorials
subcategory: Create a Virtual Assistant
language: csharp
title: Run your assistant
order: 5
---

# Tutorial: Create a Virtual Assistant

## Run your assistant

Currently VA supports both regular channels such as Direct Line, Facebook etc, as well as the Direct Line Speech channel which is currently in preview. To enable the Direct Line Speech channel, please add a separate Nuget feed in your Visual Studio Tools -> Nuget Package Manager -> Package Manager Settings, under 'Package Sources', add a new source:
https://botbuilder.myget.org/F/experimental/api/v3/index.json

With this source added, you will be able to build and run your VirtualAssistantSample project.

When deployment is complete, you can run your Virtual Assistant debugging through the following steps:

1. Press **F5** within Visual Studio to run your assistant.
2. Run the **Bot Framework Emulator**.
3. Select **Open Bot**.

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-openbot.png)

4. Populate the fields in the **Open a Bot** modal with your bot's configuration settings. Provide the endpoint of your running bot, e.g: `http://localhost:3978/api/messages`. Provide the AppId and Secret values. Find these in your `appsettings.json` file, under the `microsoftAppId` and `microsoftAppPassword` configuration settings.

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-openbotmodal.png)

5. Congratulations, you've built and run your first Virtual Assistant!

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-greetingemulator.png)
