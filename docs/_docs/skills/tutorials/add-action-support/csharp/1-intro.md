---
layout: tutorial
category: Skills
subcategory: Add action support
language: csharp
title: Intro
order: 1
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

### Purpose

In the Bot Framework 4.7 release, the Bot Framework Skills capability was transitioned into a core part of the core SDK and reached the General Availability (GA) milestone. Existing Virtual Assistant and Skill Template projects built using Bot Builder packages 4.6.2 and below need to be migrated in order to use this new approach. With the 4.7 skill protocol, any bot can become a skill without adapter changes hence the simplification that was achieved.

As part of this new capability, Actions can now be defined alongside existing utterance invocation enabling callers to invoke a specific capability of your Skill passing input parameters and receiving output parameters. This enables two key scenarios:

- Products such as Power Virtual Agents to invoke actions directly without needing the user to type a question activating the Skill.
- A parent Bot can orchestrate calls across multiple Skills as part of a experience, aggregating returned datasets. For example a "Good Morning" experience could retrieve Calendar Items, Tasks and Customer information and create a unified adaptive card with key information.

This tutorial will guide you through adding Action support to a Skill.

### Prerequisites

- [Create a new skill ]({{site.baseurl}}/skills/tutorials/create-skill/csharp/1-intro) to provision a new Skill or if you have an existing skill created using an older version of the Skill Template then [Migrate to GA Skill support.]({{site.baseurl}}/overview/whats-new/0.8-beta/migrate-existing-skills-to-0.8)

## Review Manifest

1. Within the `wwwroot\manifest` folder of your Skill you will find the manifest file(s) related to your Skill. The provided template  surfaces two `activities`. One that enables a users utterance to be passed along with an action called `SampleAction` which defines a data structure that can be passed as an input parameter and another data structure that can be returned from the Skill. 

> At the time of writing Power Virtual Agents only supports the [2.0](https://schemas.botframework.com/schemas/skills/skill-manifest-2.0.0.json) manifest rather than the extended [2.1](https://schemas.botframework.com/schemas/skills/skill-manifest-2.1.preview-0.json) version. Therefore, in Power Virtual Agent scenarios ensure you use the `manifest-1.0` manifest file at this time rather than `manifest-1.1`.

The sample below shows a 2.0 manifest example with an action defined:

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

## Add your own Action

Following the example shown above, add an additional action relating to your scenario and as appropriate define additional input and output object types. See the [manifest documentation]({{site.baseurl}}/skills/handbook/manifest/) for more information.