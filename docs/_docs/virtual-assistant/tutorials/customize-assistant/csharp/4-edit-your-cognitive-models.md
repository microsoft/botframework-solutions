---
layout: tutorial
category: Virtual Assistant
subcategory: Customize
language: C#
title: Edit your cognitive models
order: 4
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Update your knowledgebases
The Virtual Assistant Template include two knowledgebases, FAQ and Chitchat, that can be customized to fit your scenario. For example, QnA Maker offers FAQ and PDF extraction to automatically build a knowledgebase from your existing content ([learn more](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/concepts/data-sources-supported)). They also offer a variety of prebuilt chitchat knowledgebases with different personality types ([learn more](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/how-to/chit-chat-knowledge-base)). Refer to this documentation to learn how to edit your knowledgebases in the QnA Maker portal: https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/how-to/edit-knowledge-base.

Once you have made your desired changes, your Virtual Assistant Dispatch model will need to be updated with your changes. Run the following command from your project directory to update your Dispatch model:
```
.\Deployment\Scripts\update_cognitive_models.ps1 -RemoteToLocal
```
> This script updates your local .lu files with any changes in made in the QnA Maker or LUIS portals, then runs `dispatch refresh` to update your Dispatch model with the changes.

## Add an additional knowledgebase

You may wish to add an additional [QnA Maker](https://www.qnamaker.ai/) knowledge base to your assistant, this can be performed through the following steps.

1. Create your new knowledge base using the QnAMaker portal. You can alternatively create this from a new `.lu` file by adding that file to the corresponding resource folder. For example, if you are using an English resource, you should place it in the `deployment/resources/QnA/en` folder. To understand how to create a knowledge base from a `.lu` file using the `ludown` and `qnamaker` CLI tools please refer to [this blog post](https://blog.botframework.com/2018/06/20/qnamaker-with-the-new-botbuilder-tools-for-local-development/) for more information.

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
        ./Deployment/Scripts/update_cognitive_models.ps1 -RemoteToLocal
    ```

5. Update the `Dialogs/MainDialog.cs` file to include the corresponding Dispatch intent for your new QnA source following the existing examples provided.

You can now leverage multiple QnA sources as a part of your assistant's knowledge.

## Update your local LU files for LUIS and QnAMaker

As you build out your assistant you will likely update the LUIS models and QnAMaker knowledgebases for your Assistant in the respective portals. You'll then need to ensure the LU files representing your LUIS models in source control are kept up to date. We have provided the following script to refresh the local LU files for your project which is driven by the sources in your `cognitiveModels.json` file.

Run the following command from within  Powershell (pwsh.exe) within your **project directory**.

    ```shell
        ./Deployment/Scripts/update_cognitive_models.ps1 -RemoteToLocal
    ```
