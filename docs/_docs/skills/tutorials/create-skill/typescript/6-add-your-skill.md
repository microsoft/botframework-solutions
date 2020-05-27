---
layout: tutorial
category: Skills
subcategory: Create
language: typescript
title: Add your skill to a Virtual Assistant
order: 6
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

## Validate the Skill manifest endpoint

To validate your Skill is deployed and working open up a browser window and navigate to your deployed Skill manifest (`/api/skill/manifest endpoint`). e.g.  `http://localhost:3978/api/skill/manifest`

## Adding your skill to an assistant

To add your new Skill to your assistant we provide the [botskills](https://www.npmjs.com/package/botskills) command line tool to automate the process of adding the skill to your dispatch model and creating authentication connections where needed. 

Run the following command from a command prompt **within the directory of your assistant/Bot**. 

```bash
botskills connect --remoteManifest "https://<YOUR_SKILL_NAME>.azurewebsites.net/manifest/manifest-1.1.json" --ts --luisFolder "path-to-lu-folder"
```

Remember to re-publish your assistant to Azure after you’ve added a Skill unless you plan on testing locally only.

Once the connect command finish successfully, you can see under the `botFrameworkSkills` property of your assistant’s appsettings.json file that the following structure was added with the information provided in the Skill manifest.

```
    "botFrameworkSkills": {
        "id": "<SKILL_ID>",
        "appId": "<SKILL_APPID>",
        "skillEndpoint": "<SKILL_ENDPOINT>",
        "name": "<SKILL_NAME>",
        "description": "<SKILL_DESCRIPTION>"
    },
    "skillHostEndpoint": "<VA-SKILL_ENDPOINT>"
```

For further documentation, please check the following links:
- [Adding Skills]({{site.baseurl}}/skills/handbook/add-skills-to-a-virtual-assistant/)
- [Connect command]({{site.repo}}/tree/master/tools/botskills/docs/commands/connect.md)
