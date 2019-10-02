---
category: Skills
subcategory: Create a Skill
language: C#
title: Run your skill
order: 5
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Test your Skill

Once deployment is complete, you can start debugging through the following steps:

- Open the [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator).
- Within the Emulator, click **File > New Bot Configuration**.
- Provide the endpoint of your running Bot, e.g: http://localhost:3978/api/messages
- Provide the AppId and Secret values which you can find in your `appsettings.json` file under the `microsoftAppId` and `microsoftAppPassword` configuration settings.
- Click on **Save and Connect**.