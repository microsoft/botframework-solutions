---
layout: tutorial
category: Skills
subcategory: Create
language: TypeScript
title: Add your skill to a Virtual Assistant
order: 6
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

## Validate the Skill manifest endpoint

- To validate your Skill is deployed and working open up a browser window and navigate to your deployed Skill manifest (`/api/skill/manifest endpoint`). e.g.  `http://localhost:3978/api/skill/manifest`

## Adding your Skill to an assistant

To add your new Skill to your assistant/Bot we provide a `botskills` command line tool to automate the process of adding the Skill to your dispatch model and creating authentication connections where needed. 

Run the following command from a command prompt **within the directory of your assistant/Bot**. 

```bash
botskills connect --botName YOUR_BOT_NAME --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest" --luisFolder "<YOUR-SKILL_PATH>\Deployment\Resources\LU" --languages "en-us" --cs
```

See the [Adding Skills]({{site.baseurl}}/skills/handbook/add-skills-to-a-virtual-assistant/) for more detail on how to add skills.
