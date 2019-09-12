---
category: Tutorials
subcategory: Create a skill
language: C#
title: Add your skill to a Virtual Assistant
order: 6
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Validate the Skill manifest endpoint

- To validate your Skill is deployed and working open up a browser window and navigate to your deployed Skill manifest (`/api/skill/manifest endpoint`). e.g.  `http://localhost:3978/api/skill/manifest`

## Adding your Skill to an assistant

To add your new Skill to your assistant/Bot we provide a `botskills` command line tool to automate the process of adding the Skill to your dispatch model and creating authentication connections where needed. 

Run the following command from a command prompt **within the directory of your assistant/Bot**. 

```bash
botskills connect --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest" --luisFolder "<YOUR-SKILL_PATH>/Deployment/Resources/LU/en/" --cs
```

See the [Adding Skills]({{site.baseurl}}/howto/skills/addingskills) for more detail on how to add skills.