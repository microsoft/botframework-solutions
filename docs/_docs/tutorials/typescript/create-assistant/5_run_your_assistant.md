---
category: Tutorials
subcategory: Create a Virtual Assistant
language: TypeScript
title: Run your assistant
order: 5
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Run your assistant
When deployment is complete, you can run your Virtual Assistant through the following steps:

1. Open the generated assistant in your desired IDE (e.g Visual Studio Code).
2. Run `npm run start`.
2. Run the **Bot Framework Emulator**.
3. Select **Open Bot**.

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-openbot.png)

4. Populate the fields in the **Open a Bot** modal with your bot's configuration settings. Provide the endpoint of your running bot, e.g: `http://localhost:3978/api/messages`. Provide the AppId and Secret values. Find these in your `appsettings.json` file, under the `microsoftAppId` and `microsoftAppPassword` configuration settings.

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-openbotmodal.png)

5. Congratulations, you've built and run your first Virtual Assistant!

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-greetingemulator.png)
