---
layout: tutorial
category: Virtual Assistant
subcategory: Deploy
language: cli
title: Create a Dispatch LUIS model
order: 7
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{page.title}}

1. Initialize the dispatch model. See [dispatch init](https://www.npmjs.com/package/botdispatch#initializing-dispatch) command.
    ```
    dispatch init `
        --name "dispatch-name" `
        --luisAuthoringKey "luis-authoring-key" `
        --luisAuthoringRegion "luis-authoring-region `
        --dataFolder "path-to-output-folder"
    ```
1. Add LUIS and QnA Maker sources to dispatch
    - Foreach LUIS app, run the following command. See [dispatch add](https://www.npmjs.com/package/botdispatch#adding-source-to-dispatch) command.
        ```
        dispatch add `
            --type "luis" `
            --name "luis-app-name" `
            --id "luis-app-id" `
            --region "luis-authoring-region" `
            --intentName "l_luis-app-name" `
            --dataFolder "path-to-output-folder" `
            --dispatch "path-to-.dispatch-file"
        ```

    - Foreach QnA Maker knowledgebase, run the following command.
        ```
        dispatch add `
            --type "qna" `
            --name "kb-name" `
            --id "kb-id" `
            --key "qna-subscription-key" `
            --intentName "q_kb-app-name" `
            --dataFolder "path-to-output-folder" `
            --dispatch "path-to-.dispatch-file"
        ```
1. Create the dispatch model. See [dispatch create](https://www.npmjs.com/package/botdispatch#creating-your-dispatch-model) command.
    ```
    dispatch create `
        --dispatch "path-to-.dispatch-file" `
        --dataFolder "path-to-output-folder" `
        --culture "dispatch-culture"
    ```
1. Run the following command to generate a strongly typed C# source code of your (json) Dispatch application. See [bf luis:generate:cs](https://www.npmjs.com/package/@microsoft/botframework-cli#bf-luisgeneratecs) command.
    ```
    bf luis:generate:cs `
        --in "path-to-json-file" `
        --className "DispatchLuis" `
        --out "path-to-output-folder"
    ```
1. Add the following configuration to the `cognitiveModels.your-locale.dispatchModel` collection in cognitivemodels.json file of your bot.
    ```json
    "dispatchModel": {
        "authoringkey": "",
        "appid": "",
        "name": "",
        "subscriptionkey": "",
        "region": "",
        "authoringRegion": "",
        "type": "dispatch"
    }
    ```
