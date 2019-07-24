---
category: Tutorials
subcategory: Create a Virtual Assistant
language: csharp
title: Run your assistant
order: 5
---

## Run your assistant

Currently VA supports both regular channels such as Direct Line, Facebook etc, as well as the Direct Line Speech channel which is currently in preview. To enable the Direct Line Speech channel, please add a separate Nuget feed in your Visual Studio Tools -> Nuget Package Manager -> Package Manager Settings, under 'Package Sources', add a new source:
https://botbuilder.myget.org/F/experimental/api/v3/index.json

With this source added, you will be able to build and run your VirtualAssistantSample project.

When deployment is complete, you can run your Virtual Assistant debugging through the following steps:

1. Press **F5** within Visual Studio to run your assistant.
2. Run the **Bot Framework Emulator**.
3. Select **Open Bot**.

  <!-- <p align="center">
  <img src="../../media/quickstart-virtualassistant-openbot.png" width="600">
  </p> -->

4. Populate the fields in the **Open a Bot** modal with your bot's configuration settings. Provide the endpoint of your running bot, e.g: `http://localhost:3978/api/messages`. Provide the AppId and Secret values. Find these in your `appsettings.json` file, under the `microsoftAppId` and `microsoftAppPassword` configuration settings.

  <!-- <p align="center">
  <img src="../../media/quickstart-virtualassistant-openbotmodal.png" width="600">
  </p> -->

5. Congratulations, you've built and run your first Virtual Assistant!

<!-- <p align="center">
<img src="../../media/quickstart-virtualassistant-greetingemulator.png" width="600">
</p> -->