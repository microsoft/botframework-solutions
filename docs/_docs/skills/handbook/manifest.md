---
category: Skills
subcategory: Handbook
title: Manifest
description: Overview of the Skill manifest and its role with Skill registration and invocation.
order: 2
toc: true
---

# {{ page.title }}
{:.no_toc}

The [Skill manifest](https://schemas.botframework.com/schemas/skills/v2.0/skill-manifest.json) enables Skills to be self-describing in that they communicate the name and description of a Skill. Each action provides utterances that the caller can use to identify when an utterance should be passed across to a skill along with slots (parameters) that it can accept for slot-filling if required.

This manifest provides all of the metadata required for a calling Bot to know when to trigger invoking a skill and what actions it provides. The manifest is used by the Botskills command-line tool to configure a Bot to make use of a Skill.

Each skill exposes a manifest endpoint enabling easy retrieval of a manifest, this is typically found at the `/manifest/manifest.json` of your Skill URI.

## Manifest structure

A manifest is made up of the following structure:

- Description
- Endpoints
- DispatchModels
- Activities
- Definitions
- ResponseObject

### Description
{:.no_toc}

The top level section of your Manifest provides high level information relating to your Skill, the table below provides more information on each item.

 Parameter  | Description | Required
 ---------  | ----------- | --------
 id | Identifier for your skill, no spaces or special characters | **Yes**
 name | Display name for your skill | **Yes**
 description | Description of the capabilities your Skill provides | **Yes**
 publisherName | Publisher name | **Yes**
 version | Version number for your skill | **Yes**
 iconUrl | Icon Uri representing your skill, potentially used to show the skills registered with a Bot | No
 copyright | Copyright message | No
 license | License information | No
 privacyUrl | Exposed entrypoint for communicating with your skill | No
 tags | Exposed entrypoint for communicating with your skill | No

```json
{
  "$schema": "https://schemas.botframework.com/schemas/skills/v2.0/skill-manifest.json",
  "$id": "SampleSkill",
  "name": "SampleSkill",
  "description": "SampleSkill description",
  "publisherName": "Your Company",
  "version": "1.0",
  "iconUrl": "https://{YOUR_SKILL_URL}/SampleSkill.png",
  "copyright": "Copyright (c) Microsoft Corporation. All rights reserved.",
  "license": "",
  "privacyUrl": "https://{YOUR_SKILL_URL}/privacy.html",
  "tags": [
    "sample",
    "skill"
  ]
}
```

### Endpoints

The `endpoints` section details 1 or more endpoints that your Skill will accept messages from.

- `endpointUrl` must be manually updated to reflect the deployed location of your Skill.
- `msAppId` must be manually updated to reflect the Azure AD Application ID of your deployed skill, this can be found in your `appSettings.json` file.

```json
"endpoints": [
  {
    "name": "production",
    "protocol": "BotFrameworkV3",
    "description": "Production endpoint for the SampleSkill",
    "endpointUrl": "https://{YOUR_SKILL_URL}/api/messages",
    "msAppId": "{YOUR_SKILL_APPID}"
  }
]
```

### Dispatch Models

The `dispatchModels` section provides pointers to accompanying language understanding data enabling a caller to train a local dispatcher to identify utterances that should be routed to the skill. 
- `languages` section supports multiple locales enabling a Skill to surface LU sources for each supported language.
- `url` property enables the manifest to point at a variety of locations including file, http and luis endpoints.
- `intents` section provides an optional mapping of Intents to the default message routing address. This enables a caller to only retrieve the LU data for the supported intents rather than the entire LU model.

```json
"dispatchModels": {
  "languages": {
    "en-us": [
      {
        "id": "SampleSkillLuModel-en",
        "name": "SampleSkill LU (English)",
        "contentType": "application/lu",
        "url": "",
        "description": "English language model for the skill"
      }
    ]
  },
  "intents": {
    "SampleIntent": "#/activities/message",
  }
}
```

### Activities

The `activities` section optionally defines a set of actions that the Skill supports. In assistant scenarios, utterance based triggering is typically used whereby an utterance is passed across to the Skill for intent detection and processing - the client's responsibility is purely to detect the question is within the domain of a Skill.

Action based invocation is analagous to a function call. It enables the caller to invoke a specific capability of a Skill optionally passing input data (slots) in the form of an input object (`value`) and receiving data back through the form of an output object (`resultValue`). This invocation is performed through an Activity of type `Event` with the `Name` property set to the required Action name.

The `Value` property is used on the incoming event used to indicate an action and also the final `EndOfConversation` activity sent from the Skill to a calling Bot.

If data isn't provided, the Skill can prompt for missing information as usual using Prompts. A Skill can also send response activities in addition to any result object.

The example activity definition below shows a `SampleAction` action being defined with `value` and `resultValue` types being specified in addition to a general message handler.

```json
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
  }
```

### Definitions

The `definitions` section provides the definitions for any types referenced in the preceding `activities` section. An example below defines two example objects.

```json
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
```

### Example Skill Manifest
{:.no_toc}

```json
{
  "$schema": "https://schemas.botframework.com/schemas/skills/v2.0/skill-manifest.json",
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