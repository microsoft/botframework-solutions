---
layout: tutorial
category: Virtual Assistant
subcategory: Deploy
language: cli
title: Deploy LUIS models
order: 5
---

# Tutorial: {{page.subcategory}} ({{page.language}})
## {{page.title}}

1. Run the following command for each `.lu` file in `\Deployment\Resources\LU` to parse the files to `.luis` files that can be imported to LUIS. See [bf luis:convert](https://www.npmjs.com/package/@microsoft/botframework-cli#bf-luisconvert) command.
    ```
    bf luis:convert `
        --in "path-to-lu-file" `
        --culture "culture-code" `
        --out "output-file-name.luis or folder name"
    ```
1. Run the following command to import the LUIS application from JSON or LU content. See [bf luis:application:import](https://www.npmjs.com/package/@microsoft/botframework-cli#bf-luisapplicationimport) command.
    ```
    bf luis:application:import `
        --in "path-to-luis-file" `
        --name "app-name" `
        --endpoint "luid-endpoint-hostname" `
        --subscriptionKey "luis-authoring-key" `
        --save
    ```
1. Run the following command to train the LUIS application. See [bf luis:train:run](https://www.npmjs.com/package/@microsoft/botframework-cli#bf-luistrainrun) command.
    ```
    bf luis:train:run `
        --appId "app-id" `
        --endpoint "luis-endpoint-hostname" `
        --subscriptionKey "luis-authoring-key" `
        --versionId "version-id" `
        --wait
    ```
1. Run the following command to publish the LUIS application's version. See [bf luis:application:publish](https://www.npmjs.com/package/@microsoft/botframework-cli#bf-luisapplicationpublish) command.
    ```
    bf luis:application:publish `
        --appId "app-id" `
        --endpoint "luis-endpoint-hostname" `
        --subscriptionKey "luis-authoring-key" `
        --versionId "version-id"
    ```
1. Run the following command to generate a strongly typed C# source code from your exported (json) LUIS application. See [bf luis:generate:cs](https://www.npmjs.com/package/@microsoft/botframework-cli#bf-luisgeneratecs) command.
    ```
    bf luis:generate:cs `
        --in "path-to-json-file" `
        --className "luis-name" `
        --out "path-to-output-folder"
    ```
1. For each LUIS application, add the following configuration to the `cognitiveModels.your-locale.languageModels` collection in cognitivemodels.json file of your bot.
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
