---
layout: tutorial
category: Skills
subcategory: Convert a v4 Bot
language: csharp
title: Add a Skill Manifest
order: 3
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

### Steps

1. Create a folder called `Manifest` within the `wwwroot` of your Skill project and create a new file called `manifest.json` with the JSON fragment below. Ensure the Build Action on the file is set to Content.

This example manifest surfaces two `activities`. One that enables a users utterance to be passed along with `SampleAction` that defines a data structure that can be passed as an input parameter and another data structure that can be returned from the Skill. 

```json
{
  "$schema": "https://schemas.botframework.com/schemas/skills/skill-manifest-2.0.0.json",
  "$id": "SampleSkill",
  "name": "SampleSkill",
  "description": "Sample Skill description",
  "publisherName": "Your Company",
  "version": "1.0",
  "iconUrl": "https://{YOUR_SKILL_URL}/sampleSkill.png",
  "copyright": "Copyright (c) Microsoft Corporation. All rights reserved.",
  "license": "",
  "privacyUrl": "https://{YOUR_SKILL_URL}/privacy.html",
  "tags": [
    "sample",
    "skill"
  ],
  "endpoints": [
    {
      "name": "production",
      "protocol": "BotFrameworkV3",
      "description": "Production endpoint for the Sample Skill",
      "endpointUrl": "https://{YOUR_SKILL_URL}/api/messages",
      "msAppId": "{YOUR_SKILL_APPID}"
    }
  ],
  "activities": {
    "sampleAction": {
      "description": "Sample action which accepts an input object and returns an object back.",
      "type": "event",
      "name": "SampleAction",
      "value": {
        "$ref": "#/definitions/inputObject"
      },
      "resultValue": {
        "$ref": "#/definitions/responseObject"
      }
    },
    "message": {
      "type": "message",
      "description": "Receives the users utterance and attempts to resolve it using the skill's LU models"
    }
  },
  "definitions": {
    "inputObject": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string",
          "description": "The users name."
        }
      }
    },
     "responseObject": {
      "type": "object",
      "properties": {
        "customerId": {
          "type": "integer",
          "description": "A customer identifier."
        }
      }
    }
  }
}
```

1. Update `{YOUR_SKILL_URL}` with the URL of your deployed Skill endpoint, this must be prefixed with https.

1. Update `{YOUR_SKILL_APPID}` with the Active Directory AppID of your deployed Skill, you can find this within your `appSettings.json` file.

1. Publish the changes to your Skill endpoint and validate that you can retrieve the manifest using the browser (`/manifest/manifest.json`)