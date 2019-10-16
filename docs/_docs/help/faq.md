---
category: Help
title: Frequently asked questions
order: 1
toc: true
---


# {{ page.title }}
{:.no_toc}

## Virtual Assistant

### What is the Bot Framework Virtual Assistant solution accelerator?
The Bot Framework Virtual Assistant template enables you to build a conversational assistant tailored to your brand, personalized for your users, and available across a broad range of clients and devices.
This greatly simplifies the creation of a new bot project by providing basic conversational intents, a dispatch model, Language Understanding and QnA Maker integration, SKills, and automated ARM deployment.

### What is the architecture of a Virtual Assistant solution?
Learn more about the [Virtual Assistant solution architecture]().

### How do I create a Virtual Assistant?
Follow a guided tutorial to create a Virtual Assistant (available in [C#]() or [Typescript]()).

### How do I customize a Virtual Assistant?
Follow a guided tutorial to customize a Virtual Assistant (available in [C#]() or [Typescript]()).

### How do I deploy a Virtual Assistant?
Learn how to deploy a Virtual Assistant by [automated scripts]() or [manual configuration]().

### How do I test a Virtual Assistant?
Learn how to [test a Virtual Assistant]().

### How do I pass events to a Virtual Assistant?
Event activities are used to pass metadata between a Bot and user without being visible to the user. The data from these activities can be processed by a Virtual Assistant to fulfill scenarios like providing a summary of the day ahead or filling semantic action slots on a Skill.
Learn more on [sample event activities packaged with the Virtual Assistant template]().

### How do I link user accounts to a Virtual Assistant?
Learn how to [link user accounts to a Virtual Assistant]().

### How do I collect feedback from users for a Virtual Assistant?
Learn more about using the [sample feedback middleware that enables you to capture feedback from a Virtual Assistant's users]() in Application Insights telemetry.

### How does localization work for a Virtual Assistant?
Learn how to [manage localization across a Virtual Assistant environment]().

### How do I send proactive messages to users?
Learn how to [send proactive messages to users]().

### How do I convert from the Enterprise Template to the Virtual Assistant Template?
Learn how to [convert from the Enterprise Template to the Virtual Assistant Template]().

### What happened to the Virtual Assistant solution (v0.3 and earlier)?
The Virtual Assistant solution from v0.3 and earlier was delivered with multiple sample SKills to support productivty and point of interest scenarios. These are now available as indepdendent [Bot Framework SKills], reusable Skills that can be added to an existing bot.

## Skills

### What is a Bot Framework Skill?
Bot Framework SKills are re-usable skill building blocks covering conversational use-cases, enabling you to add extensive functionality to a Bot within minutes.
Skills include Language Understanding models, dialogs, and integration code, and are delivered in source code - enabling you to customize and extend as required.

### What sample Skills are available?
The following sample Skills are available out of the box, with appropriate steps required to deploy and configure for your own use/
- [Calendar]()
- [Email]()
- [To Do]()
- [Point of Interest]()
- [Experimental]()

### How do I create a Bot Framework Skill?
Follow a guided tutorial to create a Bot Framework Skill (available in [C#]() or [Typescript]()).

### How do I customize a Bot Framework Skill?
Follow a guided tutorial to customize a Bot Framework Skill (available in [C#]() or [Typescript]()).

### What are the best practices when developing custom Bot Framework Skills?
Learn the [best practices when developing a custom Bot Framework Skill]().

### How do I add Skills to a Virtual Assistant?
Learn how to [add SKills to a Virtual Assistant]().

### What is a Bot Framework Skill manifest?
The [Bot Framework Skill manifest]() enables Skills to be self-describing in that they communicate the name and sceription of a SKill, it's authentication requirements (if appropriate), along with discrete actions it exposes.

This manifest provides all of the metadata required for a calling Bot to know when to trigger invoking a Skill and what actions it provides. The manifest is used by the Botskills command line tool to configure a Bot to make use of a SKill.

### How does Bot Framework Skill authentication work?
A Skill needs to be able to authenticate the request from a Virtual Assistant, [learn how a Skill uses JWT and whitelist authentication]().

### What is the Botskills Command Line (CLI) tool?
[Botskills command line tool]() allows you to automate teh connection between a Virtual Assistant and your Skills; this includes the process of updating your dispatch models and creating authentication connections when needed.

### How do I enable Bot Framework Skills on an existing v4 Bot?
Learn how to [enable Bot Framework Skill support on an existing v4 Bot]().

### How do I convert an existing v4 Bot to a Bot Framework Skill?
Learn how to [convert an existing v4 Bot to a Bot Framework Skill]().

## Analytics

### How do I enable analytics for a bot or a Virtual Assistant?
[Application Insights](https://azure.microsoft.com/en-us/services/application-insights/) is an Azure service which enables analytics about your applications, infrastructure and network. Bot Framework can use the built-in Application Insights telemetry to provide information about how your bot is performing and track key metrics. The Bot Framework SDK ships with several samples that demonstrate how to add telemtry to your bot and produce reports (included).

[Power BI](https://powerbi.microsoft.com/) is a business analytics services that lets you visualize your data and share insights across your organization. You can ingest data from Application Insights into live dashboards and reports.

[Learn more]({{site.baseurl}}/overview/analytics/)

### How do I configure Application Insights for a bot or Virtual Assistant?
Bot Framework can use the Application Insights telemetry to provide information about how your bot is performing, and track key metrics. The Bot Framework SDK ships with several samples that demonstrate how to add telemetry to your bot and produce reports (included).

Common queries for bot analytics are available in [Application Insights Analytics]().

Examples of Power BI dashboards are provided in the [Power BI Analytics sample](https://aka.ms/botPowerBiTemplate), highlighting how to gain insights on your bot's performance and quality.

### Where can I download the sample Power BI for a Virtual Assistant?
Examples of Power BI dashboards are provided in the [Power BI Analytics sample](https://aka.ms/botPowerBiTemplate), highlighting how to gain insights on your bot's performance and quality.

## Samples

### How do I set up Enterprise Notifications for a Virtual Assistant?
Learn how to [set up the Enterprise Notifications sample for a Virtual Assistant](https://aka.ms/enterprisenotificationssample).

### How do I use the Virtual Assistant Android Client?
Learn how to [configure your Virtual Assistant with the Virtual Assistant Android Client](https://aka.ms/bfvirtualassistantclientdocs).

### How do I use the Hospitality Assistant sample?
The [Hospitality Aassistant sample](https://aka.ms/hospitalityassistantdocs) is a prototype of a Virtual Assistant solution that helps to conceptualize and demonstrate how an assistant could be used in a hospitality-focused scenario. It also provides a starting point for those interested in creating an assistant customized for this scenario.