---
layout: tutorial
category: Skills
subcategory: Create
language: csharp
title: Invoke your skill
order: 8
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

In order to confirm that the skill connection with the Virtual Assistant was correctly made, you can test the communication using the **Bot Framework Emulator**, sending an utterance from the Virtual Assistant that should be recognized by the Skill (e.g., "Run sample dialog"). You will see how the Skill starts the workflow, and when the skill is finished it will send the control back to the Virtual Assistant.

1. Press **F5** within Visual Studio to run your Virtual Assistant.
1. Open the **Bot Framework Emulator**.
1. Select **Open Bot**.

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-openbot.png)

1. Provide the messaging endpoint of your running Virtual Assistant (e.g: http://localhost:3978/api/messages), the Microsoft App ID and Microsoft App Password values from its `appsettings.json` file.

    ![]({{site.baseurl}}/assets/images/quickstart-virtualassistant-openbotmodal.png)

1. The greeting card of your **Virtual Assistant** should appear.
1. Write your name, and invoke your skill writing *"Run sample dialog"*.

    ![]({{site.baseurl}}/assets/images/virtualAssistant-Skill-communication.png)

1. Congratulations, you’ve invoked your skill through your Virtual Assistant!
