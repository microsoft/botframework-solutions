---
layout: tutorial
category: Virtual Assistant
subcategory: Customize
language: csharp
title: Edit your cognitive models
order: 4
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Update your knowledge bases
The Virtual Assistant Template includes two knowledge bases, FAQ and Chitchat, that can be customized to fit your scenario. For example, QnA Maker offers FAQ and PDF extraction to automatically build a knowledge base from your existing content ([learn more](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/concepts/data-sources-and-content)).

There are also a variety of prebuilt chitchat knowledge bases with different personality types ([learn more](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/how-to/chit-chat-knowledge-base)). 

Learn [how to edit a knowledge base in the QnA Maker portal](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/how-to/edit-knowledge-base). After publishing your desired changes, your Virtual Assistant Dispatch model will need to be updated with your changes. Run the following command from your project directory to update your Dispatch model:
```
./Deployment/Scripts/update_cognitive_models.ps1 -RemoteToLocal
```
> This script updates your local .lu files with any changes in made in the QnA Maker or LUIS portals, then runs [`dispatch refresh`](https://www.npmjs.com/package/botdispatch#refreshing-your-dispatch-model) to update your Dispatch model with the changes.

## Add an additional knowledge base

You may wish to add an additional [QnA Maker](https://www.qnamaker.ai/) knowledge base to your assistant, this can be performed through the following steps.

1. Create your new knowledge base using the QnAMaker portal. You can alternatively create this from a new `.lu` file by adding that file to the corresponding resource folder. For example, if you are using an English resource, you should place it in the `Deployment/Resources/QnA/en-us` folder. To understand how to create a knowledge base from a `.lu` and `.qna` files, please refer to [bf luis:convert](https://www.npmjs.com/package/@microsoft/botframework-cli#bf-luisconvert) and [bf qnamaker:convert](https://www.npmjs.com/package/@microsoft/botframework-cli#bf-qnamakerconvert) respectively of [botframework-cli](https://github.com/microsoft/botframework-cli) repository.

1. Update the `cognitivemodels.json` file in the root of your project with a new entry for your newly created QnA Maker knowledge base, an example is shown below:

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

    The `kbId`, `hostname` and `endpointKey` can all be found within the **Publish** page on the [QnA Maker portal](https://qnamaker.ai). The `subscriptionKey` is available from your QnA resource in the Azure Portal.

1. The final step is to update your dispatch model and associated strongly typed class ([bf luis:generate:cs](https://www.npmjs.com/package/@microsoft/botframework-cli#bf-luisgeneratecs)). We have provided the `update_cognitive_models.ps1` script to simplify this for you. The optional `-RemoteToLocal` parameter will generate the matching LU file on disk for your new knowledgebase (if you created using portal). The script will then refresh the dispatcher. 

    Run the following command from within  Powershell (pwsh.exe) within your **project directory**.

    ```shell
    ./Deployment/Scripts/update_cognitive_models.ps1 -RemoteToLocal
    ```

1. Update the `Dialogs/MainDialog.cs` file to include the corresponding Dispatch intent for your new QnA source following the existing examples provided. Also, add the following code:

    As global variable:
    ```csharp
    private const string QnAMakerKB = "ID_OF_YOUR_NEW_KB";
    ```

    In the method `OnContinueDialogAsync`:
    ```csharp
    if (innerDc.ActiveDialog.Id == QnAMakerKB)
    {
        // user is in a mult turn QnAMakerKB dialog
        var qnaDialog = TryCreateQnADialog(QnAMakerKB, localizedServices);
        if (qnaDialog != null)
        {
            Dialogs.Add(qnaDialog);
        }
    }
    ```

    In the method `RouteStepAsync`:
    ```csharp
    if (dispatchIntent == DispatchLuis.Intent.q_QnAMaker)
    {
        stepContext.SuppressCompletionMessage(true);

        var knowledgebaseId = QnAMakerKB;
        var qnaDialog = TryCreateQnADialog(knowledgebaseId, localizedServices);
        if (qnaDialog != null)
        {
            Dialogs.Add(qnaDialog);
        }

        return await stepContext.BeginDialogAsync(knowledgebaseId, cancellationToken: cancellationToken);
    }
    ```

    Finally, in the method `IsSkillIntent` add the following condition:
    ```csharp
    dispatchIntent.ToString().Equals(DispatchLuis.Intent.INTENT_OF_YOUR_NEW_KB.ToString(), StringComparison.InvariantCultureIgnoreCase)
    ```

You can now leverage multiple QnA sources as a part of your assistant's knowledge.

## Update your local LU files for LUIS and QnAMaker

As you build out your assistant, you will likely update the LUIS models and QnA Maker knowledge bases for your assistant in the respective portals. You'll then need to ensure the LU files representing your LUIS models in source control are kept up to date. We have provided the following script to refresh the local LU files for your project which is driven by the sources in your `cognitivemodels.json` file.

Run the following command from within  Powershell (pwsh.exe) within your **project directory**.

```shell
./Deployment/Scripts/update_cognitive_models.ps1 -RemoteToLocal
```