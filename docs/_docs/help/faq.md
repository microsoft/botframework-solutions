---
category: Help
title: Frequently asked questions
order: 1
toc: true
---


# {{ page.title }}
{:.no_toc}

## Virtual Assistant

### What is the Bot Framework Virtual Assistant Solution Accelerator?
{:.no_toc}
The Bot Framework Virtual Assistant template enables you to build a conversational assistant tailored to your brand, personalized for your users, and available across a broad range of clients and devices.
This greatly simplifies the creation of a new bot project by providing basic conversational intents, a dispatch model, Language Understanding and QnA Maker integration, SKills, and automated ARM deployment.

### What is the architecture of a Virtual Assistant solution?
{:.no_toc}
Learn more about the [Virtual Assistant solution architecture]({{site.baseurl}}/overview/virtual-assistant-solution).

### How do I create a Virtual Assistant?
{:.no_toc}
Follow a guided tutorial to create a Virtual Assistant (available in [C#]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/csharp/1-intro) or [Typescript]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/typescript/1-intro)).

### How do I customize a Virtual Assistant?
{:.no_toc}
Follow a guided tutorial to customize a Virtual Assistant (available in [C#]({{site.baseurl}}/virtual-assistant/tutorials/customize-assistant/csharp/1-intro) or [Typescript]({{site.baseurl}}/virtual-assistant/tutorials/customize-assistant/typescript/1-intro).

### How do I deploy a Virtual Assistant?
{:.no_toc}
Learn how to deploy a Virtual Assistant by [automated scripts]({{site.baseurl}}/virtual-assistant/handbook/deployment-scripts) or [manual configuration]({{site.baseurl}}/virtual-assistant/tutorials/deploy-assistant/cli/1-intro).

### How do I test a Virtual Assistant?
{:.no_toc}
Learn how to [test a Virtual Assistant]({{site.baseurl}}/virtual-assistant/handbook/testing/).

### How do I pass events to a Virtual Assistant?
{:.no_toc}
Event activities are used to pass metadata between a Bot and user without being visible to the user. The data from these activities can be processed by a Virtual Assistant to fulfill scenarios like providing a summary of the day ahead or filling semantic action slots on a Skill.
Learn more on [sample event activities packaged with the Virtual Assistant template]({{site.baseurl}}/virtual-assistant/handbook/events/).

### How do I link user accounts to a Virtual Assistant?
{:.no_toc}
Learn how to [link user accounts to a Virtual Assistant]({{site.baseurl}}/solution-accelerators/samples/linked-accounts/).

### How do I collect feedback from users for a Virtual Assistant?
{:.no_toc}
Learn more about using the [sample feedback middleware that enables you to capture feedback from a Virtual Assistant's users]({{site.baseurl}}/virtual-assistant/handbook/feedback/) in Application Insights telemetry.

### How does localization work for a Virtual Assistant?
{:.no_toc}
Learn how to [manage localization across a Virtual Assistant environment]({{site.baseurl}}/virtual-assistant/handbook/localization/).

### How do I send proactive messages to users?
{:.no_toc}
Learn how to [send proactive messages to users]({{site.baseurl}}/solution-accelerators/samples/proactive-notifications/).

### How do I convert from the Enterprise Template to the Virtual Assistant Template?
{:.no_toc}
Learn how to [convert from the Enterprise Template to the Virtual Assistant Template]({{site.baseurl}}/virtual-assistant/handbook/migration/).

### What happened to the Virtual Assistant solution (v0.3 and earlier)?
{:.no_toc}
The Virtual Assistant solution from v0.3 and earlier was delivered with multiple sample Skills to support productivty and point of interest scenarios. These are now available as indepdendent [Bot Framework SKills], reusable Skills that can be added to an existing bot.

## Skills

### What is a Bot Framework Skill?
{:.no_toc}
Bot Framework Skills are re-usable skill building blocks covering conversational use-cases, enabling you to add extensive functionality to a Bot within minutes.
Skills include Language Understanding models, dialogs, and integration code, and are delivered in source code - enabling you to customize and extend as required.

### What sample Skills are available?
{:.no_toc}
The following sample Skills are available out of the box, with appropriate steps required to deploy and configure for your own use/
- [Calendar]({{site.baseurl}}/skills/samples/calendar)
- [Email]({{site.baseurl}}/skills/samples/email)
- [To Do]({{site.baseurl}}/skills/samples/to-do)
- [Point of Interest]({{site.baseurl}}/skills/samples/point-of-interest)
- [Experimental]({{site.baseurl}}/overview/skills/#experimental-skills)

### How do I create a Bot Framework Skill?
{:.no_toc}
Follow a guided tutorial to create a Bot Framework Skill (available in [C#]({{site.baseurl}}/skills/tutorials/create-skill/csharp/1-intro) or [Typescript]({{site.baseurl}}/skills/tutorials/create-skill/typescript/1-intro)).

### How do I customize a Bot Framework Skill?
{:.no_toc}
Follow a guided tutorial to customize a Bot Framework Skill (available in [C#]({{site.baseurl}}/skills/tutorials/customize-skill/csharp/1-intro) or [Typescript]({{site.baseurl}}/skills/tutorials/customize-skill/typescript/1-intro)).

### What are the best practices when developing custom Bot Framework Skills?
{:.no_toc}
Learn the [best practices when developing a custom Bot Framework Skill]({{site.baseurl}}/skills/handbook/best-practices).

### How do I add Skills to a Virtual Assistant?
{:.no_toc}
Learn how to [add Skills to a Virtual Assistant]({{site.baseurl}}/skills/handbook/add-skills-to-a-virtual-assistant).

### What is a Bot Framework Skill manifest?
{:.no_toc}
The [Bot Framework Skill manifest]({{site.baseurl}}/skills/handbook/manifest) enables Skills to be self-describing in that they communicate the name and sceription of a SKill, it's authentication requirements (if appropriate), along with discrete actions it exposes.

This manifest provides all of the metadata required for a calling Bot to know when to trigger invoking a Skill and what actions it provides. The manifest is used by the Botskills command line tool to configure a Bot to make use of a SKill.

### How does Bot Framework Skill authentication work?
{:.no_toc}
A Skill needs to be able to authenticate the request from a Virtual Assistant, [learn how a Skill uses JWT and whitelist authentication]({{site.baseurl}}/skills/handbook/authentication).

### What is the Botskills Command Line (CLI) tool?
{:.no_toc}
[Botskills command line tool]({{site.baseurl}}/skills/handbook/botskills) allows you to automate the connection between a Virtual Assistant and your Skills; this includes the process of updating your dispatch models and creating authentication connections when needed.

### How do I enable Bot Framework Skills on an existing v4 Bot?
{:.no_toc}
Learn how to [Migrate existing Virtual Assistant to Bot Framework Skills GA]({{site.baseurl}}/overview/whats-new/0.8-beta/migrate-existing-va-to-0.8/).

### How do I convert an existing v4 Bot to a Bot Framework Skill?
{:.no_toc}
Learn how to [Migrate existing Skills to Bot Framework Skills GA]({{site.baseurl}}/overview/whats-new/0.8-beta/migrate-existing-skills-to-0.8/).

## Analytics

### How do I enable analytics for a bot or a Virtual Assistant?
{:.no_toc}
[Application Insights](https://azure.microsoft.com/en-us/services/application-insights/) is an Azure service which enables analytics about your applications, infrastructure and network. Bot Framework can use the built-in Application Insights telemetry to provide information about how your bot is performing and track key metrics. The Bot Framework SDK ships with several samples that demonstrate how to add telemtry to your bot and produce reports (included).

[Power BI](https://powerbi.microsoft.com/) is a business analytics services that lets you visualize your data and share insights across your organization. You can ingest data from Application Insights into live dashboards and reports.

[Learn more]({{site.baseurl}}/solution-accelerators/tutorials/view-analytics/1-intro/)

### How do I configure Application Insights for a bot or Virtual Assistant?
{:.no_toc}
Bot Framework can use the Application Insights telemetry to provide information about how your bot is performing, and track key metrics. The Bot Framework SDK ships with several samples that demonstrate how to add telemetry to your bot and produce reports (included).

Examples of Power BI dashboards are provided in the [Power BI Analytics sample](https://aka.ms/botPowerBiTemplate), highlighting how to gain insights on your bot's performance and quality.

### Where can I download the sample Power BI for a Virtual Assistant?
{:.no_toc}
Examples of Power BI dashboards are provided in the [Power BI Analytics sample](https://aka.ms/botPowerBiTemplate), highlighting how to gain insights on your bot's performance and quality.

## Samples

### How do I set up Enterprise Notifications for a Virtual Assistant?
{:.no_toc}
Learn how to [set up the Enterprise Notifications sample for a Virtual Assistant](https://aka.ms/enterprisenotificationssample).

### How do I use the Virtual Assistant Android Client?
{:.no_toc}
Learn how to [configure your Virtual Assistant with the Virtual Assistant Android Client](https://aka.ms/bfvirtualassistantclientdocs).

### How do I use the Hospitality Assistant sample?
{:.no_toc}
The [Hospitality Aassistant sample](https://aka.ms/hospitalityassistantdocs) is a prototype of a Virtual Assistant solution that helps to conceptualize and demonstrate how an assistant could be used in a hospitality-focused scenario. It also provides a starting point for those interested in creating an assistant customized for this scenario.