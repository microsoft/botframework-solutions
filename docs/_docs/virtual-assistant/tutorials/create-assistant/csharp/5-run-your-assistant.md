---
layout: tutorial
category: Virtual Assistant
subcategory: Create
language: C#
title: Run your assistant
order: 5
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Run your assistant

After deployment, you and run and test your Virtual Assistant project using these steps:

1. Press **F5** within Visual Studio to run your assistant.
1. Open the **Bot Framework Emulator**.
1. Select **Open Bot**.

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-openbot.png)

1. Provide the messaging endpoint of your running bot (e.g: http://localhost:3978/api/messages).
1. Provide the Microsoft App ID and Microsoft App Password values from your **appsettings.json** file.

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-openbotmodal.png)

1. Congratulations, you've built and run your first Virtual Assistant!

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-greetingemulator.png)