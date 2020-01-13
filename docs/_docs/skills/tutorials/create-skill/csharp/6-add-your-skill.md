---
layout: tutorial
category: Skills
subcategory: Create
language: csharp
title: Add your skill to a Virtual Assistant
order: 6
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

## Validate the Skill manifest endpoint

- To validate your Skill is deployed and working open up a browser window and navigate to your deployed Skill manifest (`/api/skill/manifest endpoint`). e.g.  `http://localhost:3978/api/skill/manifest`

## Adding your skill to an assistant

To add your new Skill to your assistant we provide the `botskills` command line tool to automate the process of adding the skill to your dispatch model and creating authentication connections where needed. 

Run the following command from a command prompt **within the directory of your assistant/Bot**. 

```bash
botskills connect --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest" --luisFolder "<YOUR-SKILL_PATH>\Deployment\Resources\LU" --languages "en-us" --cs
```

See [Adding Skills]({{site.baseurl}}/skills/handbook/add-skills-to-a-virtual-assistant/) for more details.
