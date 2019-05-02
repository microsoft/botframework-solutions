![Bot Framework Solutions](/docs/media/bot_framework_solutions_header.png)
 
# Documentation Overview

## Table of Contents
- [Documentation Overview](#documentation-overview)
  - [Table of Contents](#table-of-contents)
- [Overview](#overview)
- [Tutorials](#tutorials)
- [Advanced Scenarios](#advanced-scenarios)
  - [Virtual Assistant](#virtual-assistant)
  - [Skills](#skills)
- [Reference documentation](#reference-documentation)
  - [Virtual Assistant](#virtual-assistant-1)
  - [Skills](#skills-1)
  - [Analytics](#analytics)
- [Need Help?](#need-help)

# Overview

High level overview into the Virtual Assistant, Skills and Analytics.

|Name|Description|
|-------------|-----------|
|[Virtual Assistant](/docs/overview/virtualassistant.md)|An introduction to the Virtual Assistant template and key concepts.|
|[Skills](/docs/overview/skills.md)|Developers can compose conversational experiences by stitching together re-usable conversational capabilities, known as Skills.|
|[Analytics](/docs/overview/analytics.md)|Gain key insights into your bot’s health and behavior|

# Tutorials

Create, Deploy and Customize your own Virtual Assistant and Skills in minutes.

|Name|Description|
|------------|-----------|
|1. Create your Assistant in 10 minutes <br/> <center>[![Create your Assistant in 10 minutes with C#](./media/csharp_icon.png)](/docs/tutorials/csharp/virtualassistant.md) [![Create your Assistant in 10 minutes with TypeScript](./media/typescript_icon.png)](/docs/tutorials/typescript/virtualassistant.md)</center>|Creating your assistant using the Virtual Assistant template|
|2. Customize your Assistant <br/> <center>[![Customize your Assistant with C#](./media/csharp_icon.png)](/docs/tutorials/csharp/customizeassistant.md) [![Customize your Assistant with TypeScript](./media/typescript_icon.png)](/docs/tutorials/typescript/customizeassistant.md)</center>|Personalize your assistant, change the name, branding, QnA| 
|3. Create and deploy a new Skill <br/> <center>[![Create and deploy a new Skill with C#](./media/csharp_icon.png)](/docs/tutorials/csharp/skill.md) [![Create and deploy a new Skill with TypeScript](./media/typescript_icon.png)](/docs/tutorials/typescript/skill.md)</center>|Creating a new Skill using the template|
|4. Customize your Skill <br/> <center>[![Customize your Skill with C#](./media/csharp_icon.png)](/docs/tutorials/csharp/customizeskill.md) [![Customize your Skill with TypeScript](./media/typescript_icon.png)](/docs/tutorials/typescript/customizeassistant.md)</center> |Building your first skill| 


# Advanced Scenarios

How-to guides on achieving more complex scenarios.

## Virtual Assistant

|Name|Description|Link|
|-------------|-----------|----|
|Under the covers|Detailed documentation covering what the template provides and how it works| [View](/docs/advanced/assistant/underthecovers.md)
|Enhancing your Assistant with additional Skills|Adding the out of the box Skills to your Virtual Assistant|[View](/docs/skills/common/addingskill.md)
|Migration from Enterprise Template|Guidance on how to move from an Enterprise Template based Bot to the new Template|[C#](/docs/advanced/assistant/csharp/ettovamigration.md)
|Migration from the old Virtual Assistant solution|Guidance on how to move from the original Virtual Assistant solution to the new Template|[C#](/docs/advanced/assistant/csharp/oldvatovamigration.md)
|Proactive Messaging|Adding proactive experiences to your assistant|[View](/docs/advanced/assistant/csharp/proactivemessaging.md)
|Linked Accounts|Enable users to link 3rd party accounts (e.g. o365) to their assistant|[View](/docs/advanced/assistant/linkedaccounts.md)
|Stitching together Bots into one conversational experience|Create one central Bot which hands-off to child bots, a common enterprise scenario.|[View](/docs/advanced/assistant/parentchildbotpattern.md)
|Configuring Deployment|How to customise the provided ARM template for different deployment scenarios.|[View](/docs/advanced/assistant/customisingdeployment.md)
|Adding Authentication to your assistant |How to add Authentication support to your Assistant| [C#](/docs/advanced/assistant/csharp/addauthentication.md), [TS](/docs/advanced/assistant/typescript/addauthentication.md)
|Adding KeyVault |How to add KeyVault support| [C#](/docs/advanced/assistant/csharp/keyvault.md), [TS](/docs/advanced/assistant/typescript/keyvault.md)

## Skills

|Name|Description|Link|
|-------------|-----------|----|
|Adding a new Skill to solution| Adding a Skill|[View](/docs/advanced/skills/addingskills.md)|
|Skill: Productivity - Calendar|Add calendar capabilities to your assistant. Powered by Microsoft Graph and Google.|[View](/docs/advanced/skills/productivity-calendar.md)
|Skill: Productivity - Email|Add email capabilities to your assistant. Powered by Microsoft Graph and Google.|[View](/docs/advanced/skills/productivity-email.md)
|Skill: Productivity - ToDo|Add task management capabilities to your assistant. Powered by Office Graph.|[View](/docs/advanced/skills/productivity-todo.md)
|Skill: Point of Interest|Find points of interest and directions powered by Azure Maps and FourSquare.|[View](/docs/advanced/skills/productivity-pointofinterest.md)
|Skill: Automotive|Industry Vertical skill for Automotive enabling car feature control.|[View](/docs/advanced/skills/automotive.md)
|Skill: Experimental|Experimental Skills:  News, Restaurant Booking and Weather.|[View](/docs/advanced/skills/experimental.md)
|Adding Skill support to a v4 SDK Bot|How to add Skills to an existing/non VA template solution.|[C#](/docs/advanced/skills/csharp/addskillsupportforv4bot.md), [TS](/docs/advanced/skills/typescript/addskillsupportforv4bot.md)
|Skill enabling an existing v4 SDK Bot|Steps required to take an existing v4 Bot and make it available as a skill|[C#](/docs/advanced/skills/csharp/skillenablingav4bot.md), [TS](/docs/advanced/skills/typescript/skillenablingav4bot.md)
|Best Practices for Skill Development|Design Best practices for Skills|[View](/docs/advanced/skills/bestpractices.md)

# Reference documentation 

Reference documentation providing more insight into key concepts across the Virtual Assistant, Skills and Analytics

## Virtual Assistant

|Name|Description|Link|
|-------------|-----------|----|
|Virtual Assistant Architecture|Detailed exploration of the overall Virtual Assistant Architecture|[View](/docs/reference/assistant/architecture.md)
|Project Structure|Walkthrough of your Virtual Assistant project|[View](/docs/reference/assistant/projectstructure.md)
|Responses|Your Virtual Assistant can respond in a variety of ways depending on the scenario and the users active device or conversation canvas|[View](/docs/reference/assistant/responses.md)
|Handling events|Events enable custom apps or device experiences to pass device or contextual user information to an assistant behind the scenes.|[View](/docs/reference/assistant/events.md)|
|Speech Enablement|Ensure your Virtual Assistant and Experiences work well in Speech scenarios|[View](/docs/reference/assistant/speechenablement.md)
|Deployment script approach|Walkthrough of the deployment script approach used in the Virtual Assistant|[View](/docs/reference/assistant/deploymentscriptapproach.md)

## Skills

|Name|Description|Link|
|-------------|-----------|----|
|Skills Architecture|Under the covers of the Skill Implementation, SkillDialog, Adapter and Middleware|[View](/docs/reference/skills/architecture.md)
|Parent Bot to Skill Authentication|Principles, Flow|[View](/docs/reference/skills/skillauthentication.md)        
|Skill Token Flow|How a Skill can request a User authentication token|[View](/docs/reference/skills/skilltokenflow.md)
|Skill Manifest| Overview of the Skill Manifest file and it's role with Skill registration and invocation|[View](/docs/reference/skills/skillmanifest.md)
|Skill CLI | Detailed information on the Skill command line tool and the steps it performs on your behalf.|[View](/docs/reference/skills/skillcli.md)
|Adaptive Card Styling|Adjusting look/feel - design packs?|[View](/docs/reference/skills/adaptivecardstyling.md)

## Analytics

|Name|Description|Link|
|-------------|-----------|----|
|Application Insights|Detailed information on how Application Insights is used to collect information and powers our Analytics capabilities.|[View](/docs/reference/analytics/applicationinsights.md)||
|PowerBI Template|Detailed information on how the provided PowerBI template provides insights into your assistant usage.|[View](/docs/reference/analytics/powerbi.md)
|Telemetry|How to configure telemetry collection for your assistant.|[View](/docs/reference/analytics/telemetrylogging.md)       

# Need Help?

If you have any questions please start with [Stack Overflow](https://stackoverflow.com/questions/tagged/botframework) where we're happy to help. Please use this GitHub Repos issue tracking capability to raise [issues](https://github.com/Microsoft/AI/issues/new?assignees=&labels=Type%3A+Bug&template=bug_report.md&title=) or [feature requests](https://github.com/Microsoft/AI/issues/new?assignees=&labels=Type%3A+Suggestion&template=feature_request.md&title=).
