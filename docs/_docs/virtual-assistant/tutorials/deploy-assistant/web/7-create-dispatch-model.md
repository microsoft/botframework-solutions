---
layout: tutorial
category: Virtual Assistant
subcategory: Deploy
language:  web
title: Create a Dispatch LUIS model
order: 7
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{page.title}}

1. Initialize the dispatch model.
    ```
    dispatch init `
        --name "dispatch-name" `
        --luisAuthoringKey "luis-authoring-key" `
        --luisAuthoringRegion "luis-authoring-region `
        --dataFolder "path-to-output-folder"
    ```
1. Add LUIS and QnA Maker sources
    - Foreach LUIS app, run the following command:
        ```
        dispatch add `
            --type "luis" `
            --name "luis-app-name" `
            --id "luis-app-id"  `
            --region "luis-region" `
            --intentName "l_luis-app-name" `
            --dataFolder "path-to-output-folder"
            --dispatch "path-to-.dispatch-file"
        ```

    - Foreach QnA Maker knowledgebase, run the following command:
        ```
        dispatch add `
            --type "qna" `
            --name "kb-name `
            --id "kb-id"  `
            --key "qna-subscription-key" `
            --intentName "q_kb-app-name" `
            --dataFolder "path-to-output-folder"
            --dispatch "path-to-.dispatch-file"
        ```
1. Create the dispatch model.
    ```
    dispatch create `
        --dispatch "path-to-.dispatch-file" `
        --dataFolder "path-to-output-folder" `
        --culture "dispatch-culture"
    ```
1. Run luisgen tool to generate a .cs representation of your Dispatch model.
    ```
    luisgen "path-to-.json-file" -cs "DispatchLuis" -o "output-folder"
    ```
1. Add the following configuration to the `cognitiveModels.your-locale.dispatchModel` collection in cognitivemodels.json file:
    ```json
    "dispatchModel": {
        "authoringkey": "",
        "appid": "",
        "name": "",
        "subscriptionkey": "",
        "region": "",
        "authoringRegion": ""
    }
    ```