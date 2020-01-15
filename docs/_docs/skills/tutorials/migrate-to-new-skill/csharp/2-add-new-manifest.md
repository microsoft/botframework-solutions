---
layout: tutorial
category: Skills
subcategory: Migrate to GA Bot Framework Skills
language: C#
title: Add a new manifest
order: 2
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

### Purpose

Now that your Skill has been updated to the latest GA version of Bot Framework Skills, we move on to providing a new Manifest file which describes your Skill's capabilities to a caller. This enables Power Virtual Agents to understand your Skills capabilities and invoke actions directly instead of relying solely on passing an utterance for processing.

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
  "iconUrl": "{YOUR_SKILL_URL}/sampleSkill.png",
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
        "response": {
          "type": "string",
          "description": "An example response object returned to the caller."
        }
      }
    }
  }
}
```

2. Update `{YOUR_SKILL_URL}` with the URL of your deployed Skill endpoint.

3. Update `{YOUR_SKILL_APPID}` with the Active Directory AppID of your deployed Skill.

4. Publish the changes to your Skill endpoint and validate that you can retrieve the manifest using the browser (`/manifest/manifest.json`)