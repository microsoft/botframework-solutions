---
layout: tutorial
category: Skills
subcategory: Connect to a sample
title: Connect skill
order: 5
---

# Tutorial: {{page.subcategory}} 

## {{ page.title }}

Once you've deployed your Skill you can now add it to your Assistant. 

To add your new Skill to your assistant/Bot we provide a [botskills](https://www.npmjs.com/package/botskills) command line tool to automate the process of adding the Skill to your dispatch model and creating authentication connections where needed. 

Run the following command from a command prompt **within the directory of your assistant/Bot**. 

```bash
botskills connect --remoteManifest "{{site.data.urls.SkillManifest}}" --cs --luisFolder "<PATH_TO_LU_FOLDER>"
```

**Remember to re-publish your Assistant to Azure after you've added a Skill unless you plan on testing locally only**

See [Adding Skills]({{site.baseurl}}/skills/handbook/add-skills-to-a-virtual-assistant/) for more details.
