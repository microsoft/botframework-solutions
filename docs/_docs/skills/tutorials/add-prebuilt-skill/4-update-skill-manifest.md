---
layout: tutorial
category: Skills
subcategory: Connect to a sample
title: Update your Skill Manifest
order: 4
---

# Tutorial: {{page.subcategory}} 

## {{ page.title }}

Once you've deployed the Skill, you need to update the Skill Manifest present in the bot in order to connect the Virtual Assistant to your Skill.

Currently, the manifest that you will update is available in the `manifest` folder of the Skill. `manifest-1.0` is provided for Power Virtual Agent support only, you should use `manifest-1.1` for Virtual Assistant scenarios.

| Language | Manifest folder |
|----------|-----------------|
| C# | [Link](https://github.com/microsoft/botframework-solutions/tree/master/samples/csharp/skill/SkillSample/wwwroot/manifest) |
| TypeScript | [Link](https://github.com/microsoft/botframework-solutions/tree/master/templates/typescript/samples/sample-skill/src/manifest) |

As soon as you open the Skill Manifest, you will find the following placeholders that **must be replaced** before connecting the Skill to the Virtual Assistant.
- `{YOUR_SKILL_URL}`: endpoint URL of the deployed Skill where the bot will receive the messages that matches with the "Messaging endpoint" of the Web App Bot resource after deployment (e.g. if the value is https://bf-skill.azurewebsites.net/api/messages, {YOUR_SKILL_URL} should be bf-skill.azurewebsites.net for the manifest).
- `{YOUR_SKILL_APPID}`: microsoftAppId value present in the appsettings.json file populated after the deployment of the Skill.

_Example of a Skill manifest-1.1.json_
```json
{
  "$schema": "https://schemas.botframework.com/schemas/skills/skill-manifest-2.1.preview-0.json",
  "$id": "SampleSkill",
  "name": "SampleSkill",
  "description": "SampleSkill description",
  "publisherName": "Your Company",
  "version": "1.1",
  "iconUrl": "https://{YOUR_SKILL_URL}/SampleSkill.png",
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
      "description": "Production endpoint for the SampleSkill",
      "endpointUrl": "https://{YOUR_SKILL_URL}/api/messages",
      "msAppId": "{YOUR_SKILL_APPID}"
    }
  ],
  "dispatchModels": {
    "languages": {
      "en-us": [
        {
          "id": "SampleSkillLuModel-en",
          "name": "CalendarSkill LU (English)",
          "contentType": "application/lu",
          "url": "file://SkillSample.lu",
          "description": "English language model for the skill"
        }
      ],
      "de-de": [
        {
          "id": "SampleSkillLuModel-de",
          "name": "CalendarSkill LU (German)",
          "contentType": "application/lu",
          "url": "file://SkillSample.lu",
          "description": "German language model for the skill"
        }
      ],
      "es-es": [
        {
          "id": "SampleSkillLuModel-es",
          "name": "CalendarSkill LU (Spanish)",
          "contentType": "application/lu",
          "url": "file://SkillSample.lu",
          "description": "Spanish language model for the skill"
        }
      ],
      "fr-fr": [
        {
          "id": "SampleSkillLuModel-fr",
          "name": "CalendarSkill LU (French)",
          "contentType": "application/lu",
          "url": "file://SkillSample.lu",
          "description": "French language model for the skill"
        }
      ],
      "it-it": [
        {
          "id": "SampleSkillLuModel-it",
          "name": "CalendarSkill LU (Italian)",
          "contentType": "application/lu",
          "url": "file://SkillSample.lu",
          "description": "Italian language model for the skill"
        }
      ],
      "zh-cn": [
        {
          "id": "SampleSkillLuModel-zh",
          "name": "CalendarSkill LU (Chinese)",
          "contentType": "application/lu",
          "url": "file://SkillSample.lu",
          "description": "Chinese language model for the skill"
        }
      ]
    },
    "intents": {
      "Sample": "#/activities/message",
      "*": "#/activities/message"
    }
  },
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