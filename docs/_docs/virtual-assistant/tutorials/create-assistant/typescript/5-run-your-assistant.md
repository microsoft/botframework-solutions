---
layout: tutorial
category: Virtual Assistant
subcategory: Create a Virtual Assistant
language: TypeScript
title: Run your assistant
order: 5
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Run your assistant
When deployment is complete, you can run your Virtual Assistant through the following steps:

1. Open the generated assistant in your desired IDE (e.g Visual Studio Code).
1. Run `npm run start`.
1. Run the **Bot Framework Emulator**.
1. Select **Open Bot**.

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-openbot.png)

1. Populate the fields in the **Open a Bot** modal with your bot's configuration settings. Provide the endpoint of your running bot, e.g: `http://localhost:3978/api/messages`. Provide the AppId and Secret values. Find these in your `appsettings.json` file, under the `microsoftAppId` and `microsoftAppPassword` configuration settings.

> **Note**: If you password has any JSON reserved characters (backslash, quote, etc.), the deployment scripts automatically escape them when populating `appsettings.json`. These must be removed before using tools like the [Bot Framework Emulator](https://aka.ms/botframeworkemulator) to connect to your bot.

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-openbotmodal.png)

1. Congratulations, you've built and run your first Virtual Assistant!

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-greetingemulator.png)
