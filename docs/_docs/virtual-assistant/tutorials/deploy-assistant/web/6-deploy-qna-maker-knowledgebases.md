---
layout: tutorial
category: Virtual Assistant
subcategory: Deploy
language: web
title: Deploy QnA Maker knowledge bases
order: 6
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{page.title}}

The QnA Maker portal does not accept JSON files as input, so in order to deploy directly to the QnA Maker portal, you should either author new knowledgebases based on your scenario's needs directly in the portal, or import data in TSV format.

After creating your knowledgebases, update the `cognitiveModels.your-locale.knowledgebases` collection in cognitivemodels.json file for each knowledgebase:

```json
{
    "endpointKey": "",
    "kbId": "",
    "hostname": "",
    "subscriptionKey": "",
    "name": "",
    "id": ""
}
```