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

|Name|Languages|Description|
|------------|:---:|-----------|
|1. Create your Assistant in 10 minutes|[![Create your Assistant in 10 minutes with C#](./media/csharp_icon.png)](/docs/tutorials/csharp/virtualassistant.md) [![Create your Assistant in 10 minutes with TypeScript](./media/typescript_icon.png)](/docs/tutorials/typescript/virtualassistant.md)|Creating your assistant using the Virtual Assistant template|
|2. Customize your Assistant|[![Customize your Assistant with C#](./media/csharp_icon.png)](/docs/tutorials/csharp/customizeassistant.md) [![Customize your Assistant with TypeScript](./media/typescript_icon.png)](/docs/tutorials/typescript/customizeassistant.md)|Personalize your assistant, change the name, branding, QnA| 
|3. Create and deploy a new Skill|[![Create and deploy a new Skill with C#](./media/csharp_icon.png)](/docs/tutorials/csharp/skill.md) [![Create and deploy a new Skill with TypeScript](./media/typescript_icon.png)](/docs/tutorials/typescript/skill.md)|Creating a new Skill using the template|
|4. Customize your Skill|[![Customize your Skill with C#](./media/csharp_icon.png)](/docs/tutorials/csharp/customizeskill.md) [![Customize your Skill with TypeScript](./media/typescript_icon.png)](/docs/tutorials/typescript/customizeassistant.md)|Building your first skill| 


# Advanced Scenarios

How-to guides on achieving more complex scenarios.

## Virtual Assistant

|Name|Languages &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; |Description|
|------------|:---:|-----------|
|[Under the covers](/docs/advanced/assistant/underthecovers.md)||Detailed documentation covering what the template provides and how it works| 
|[Enhancing your Assistant with additional Skills](/docs/skills/common/addingskill.md)||Adding the out-of-the-box Skills to your Virtual Assistant|
|Migrating from the Enterprise Template|[![Migrating from the Enterprise Template with C#](./media/csharp_icon.png)](/docs/advanced/assistant/csharp/ettovamigration.md)|Guidance on how to move from an Enterprise Template based Bot to the new Template|
|Migrating from the beta Virtual Assistant solution|[![Migrating from the beta Virtual Assistant solution with C#](./media/csharp_icon.png)](/docs/advanced/assistant/csharp/oldvatovamigration.md)|Guidance on how to move from the original Virtual Assistant solution to the new Template|
|[Messaging your users proactively](/docs/advanced/assistant/csharp/proactivemessaging.md)||Adding proactive experiences to your assistant|
|[Linked Accounts](/docs/advanced/assistant/linkedaccounts.md)||Enable users to link third party accounts (e.g. Office 365) to your Assistant|
|[Enable cross bot communication into one conversational experience](/docs/advanced/assistant/parentchildbotpattern.md)||Create one central Assistant which hands-off to child bots (a common enterprise scenario)|
|[Configuring Deployment](/docs/advanced/assistant/customisingdeployment.md)||How to customise the provided ARM template for different deployment scenarios.|
|Authenticating users to your Assistant|[![Authenticating users to your Assistant with C#](./media/csharp_icon.png)](/docs/advanced/assistant/csharp/addauthentication.md) [![Authenticating users to your Assistant with TypeScript](./media/typescript_icon.png)](/docs/advanced/assistant/typescript/addauthentication.md)|How to add Authentication support to your Assistant|
|Secure your keys using Azure Key Vault|[![Secure your keys using Azure Key Vault with C#](./media/csharp_icon.png)](/docs/advanced/assistant/csharp/keyvault.md) [![Secure your keys using Azure Key Vault with TypeScript](./media/typescript_icon.png)](/docs/advanced/assistant/typescript/keyvault.md) |How to safeguard your keys using Azure Key Vault|

## Skills

|Name|Languages|Description|
|------------|:---:|-----------|
|[Adding a new Skill to solution](/docs/advanced/skills/addingskills.md)|| Adding a Skill|
|[Calendar Skill](/docs/advanced/skills/productivity-calendar.md)||Add calendar capabilities to your assistant. Powered by Microsoft Graph and Google.|
|[Email Skill](/docs/advanced/skills/productivity-email.md)||Add email capabilities to your assistant. Powered by Microsoft Graph and Google.|
|[To-Do Skill](/docs/advanced/skills/productivity-todo.md)||Add task management capabilities to your assistant. Powered by Microsoft Graph.|
|[Point of Interest Skill](/docs/advanced/skills/productivity-pointofinterest.md)||Find points of interest and directions. Powered by Azure Maps and FourSquare.|
|[Automotive Skill](/docs/advanced/skills/automotive.md)||Industry-vertical Skill for showcasing enabling car feature control.|
|[Experimental Skills](/docs/advanced/skills/experimental.md)||News, Restaurant Booking and Weather.|
|Adding Skill support to a v4 SDK Bot|[![Adding Skill support to a v4 SDK Bot with C#](./media/csharp_icon.png)](/docs/advanced/skills/csharp/addskillsupportforv4bot.md) [![Adding Skill support to a v4 SDK Bot with TypeScript](./media/typescript_icon.png)](/docs/advanced/skills/typescript/addskillsupportforv4bot.md)|How to add Skills to an existing/non VA template solution.|
|Enable Skills on an existing v4 SDK Bot|[![Enable Skills on an existing v4 SDK Bot with C#](./media/csharp_icon.png)](/docs/advanced/skills/csharp/skillenablingav4bot.md) [![Enable Skills on an existing v4 SDK Bot with TypeScript](./media/typescript_icon.png)](/docs/advanced/skills/typescript/skillenablingav4bot.md)|Steps required to take an existing v4 Bot and make it available as a skill|
|[Best practices for Skill development](/docs/advanced/skills/bestpractices.md)||Design Best practices for Skills|

# Reference documentation 

Reference documentation providing more insight into key concepts across the Virtual Assistant, Skills and Analytics

## Virtual Assistant

|Name|Description|
|-------------|-----------|
|[Virtual Assistant Architecture](/docs/reference/assistant/architecture.md)|Detailed exploration of the overall Virtual Assistant Architecture|
|[Project Structure](/docs/reference/assistant/projectstructure.md)|Walkthrough of your Virtual Assistant project|
|[Write bot responses](/docs/reference/assistant/responses.md)|Your Virtual Assistant can respond in a variety of ways depending on the scenario and the users active device or conversation canvas|
|[Handle backend client events](/docs/reference/assistant/events.md)|Events enable custom apps or device experiences to pass device or contextual user information to an assistant behind the scenes.|
|[Enable speech scenarios](/docs/reference/assistant/speechenablement.md)|Ensure your Virtual Assistant and Experiences work well in Speech scenarios|
|[Deployment script](/docs/reference/assistant/deploymentscriptapproach.md)|Walkthrough of the deployment script approach used in the Virtual Assistant|

## Skills

|Name|Description|
|-------------|-----------|
|[Skills Architecture](/docs/reference/skills/architecture.md)|Under the covers of the Skill Implementation, SkillDialog, Adapter and Middleware|
|[Parent Bot to Skill Authentication](/docs/reference/skills/skillauthentication.md)  |Principles, Flow|      
|[Skill Token Flow](/docs/reference/skills/skilltokenflow.md)|How a Skill can request a User authentication token|
|[Skill Manifest](/docs/reference/skills/skillmanifest.md)| Overview of the Skill Manifest file and it's role with Skill registration and invocation|
|[Skill CLI Tools](/docs/reference/skills/skillcli.md)| Detailed information on the Skill command line tool and the steps it performs on your behalf.|
|[Adaptive Card Styling](/docs/reference/skills/adaptivecardstyling.md)|Adjusting the look and feel of an Assistant's cards to reflect your brand|

## Analytics

|Name|Description|
|-------------|-----------|
|[Application Insights](/docs/reference/analytics/applicationinsights.md)|Detailed information on how Application Insights is used to collect information and powers our Analytics capabilities.|
|[Power BI Template](/docs/reference/analytics/powerbi.md)|Detailed information on how the provided PowerBI template provides insights into your assistant usage.|
|[Telemetry](/docs/reference/analytics/telemetrylogging.md)|How to configure telemetry collection for your assistant.|       

# Need Help?

If you have any questions please start with [Stack Overflow](https://stackoverflow.com/questions/tagged/botframework) where we're happy to help. Please use this GitHub Repos issue tracking capability to raise [issues](https://github.com/Microsoft/AI/issues/new?assignees=&labels=Type%3A+Bug&template=bug_report.md&title=) or [feature requests](https://github.com/Microsoft/AI/issues/new?assignees=&labels=Type%3A+Suggestion&template=feature_request.md&title=).
