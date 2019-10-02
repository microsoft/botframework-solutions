---
layout: tutorial
category: Skills
subcategory: Connect to a sample Skill
title: Connect skill
order: 4
---

# Tutorial: Connect a pre-built skill to your assistant

## Connect your Skill to an assistant

Once you've deployed your Skill you can now add it to your Assistant. 

To add your new Skill to your assistant/Bot we provide a `botskills` command line tool to automate the process of adding the Skill to your dispatch model and creating authentication connections where needed. 

Run the following command from a command prompt **within the directory of your assistant/Bot**. 

```bash
botskills connect --botName YOUR_BOT_NAME --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest" --luisFolder "<YOUR-SKILL_PATH>\Deployment\Resources\LU\en\" --cs
```

**Remember to re-publish your Assistant to Azure after you've added a Skill unless you plan on testing locally only**

See the [Adding Skills]({{site.baseurl}}/howto/skills/botskills.md#Connect-Skills) for more detail on how to add skills.
