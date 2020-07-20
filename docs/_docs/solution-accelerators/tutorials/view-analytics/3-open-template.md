---
layout: tutorial
category: Solution Accelerators
subcategory: View analytics with Power BI
title: Open the Power BI template
order: 3
---

# Tutorial: {{page.subcategory}}

## {{ page.title }}

1. Open the [Virtual Assistant analytics template]({{site.baseurl}}/assets/analytics/virtual-assistant-analytics-sample.pbit) and paste your **Application Insights Application ID**.
![Screenshot of the load template view of a new Virtual Assistant analytics Power BI template]({{site.baseurl}}/assets/images/analytics/virtual-assistant-analytics-powerbi-load-template.png)

1. After loading the tables with your populated data, you should now see insights from your Virtual Assistant.

*Note: You may run into authentication issues when opening the template, particularly if you have opened the template previously using another Application ID. If so, perform the following steps to re-authenticate the template with your Application Insights service:*

1. Open the Template
2. File > Options and Settings > Data Source Settings
3. Click "Global permissions"
4. Click on anything resembling "api.logalytics.io" and Clear Permissions > Clear Permissions > Delete
5. Close and Re-open the Template
6. Paste your Application Insights AppId
7. Click Load
8. *Important*: Select Organizational Account > Sign In > Connect

## Additional Telemetry

By default, a Virtual Assistant or Skill template based project collects personally identifiable information (e.g. Conversation drill-down and transcripts) which will lead to the respective sections in the PowerBI dashboard to function as expected. If you wish to not collect this information make the following change to `appsettings.json`

Change this entry:

```csharp
    "logPersonalInfo": true
```

To the following:

```csharp
    "logPersonalInfo": false
```
