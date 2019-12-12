---
layout: tutorial
category: Skills
subcategory: Create
language: TypeScript
title: Run your skill
order: 5
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

Once deployment is complete, you can start debugging through the following steps:

- Open the generated skill in your desired IDE (e.g Visual Studio Code)
- Run `npm run start` 
1. Open the **Bot Framework Emulator**.
1. Select **Open Bot**.

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-openbot.png)

1. Provide the messaging endpoint of your running bot (e.g: http://localhost:3978/api/messages).
1. Provide the Microsoft App ID and Microsoft App Password values from your **appsettings.json** file.

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-openbotmodal.png)

1. Congratulations, you've built and run your first skill!

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-greetingemulator.png)