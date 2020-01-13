---
layout: tutorial
category: Virtual Assistant
subcategory: Deploy
language: cli
title: Deploy QnA Maker knowledge bases
order: 6
---

# Tutorial: {{page.subcategory}} ({{page.language}})
## {{page.title}}

1. Run the following command for each .lu file in `\Deployment\Resources\QnA` to parse the files to .json files that can be deployed to QnA Maker:
    ```
    ludown parse toqna `
        --in "path-to-lu-file" `
        --out_folder "output-folder" `
        --out "output-file-name.qna"
    ```
1. Run the following command to import .qna file to QnA Maker.
    ```
    qnamaker create kb `
        --name "kb-name" `
        --subscriptionKey "qna-subscription-key" `
        --in "path-to-qna-file" `
        --force `
        --wait
    ```
1.  Run the following command to publish the knowledgebase.
    ```
    qnamaker publish kb `
        --kbId "kb-id" `
        --subscriptionKey "qna-subscription-key"
    ```
1. For each QnA Maker knowledgebase model, add the following configuration to the `cognitiveModels.your-locale.knowledgebases` collection in cognitivemodels.json file:
    ```json
    {
        "endpointKey": "",
        "kbId": "",
        "hostname": "",
        "subscriptionKey": "",
        "name": "",
        "id": ""
    }
    ```
