![Bot Framework Solutions](/docs/media/bot_framework_solutions_header.png)

# Table of Contents

- [Table of Contents](#table-of-contents)
- [Overview](#overview)
- [Tutorials](#tutorials)
- [How-To](#how-to)
  - [Virtual Assistant](#virtual-assistant)
  - [Skills](#skills)
- [Reference](#reference)
  - [Virtual Assistant](#virtual-assistant-1)
  - [Skills](#skills-1)
  - [Analytics](#analytics)
- [Need Help?](#need-help)

# Overview

> High-level overview into the Virtual Assistant, Skills and Analytics.

| Name | Description |  
|:------------:|------------|
| [Virtual Assistant](https://github.com/Microsoft/AI/blob/master/docs/overview/virtualassistant.md) | Customers and partners have a significant need to deliver a conversational assistant tailored to their brand, personalized to their users, and made available across a broad range of canvases and devices. This brings together all of the supporting components and greatly simplifies the creation of a new bot project including: basic conversational intents, Dispatch integration, QnA Maker, Application Insights and an automated deployment.|
| [Skills](https://github.com/Microsoft/AI/blob/master/docs/overview/skills.md) | A library of re-usable conversational skill building-blocks enabling you to add functionality to a Bot. We currently provide Calendar, Email, Task, Point of Interest, Automotive, Weather and News skills. Skills include LUIS models, Dialogs, and integration code delivered in source code form to customize and extend as required.|
| [Analytics](https://github.com/Microsoft/AI/blob/master/docs/overview/analytics.md) | Gain key insights into your bot’s health and behavior with the Bot Framework Analytics solutions, which includes sample Application Insights queries and Power BI dashboards to understand the full breadth of your bot’s conversations with users.|

# Tutorials

> Create, deploy, and customize your own Virtual Assistant and Skills using these walkthroughs.

|Name|Description|
|------------|-----------|
|1. Quickstart: Create your first Virtual Assistant<br/><br/> <p align="center">[![Quickstart: Create a Virtual Assistant with C#](./media/csharp_icon.png)](/docs/tutorials/csharp/virtualassistant.md)[![Quickstart: Create a Virtual Assistant with TypeScript](./media/typescript_icon.png)](/docs/tutorials/typescript/virtualassistant.md)</p>|A tutorial covering how to create and deploy your own assistant using the Virtual Assistant template.|
|2. Customize your Virtual Assistant <br/><br/><p align="center">[![Customize your Virtual Assistant with C#](./media/csharp_icon.png)](/docs/tutorials/csharp/customizeassistant.md) [![Customize your Assistant with TypeScript](./media/typescript_icon.png)](/docs/tutorials/typescript/customizeassistant.md)</p>|A tutorial covering common initial assistant personalization steps. (e.g. name, branding, QnA),|
|3. Adding pre-existing Skills <br/><br/><p align="center">[![C#](./media/csharp_icon.png)](/docs/tutorials/csharp/addingprebuiltskills.md) [![TypeScript](./media/typescript_icon.png)](/docs/tutorials/csharp/addingprebuiltskills.md)</p>|A tutorial introducing how to add one of the provided Skills to your Assistant.|
|4. Create a new Skill <br/><br/><p align="center">[![Create and deploy a new Skill with C#](./media/csharp_icon.png)](/docs/tutorials/csharp/skill.md) [![Create and deploy a new Skill with TypeScript](./media/typescript_icon.png)](/docs/tutorials/typescript/skill.md)</p>|A tutorial introducing how you can create your own Skill using the Skill template and add it to your Assistant.|

# How-To

> How-to guides for achieving more complex scenarios.

## Virtual Assistant

| Name | Description |
| ---- | ----------- |
| [Adding Speech capabilities to your Virtual Assistant](/docs/howto/assistant/csharp/speechenablement.md) | A tutorial on how to enable Speech for your Virtual Assistant and creation of a simple application to demonstrate Speech capabilities of your Assistant. |
| [Adding Your Virtual Assistant to Microsoft Teams](/docs/howto/assistant/vaaddtoteams.md) | A tutorial on how to add your Virtual Assistant to Microsoft Teams. |
| [Parent-Child pattern of the Virtual Assistant and Skills](/docs/howto/assistant/parentchildpattern.md) | How the Virtual Assistant template and Skills can enable a Parent-Child pattern letting you democratize creation of assistant capabilities throughout your organization. |
| [How-To: Enable Linked Accounts](/docs/howto/assistant/linkedaccounts.md) | Enable users to link third party accounts (e.g. Office 365) to your Assistant. |
| How-To: Migrate from the Enterprise Template <br/><br/><p align="center">[![Migrating from the Enterprise Template with C#](./media/csharp_icon.png)](/docs/howto/assistant/csharp/ettovamigration.md)</p> | Guidance on how to move from an Enterprise Template based Bot to the new Template |
| How-to: Migrate the Virtual Assistant (Beta Release 0.3) solution to the Virtual Assistant Template <br/><br/> <p align="center">[![Migrating from the beta Virtual Assistant solution with C#](./media/csharp_icon.png)](/docs/howto/assistant/csharp/oldvatovamigration.md)</p> |Guidance on how to move from the original Virtual Assistant solution to the new Template |
| [How-To: Send activities from your assistant to users proactively](/docs/howto/assistant/csharp/proactivemessaging.md) | Adding proactive experiences to your Assistant |
| [How-To: Customize Azure Resource Deployment](/docs/howto/assistant/customizedeployment.md) | How to customise the provided ARM template for different deployment scenarios. |
| [How-To: Update your deployment scripts to the latest version.](/docs/howto/assistant/updatedeploymentscripts.md) | How to update the deployment scripts for Virtal Assistant and Skill projects that you have already created to ensure you have the latest updates. |


## Skills

| Name | Description |
| ---- | ----------- |
| [How-To: Add a new Skill to your Assistant](/docs/howto/skills/botskills.md#Connect-Skills) | Adding a Skill |
| How-To: Add Skills to an existing SDK v4 bot<br/><br/><p align="center">[![Adding Skill support to a v4 SDK Bot with C#](./media/csharp_icon.png)](/docs/howto/skills/csharp/addskillsupportforv4bot.md) [![Adding Skill support to a v4 SDK Bot with TypeScript](./media/typescript_icon.png)](/docs/howto/skills/typescript/addskillsupportforv4bot.md)</p>|How to add Skills to an existing bot (not Virtual Assistant template). | 
|How-To: Convert an existing v4 SDK Bot to a Skill <br/><br/><p align="center">[![Enable Skills on an existing v4 SDK Bot with C#](./media/csharp_icon.png)](/docs/howto/skills/csharp/skillenablingav4bot.md) [![Enable Skills on an existing v4 SDK Bot with TypeScript](./media/typescript_icon.png)](/docs/howto/skills/typescript/skillenablingav4bot.md)</p> | Steps required to take an existing Bot and make it available as a skill. |
| [How-To: Develop a Skill](/docs/howto/skills/bestpractices.md) | Best practices for developing your Bot Framework Skill |

# Reference

> Reference documentation providing more insight into key concepts across the Virtual Assistant, Skills and Analytics

## Virtual Assistant

| Name | Description |
| ---- | ----------- |
|[Template Outline](/docs/reference/assistant/templateoutline.md)|An outline of what the Virtual Assistant template provides|
|[Architecture](/docs/reference/assistant/underthecovers.md)|Detailed documentation covering what the template provides and how it works| 
|[Generating bot responses](/docs/reference/assistant/responses.md)|Your Virtual Assistant can respond in a variety of ways, depending on the scenario and the user's active device or conversation canvas|
|[Managing backend client events](/docs/reference/assistant/events.md)|Events enable custom apps or device experiences to pass device or contextual user information to an Assistant behind the scenes.|
|[Deployment scripts](/docs/reference/assistant/deploymentscripts.md) | Reference for deployment scripts provided in the Virtual Assistant Template |

## Skills

| Name | Description |
| ---- | ----------- |
|[Skills Architecture](/docs/reference/skills/architecture.md)|Under the covers of the Skill Implementation, SkillDialog, Adapter and Middleware|
|[Parent Bot to Skill Authentication](/docs/reference/skills/skillauthentication.md)  |Principles, Flow|
|[Skill Token Flow](/docs/reference/skills/skilltokenflow.md)|How a Skill can request a User authentication token|
|[Skill Manifest](/docs/reference/skills/skillmanifest.md)| Overview of the Skill Manifest file and its role with Skill registration and invocation|
|[Skill Command Line Tool](/docs/howto/skills/botskills.md)| Detailed information on the Botskills command line tool and the steps it performs on your behalf.|
|[Calendar Skill](/docs/reference/skills/productivity-calendar.md)|Add calendar capabilities to your Assistant. Powered by Microsoft Graph and Google.|
|[Email Skill](/docs/reference/skills/productivity-email.md)|Add email capabilities to your Assistant. Powered by Microsoft Graph and Google.|
|[To Do Skill](/docs/reference/skills/productivity-todo.md)|Add task management capabilities to your Assistant. Powered by Microsoft Graph.|
|[Point of Interest Skill](/docs/reference/skills/pointofinterest.md)|Find points of interest and directions. Powered by Azure Maps and FourSquare.|
|[Automotive Skill](/docs/reference/skills/automotive.md)|Industry-vertical Skill for showcasing the enabling car feature control.|
|[Experimental Skills](/docs/reference/skills/experimental.md)|News, Search, Restaurant Booking, and Weather.|

## Analytics

| Name | Description |
| ---- | ----------- |
|[Application Insights](/docs/reference/analytics/applicationinsights.md)|Detailed information on how Application Insights is used to collect information and powers our Analytics capabilities.|
|[Power BI Template](/docs/reference/analytics/powerbi.md)|Detailed information on how the provided PowerBI template provides insights into your Assistant usage.|
|[Telemetry](/docs/reference/analytics/telemetrylogging.md)|How to configure a telemetry collection for your Assistant.|

# Need Help?

Check out our [Known Issues](/docs/reference/knownissues.md) for common issues and resolutions.

If you have any questions, please start with [Stack Overflow](https://stackoverflow.com/questions/tagged/botframework) where we're happy to help.

Use the GitHub Issues page to raise [issues](https://github.com/Microsoft/AI/issues/new?assignees=&labels=Type%3A+Bug&template=bug_report.md&title=) or [feature requests](https://github.com/Microsoft/AI/issues/new?assignees=&labels=Type%3A+Suggestion&template=feature_request.md&title=).
