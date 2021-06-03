---
category: Virtual Assistant
subcategory: Handbook
title: Manage cognitive models across environments
description: When trying to develop language models in a distributed team, managing conflicts can be difficult. Refer to the following guidance for some common scenarios when managing cognitive models for a team.

order: 7
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

## I want to protect my production environment against conflicting changes made by multiple editors.
{:.no_toc}

It is recommended that for project being worked on by multiple developers that you protect your production cognitive models by only deploying changes through a build pipeline. This pipeline should run the various scripts/commands needed to update your LUIS models, QnA Maker knowledgebases, and Dispatch model automatically based on your source control. Individual developers should make their changes in their own versions of the models, and push their changes in to source control when they are ready to merge.

![]({{site.baseurl}}/assets/images/model_management_flow.png)

## I want to test changes to my LUIS models and QnA Maker knowledgebases in the portal.
{:.no_toc}

When you want to test changes to your LUIS models and QnA Maker knowledgebases in the portal, it is recommended that you deploy your own personal versions to develop with, and do not make changes directly in the production apps to prevent conflicts with the other developers. After you have made all the changes you want in the portal, follow these steps to share your changes with your team:

1. Run the following command from your project folder:

    ```
    .\Deployment\Scripts\update_cognitive_models.ps1 -RemoteToLocal
    ```

    > This script downloads your modified LUIS models in the `.lu` schema so it can be published to production by your build pipeline. If you are running this script from a Virtual Assistant project, it also runs [`dispatch refresh`](https://www.npmjs.com/package/botdispatch#refreshing-your-dispatch-model) and [`bf luis:generate:cs`](https://www.npmjs.com/package/@microsoft/botframework-cli#bf-luisgeneratecs) to update your Dispatch model and DispatchLuis.cs files.

1. Check in your updated `.lu` files to source control. 
    > Your changes should go through a peer review to validate there will be no conflicts. You can also share your LUIS app and/or transcripts of the bot conversation with your changes to help in this conversation.

1. Run your build pipeline to deploy your updated files to your production environment. 
    > This pipeline should update your LUIS models, QnA Maker knowledgebases, and Dispatch model as needed.

## I've changed my skill LUIS model. What's next?
{:.no_toc}

If you have added or removed an intent from your skill LUIS model, follow these steps to update your skill manifest:

1. Open the `manifest-1.1.json` file of your skill.
1. If you have added new intents, either add them to an existing `intents` or add a new action for the intent like this:

    ```json
    "intents": {
      "AddToDo": "#/activities/message",
      "ShowToDo": "#/activities/message",
      "MarkToDo": "#/activities/message",
      "DeleteToDo": "#/activities/message",
      "*": "#/activities/message"
    }
    ```

1. Once you have updated your manifest, run the following command from your project directory to update any Virtual Assistants that are using your skill:

    ```node
    botskills update --remoteManifest "http://<YOUR_SKILL_NAME>.azurewebsites.net/manifest/manifest-1.1.json" --cs --luisFolder "<PATH_TO_LU_FOLDER>"
    ```

    > This command updates the `botFrameworkSkills` property of your Virtual Assistant's `appsettings.json` file with the latest manifest definitions for each connected skill, and runs `dispatch refresh` to update your dispatch model.