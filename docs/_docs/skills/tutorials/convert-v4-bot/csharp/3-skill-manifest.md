---
layout: tutorial
category: Skills
subcategory: Convert a v4 Bot
language: C#
title: Add a Skill Manifest
order: 3
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

Create a `manifestTemplate.json` file in the root of your Bot. Ensure at a minimum the root level `id`, `name`, `description` and action details are completed.

```json
{
  "id": "",
  "name": "",
  "description": "",
  "iconUrl": "",
  "authenticationConnections": [ ],
  "actions": [
    {
      "id": "",
      "definition": {
        "description": "",
        "slots": [ ],
        "triggers": {
          "utteranceSources": [
            {
              "locale": "en",
              "source": [
                "luisModel#intent"
              ]
            }
          ]
        }
      }
    }
  ]
}
```