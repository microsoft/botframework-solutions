# Customize your Virtual Assistant (C#)

**APPLIES TO:** ✅ SDK v4

## In this tutorial

- [Intro](#intro)
- [Edit your greeting](#edit-your-greeting)
- [Edit your responses](#edit-your-responses)
- [Edit your cognitive models](#add-an-additional-knowledgebase)
- [Next steps](#next-steps)

## Intro

### Purpose

Learn how to navigate your assistant's project and make common customizations.

### Prerequisites

[Create a Virtual Assistant](./virtualassistant.md) to setup your environment.

### Time to Complete

20 minutes

### Scenario

A personalized Virtual Assistant with a new greeting and responses.

## Edit your greeting

The assistant's greeting uses an [Adaptive Card](https://adaptivecards.io/), an open framework that lets you describe your content as you see fit and deliver it beautifully wherever your customers are.

1. Copy and paste the following JSON payload:

```json
{
    "type": "AdaptiveCard",
    "id": "NewUserGreeting",
    "backgroundImage": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAKwAAACeCAYAAACvg+F+AAAABGdBTUEAALGPC/xhBQAAAAFzUkdCAK7OHOkAAAAJcEhZcwAAFiUAABYlAUlSJPAAAAAhdEVYdENyZWF0aW9uIFRpbWUAMjAxOTowMzoxMyAxOTo0Mjo0OBCBEeIAAAG8SURBVHhe7dJBDQAgEMCwA/+egQcmlrSfGdg6z0DE/oUEw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phSTEsKYYlxbCkGJYUw5JiWFIMS4phCZm52U4FOCAVGHQAAAAASUVORK5CYII=",
    "body": [
        {
            "type": "Container",
            "items": [
                {
                    "type": "Image",
                    "url": "data:image/png;base64,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",
                    "size": "Stretch"
                }
            ]
        },
        {
            "type": "Container",
            "spacing": "None",
            "backgroundImage": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABYAAAAXCAIAAACAiijJAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAqSURBVDhPY1RgL2SgDDBBaQrAqBEIMGoEAowagQCjRiDAqBEIQLERDAwAIisAxhgAwtEAAAAASUVORK5CYII=",
            "items": [
                {
                    "type": "TextBlock",
                    "id": "title",
                    "spacing": "Medium",
                    "size": "Large",
                    "weight": "Bolder",
                    "color": "Light",
                    "text": "Hi, I'm **your** Virtual Assistant",
                    "wrap": true
                },
                {
                    "type": "TextBlock",
                    "id": "body",
                    "size": "Medium",
                    "color": "Light",
                    "text": "Now that I'm up and running, explore the links here to learn what I can do.",
                    "wrap": true
                }
            ]
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "Get started",
            "data": {
                "action": "startOnboarding"
            }
        },
        {
            "type": "Action.OpenUrl",
            "title": "Documentation",
            "url": "https://aka.ms/virtualassistantdocs"
        },
        {
            "type": "Action.OpenUrl",
            "title": "Skills",
            "url": "https://aka.ms/botframeworkskills"
        }
    ],
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "version": "1.0",
    "speak": "Hi, I'm **your** Virtual Assistant. Now that I'm up and running, explore the links here to learn what I can do."
}
```

2. In your assistant's project, navigate to **Content** > **NewUserGreeting.json** and paste the new payload.
3. Press **F5** to start your assistant and start a new conversation in the **Bot Framework Emulator** to see the change:

<p align="center">
<img src="../../media/quickstart-virtualassistant-customizedgreeting.png" width="600">
</p>

To further customize the card, paste the card into the [Adaptive Cards Designer](https://adaptivecards.io/designer/) to better match your content.

## Edit your responses

Each dialog within your assistant contains a set of responses stored in supporting resource (`.resx`) files. You can edit the responses in the Visual Studio resource editor (shown below) to modify how your assistant responds.

You can change the responses in the Visual Studio resource editor as shown below to adjust how your bot responds.

<p align="center">
<img src="../../media/quickstart-virtualassistant-editresponses.png" width="600">
</p>

This approach supports multi-lingual responses using the standard resource file localization approach. Read more on [globalization and localization in ASP.NET Core.](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-2.1)

## Add an additional knowledgebase

You may wish to add an additional [QnA Maker](https://www.qnamaker.ai/) knowledge base to your assistant, this can be performed through the following steps.

1. Create your new knowledge base using the QnAMaker portal. You can alternatively create this from a new `.lu` file by adding that file to the corresponding resource folder. For example, if you are using an English resource, you should place it in the `deployment\resources\QnA\en` folder. To understand how to create a knowledge base from a `.lu` file using the `ludown` and `qnamaker` CLI tools please refer to [this blog post](https://blog.botframework.com/2018/06/20/qnamaker-with-the-new-botbuilder-tools-for-local-development/) for more information.

3. Update the `cognitiveModels.json` file in the root of your project with a new entry for your newly created QnAMaker knowledgebase, an example is shown below:

    ```json
        {
          "id": "YOUR_KB_ID",
          "name": "YOUR_KB_ID",
          "kbId": "",
          "subscriptionKey": "",
          "hostname": "https://YOUR_NAME-qnahost.azurewebsites.net",
          "endpointKey": ""
        }
    ```

    The `kbID`, `hostName` and `endpoint key` can all be found within the **Publish** page on the [QnAMaker portal](https://qnamaker.ai). The subscription key is available from your QnA resource in the Azure Portal.

4. The final step is to update your dispatch model and associated strongly typed class (LuisGen). We have provided the `update_cognitive_models.ps1` script to simplify this for you. The optional `-RemoteToLocal` parameter will generate the matching LU file on disk for your new knowledgebase (if you created using portal). The script will then refresh the dispatcher. 

    Run the following command from within  Powershell (pwsh.exe) within your **project directory**.

    ```shell
        .\Deployment\Scripts\update_cognitive_models.ps1 -RemoteToLocal
    ```

5. Update the `Dialogs\MainDialog.cs` file to include the corresponding Dispatch intent for your new QnA source following the existing examples provided.

You can now leverage multiple QnA sources as a part of your assistant's knowledge.

## Update your local LU files for LUIS and QnAMaker

As you build out your assistant you will likely update the LUIS models and QnAMaker knowledgebases for your Assistant in the respective portals. You'll then need to ensure the LU files representing your LUIS models in source control are kept up to date. We have provided the following script to refresh the local LU files for your project which is driven by the sources in your `cognitiveModels.json` file.

Run the following command from within  Powershell (pwsh.exe) within your **project directory**.

    ```shell
        .\Deployment\Scripts\update_cognitive_models.ps1 -RemoteToLocal
    ```


## Next steps

Now that you've learned learned how to personalize a Virtual Assistant, it's time to [create a new Bot Framework Skill](/docs/tutorials/csharp/skill.md).
