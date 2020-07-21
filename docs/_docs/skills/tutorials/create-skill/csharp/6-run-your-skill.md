---
layout: tutorial
category: Skills
subcategory: Create
language: csharp
title: Run your skill
order: 6
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

After deployment, you can run and test your Skill project using these steps:

1. Press **F5** within Visual Studio to run your skill.
1. Open the **Bot Framework Emulator**.
1. Select **Open Bot**.

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-openbot.png)

1. Provide the messaging endpoint of your running bot (e.g: http://localhost:3978/api/messages).
1. Provide the Microsoft App ID and Microsoft App Password values from your **appsettings.json** file.

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-openbotmodal.png)

1. Congratulations, you've built and run your first skill!

    ![]({{site.baseurl}}/assets/images/quickstart-skill-greetingemulator.png)