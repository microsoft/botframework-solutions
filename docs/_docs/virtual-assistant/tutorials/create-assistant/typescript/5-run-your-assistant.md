---
layout: tutorial
category: Virtual Assistant
subcategory: Create
language: typescript
title: Run your assistant
order: 5
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Run your assistant

After deployment, you can run and test your Virtual Assistant project using these steps:

1. Open the generated assistant in your desired IDE (e.g Visual Studio Code).
1. Run `npm run start`.
1. Open the **Bot Framework Emulator**.
1. Select **Open Bot**.

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-openbot.png)

1. Provide the messaging endpoint of your running bot (e.g: http://localhost:3979/api/messages).
1. Provide the Microsoft App ID and Microsoft App Password values from your **appsettings.json** file.

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-openbotmodal.png)

1. Congratulations, you've built and run your first Virtual Assistant!

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-greetingemulator.png)