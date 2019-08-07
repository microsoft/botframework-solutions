---
category: Overview
title: Analytics
order: 3
---

# {{ page.title }}
{:.no_toc}

## Contents
{:.no_toc}

* 
{:toc}

## Intro

[Application Insights](https://azure.microsoft.com/en-us/services/application-insights/) is an Azure service which enables analytics about your applications, infrastructure and network. The Bot Framework can use the  Application Insights telemetry to provide information about how your bot is performing, and track key metrics. The Bot Framework SDK ships with several samples that demonstrate how to add telemetry to your bot and produce reports (included).

[Power BI](https://powerbi.microsoft.com/) is a business analytics services that lets you visualize your data and share insights across your organization. You can ingest data from Application Insights into live dashboards and reports.

## Prerequisites

The [Conversational Analytics Power BI sample](https://aka.ms/botPowerBiTemplate) is generated under the assumption you are using the latest Bot Framework SDK and telemetry middleware. You can find these (and generate the required Application Insights resource) with the following samples:

- [Virtual Assistant Template]({{site.repo}}/tree/master/templates/Virtual-Assistant-Template/csharp/Sample)
- [Skill Template]({{site.repo}}/tree/master/templates/Skill-Template/csharp/Sample)

### Configuring Sentiment

LUIS enables you to run a sentiment analysis on a user's utterance. This can be enabled through the [LUIS portal](https://www.luis.ai).
Sentiment must be enabled for each application. To enable sentiment:

1. Log in to the portal.
2. Select **My Apps**.
3. Click on the specific application you want to enable sentiment.
4. Select **Manage** on the upper menu.
5. Select **Publish Settings** on the side menu. It should resemble the below.

![Enabling Sentiment]({{site.baseurl}}/assets/images/enable_sentiment.png)

6. **Enable** the *Use sentiment analysis to determine if a user's utterance is positive, negative, or neutral* checkbox.
7. Select **Publish** and repeat for each LUIS application.

### Power BI Installation

The [PowerBI Desktop client](https://aka.ms/pbidesktopstore) is available for Windows clients.
Alternatively, you can use the Power BI service. If you don't have a PowerBI service account, sign up for a [free 60 day trial account](https://app.powerbi.com/signupredirect?pbi_source=web) and upload the Power BI template to view the reports.

## Telemetry Logging

This [guide](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-telemetry?view=azure-bot-service-4.0) highlights the provided telemetry for bot and user activities, including [LUIS](https://www.luis.ai/) and [QnA Maker](https://www.qnamaker.ai/) results. how to configure your bot's telemetry, either through bot configuring or overriding the telemetry client.

## Application Insights Analytics

Common queries for bot analytics are available in [Applications Insights Analytics]({{site.baseurl}}/reference/analytics/applicationinsights).

## Power BI Analytics Sample

Examples of Power BI dashboards are provided in the [Power BI Analytics sample]({{site.baseurl}}/reference/analytics/powerbi), highlighting how to gain insights on your bot's performance and quality.