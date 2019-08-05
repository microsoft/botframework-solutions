---
category: Tutorials
subcategory: Customize a skill
language: csharp
title: Edit responses
order: 2
---

# Tutorial: Customize a Skill

## Edit default responses
Edit the MainResponses.json and SharedResponses.json files in the Responses folder to modify the default responses used by the template.

## Add additional responses
To add additional responses, create a new folder in the Responses directory, then copy the .tt and .json files from Responses/Sample. Rename the files to match your domain, then modify the json file as needed. In the Build menu of Visual Studio, run "Transform All t4 templates" to generate the necessary .cs file. In startup, register your response class in the ResponseManager.

### Learn More
For more information, refer to the [Skill Responses reference]({{site.baseurl}}/reference/skills/responses).