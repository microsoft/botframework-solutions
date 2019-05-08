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
|<img src="https://raw.githubusercontent.com/Microsoft/AI/4.4/docs/media/vatemplateintrocard.png" width="1250"> | [**Virtual Assistant.**](https://github.com/Microsoft/AI/blob/master/docs/overview/virtualassistant.md) Customers and partners have a significant need to deliver a conversational assistant tailored to their brand, personalized to their users, and made available across a broad range of canvases and devices. <br/><br/>  This brings together all of the supporting components and greatly simplifies the creation of a new bot project including: basic conversational intents, Dispatch integration, QnA Maker, Application Insights and an automated deployment.|
|[<img src="https://raw.githubusercontent.com/Microsoft/AI/4.4/docs/media/calendarskillcardexample.png" width="1250">]((https://github.com/Microsoft/AI/blob/master/docs/readme.md))| [**Skills.**](https://github.com/Microsoft/AI/blob/master/docs/overview/skills.md) A library of re-usable conversational skill building-blocks enabling you to add functionality to a Bot. We currently provide: Calendar, Email, Task, Point of Interest, Automotive, Weather and News skills. Skills include LUIS models, Dialogs, and integration code delivered in source code form to customize and extend as required.|
|<img src="https://raw.githubusercontent.com/Microsoft/AI/4.4/docs/media/powerbi-conversationanalytics-luisintents.png" width="1250">| [**Analytics.**](https://github.com/Microsoft/AI/blob/master/docs/readme.md#analytics) Gain key insights into your bot’s health and behavior with the Bot Framework Analytics solutions, which includes: sample Application Insights queries, and Power BI dashboards to understand the full breadth of your bot’s conversations with users.|

# Tutorials

> Create, deploy, and customize your own Virtual Assistant and Skills using these walkthroughs.

|Name|Description|
|------------|-----------|
|1. Quickstart: Create your first Virtual Assistant<br/><br/> <p align="center">[![Quickstart: Create a Virtual Assistant with C#](./media/csharp_icon.png)](/docs/tutorials/csharp/virtualassistant.md)[![Quickstart: Create a Virtual Assistant with TypeScript](./media/typescript_icon.png)](/docs/tutorials/typescript/virtualassistant.md)</p>|Deploying your own assistant using the Virtual Assistant template|
|2. Customize your Virtual Assistant <br/><br/><p align="center">[![Customize your Virtual Assistant with C#](./media/csharp_icon.png)](/docs/tutorials/csharp/customizeassistant.md) [![Customize your Assistant with TypeScript](./media/typescript_icon.png)](/docs/tutorials/typescript/customizeassistant.md)</p>|Personalize your assistant, change the name, branding, QnA| 
|3. Create a new Skill <br/><br/><p align="center">[![Create and deploy a new Skill with C#](./media/csharp_icon.png)](/docs/tutorials/csharp/skill.md) [![Create and deploy a new Skill with TypeScript](./media/typescript_icon.png)](/docs/tutorials/typescript/skill.md)</p>|Creating a new Skill using the template|
|4. Customize your Skill <br/><br/><p align="center">[![Customize your Skill with C#](./media/csharp_icon.png)](/docs/tutorials/csharp/customizeskill.md)</p>|Building your first skill| 

# How-To

> How-to guides for achieving more complex scenarios.

## Virtual Assistant

| Name | Description |
| ---- | ----------- |
| [How To: Enable Linked Accounts](/docs/howto/assistant/linkedaccounts.md) | Enable users to link third party accounts (e.g. Office 365) to your Assistant |
| How To: Migrate from the Enterprise Template <br/><br/><p align="center">[![Migrating from the Enterprise Template with C#](./media/csharp_icon.png)](/docs/howto/assistant/csharp/ettovamigration.md)</p> | Guidance on how to move from an Enterprise Template based Bot to the new Template|
| How to: Migrate the Virtual Assistant (Beta Release 0.3) solution to the Virtual Assistant Template <br/><br/> :construction_worker_woman: |Guidance on how to move from the original Virtual Assistant solution to the new Template |
| [How To: Messaging your users proactively](/docs/howto/assistant/csharp/proactivemessaging.md) | Adding proactive experiences to your assistant |
| [How To: Enable cross bot communication into one conversational experience](/docs/howto/assistant/parentchildbotpattern.md) | Create one central Assistant which hands-off to child bots (a common enterprise scenario) |
| [How To: Customize Azure Resource Deployment](/docs/howto/assistant/customizedeployment.md) | How to customise the provided ARM template for different deployment scenarios. |
| How To: Secure your keys using Azure Key Vault <br/>:construction_worker_woman: | How to safeguard your keys using Azure Key Vault|

## Skills

| Name | Description |
| ---- | ----------- |
| [How To: Add a new Skill to your Assistant](/docs/howto/skills/addingskills.md) | Adding a Skill |
| How To: Add Skills to an existing SDK v4 bot<br/><br/><p align="center">[![Adding Skill support to a v4 SDK Bot with C#](./media/csharp_icon.png)](/docs/howto/skills/csharp/addskillsupportforv4bot.md) [![Adding Skill support to a v4 SDK Bot with TypeScript](./media/typescript_icon.png)](/docs/howto/skills/typescript/addskillsupportforv4bot.md)</p>|How to add Skills to an existing bot (not Virtual Assistant template). | 
|How To: Convert an existing v4 SDK Bot to a Skill <br/><br/><p align="center">[![Enable Skills on an existing v4 SDK Bot with C#](./media/csharp_icon.png)](/docs/howto/skills/csharp/skillenablingav4bot.md) [![Enable Skills on an existing v4 SDK Bot with TypeScript](./media/typescript_icon.png)](/docs/howto/skills/typescript/skillenablingav4bot.md)</p> | Steps required to take an existing and make it available as a skill. |
| [How To: Develop a Skill](/docs/howto/skills/bestpractices.md) | Design Best practices for Skills |

# Reference 

> Reference documentation providing more insight into key concepts across the Virtual Assistant, Skills and Analytics

## Virtual Assistant

| Name | Description |
| ---- | ----------- |
|[Template Outline](/docs/reference/assistant/templateoutline.md)|An outline of what the Virtual Assistant template provides|
|[Under the covers](/docs/reference/assistant/underthecovers.md)|Detailed documentation covering what the template provides and how it works| 
|[Underlying architecture](/docs/reference/assistant/architecture.md)|Detailed exploration of the overall Virtual Assistant Architecture|
|[Template structure](/docs/reference/assistant/projectstructure.md)|Walkthrough of your Virtual Assistant project|
|[Generating bot responses](/docs/reference/assistant/responses.md)|Your Virtual Assistant can respond in a variety of ways depending on the scenario and the users active device or conversation canvas|
|[Managing backend client events](/docs/reference/assistant/events.md)|Events enable custom apps or device experiences to pass device or contextual user information to an assistant behind the scenes.|
|[Enabling speech scenarios](/docs/reference/assistant/speechenablement.md)|Ensure your Virtual Assistant and Experiences work well in Speech scenarios|
|[Deployment Scripts](/docs/reference/assistant/deploymentscripts) | Reference for deployment scripts provided in the Virtual Assistant Template |

## Skills

| Name | Description |
| ---- | ----------- |
|[Skills Architecture](/docs/reference/skills/architecture.md)|Under the covers of the Skill Implementation, SkillDialog, Adapter and Middleware|
|[Parent Bot to Skill Authentication](/docs/reference/skills/skillauthentication.md)  |Principles, Flow|
|[Skill Token Flow](/docs/reference/skills/skilltokenflow.md)|How a Skill can request a User authentication token|
|[Skill Manifest](/docs/reference/skills/skillmanifest.md)| Overview of the Skill Manifest file and it's role with Skill registration and invocation|
|[Skill CLI Tools](/docs/reference/skills/skillcli.md)| Detailed information on the Skill command line tool and the steps it performs on your behalf.|
|[Adaptive Card Styling](/docs/reference/skills/adaptivecardstyling.md)|Adjusting the look and feel of an Assistant's cards to reflect your brand|
|[Calendar Skill](/docs/reference/skills/productivity-calendar.md)|Add calendar capabilities to your assistant. Powered by Microsoft Graph and Google.|
|[Email Skill](/docs/reference/skills/productivity-email.md)|Add email capabilities to your assistant. Powered by Microsoft Graph and Google.|
|[To Do Skill](/docs/reference/skills/productivity-todo.md)|Add task management capabilities to your assistant. Powered by Microsoft Graph.|
|[Point of Interest Skill](/docs/reference/skills/productivity-pointofinterest.md)|Find points of interest and directions. Powered by Azure Maps and FourSquare.|
|[Automotive Skill](/docs/reference/skills/automotive.md)|Industry-vertical Skill for showcasing enabling car feature control.|
|[Experimental Skills](/docs/reference/skills/experimental.md)|News, Restaurant Booking and Weather.|

## Analytics

| Name | Description |
| ---- | ----------- |
|[Application Insights](/docs/reference/analytics/applicationinsights.md)|Detailed information on how Application Insights is used to collect information and powers our Analytics capabilities.|
|[Power BI Template](/docs/reference/analytics/powerbi.md)|Detailed information on how the provided PowerBI template provides insights into your assistant usage.|
|[Telemetry](/docs/reference/analytics/telemetrylogging.md)|How to configure telemetry collection for your assistant.|       

# Need Help?
Check out our [Known Issues](/docs/reference/knownissues.md) for common issues and resolutions.

If you have any questions please start with [Stack Overflow](https://stackoverflow.com/questions/tagged/botframework) where we're happy to help. 

Use the GitHub Issues page to raise [issues](https://github.com/Microsoft/AI/issues/new?assignees=&labels=Type%3A+Bug&template=bug_report.md&title=) or [feature requests](https://github.com/Microsoft/AI/issues/new?assignees=&labels=Type%3A+Suggestion&template=feature_request.md&title=).
