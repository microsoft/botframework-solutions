---
layout: tutorial
category: Virtual Assistant
subcategory: Deploy
language: Using CLI tools
title: Deploy LUIS models
order: 5
---

# Tutorial: {{page.subcategory}} ({{page.language}})
## {{page.title}}

1. Run the following command for each .lu file in `\Deployment\Resources\LU` to parse the files to .luis files that can be imported to LUIS:
    ```
    ludown parse toluis `
        --in "path-to-lu-file" `
        --luis_culture "culture-code" `
        --out_folder "output-folder" `
        --out "output-file-name.luis"
    ```
1. Run the following command to import the LUIS model into the LUIS portal.
    ```
    luis import application `
        --appName "app-name" `
        --authoringKey "luis-authoring-key" `
        --subscriptionKey "luis-authoring-key" `
        --region "region" `
        --in "path-to-luis-file" `
        --wait
    ```
1. Run the following command to train the LUIS model.
    ```
    luis train version `
        --appId "app-id" `
        --region "region" `
        --authoringKey "authoring-key" `
        --versionId "version-id" `
        --wait
    ```
1. Run the following command to publish the LUIS model.
    ```
    luis publish version `
        --appId "app-id" `
        --region "region" `
        --authoringKey "authoring-key" `
        --versionId "version-id" `
        --wait
    ```
1. Run the following command to create a .cs representation of your LUIS model.
    ```
    luisgen "path-to-luis-file" -cs "YourModelNameLuis" -o "path-to-output-folder"
    ```
1. For each LUIS model, add the following configuration to the `cognitiveModels.your-locale.languageModels` collection in cognitivemodels.json file:
    ```json
    {
        "subscriptionkey": "",
        "appid": "",
        "id": "",
        "version": "",
        "region": "",
        "name": "",
        "authoringkey": "",
        "authoringRegion": ""
    }
    ```
