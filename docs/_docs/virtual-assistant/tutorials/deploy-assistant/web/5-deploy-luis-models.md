---
layout: tutorial
category: Virtual Assistant
subcategory: Deploy
language: web
title: Deploy LUIS models
order: 5
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{page.title}}

1. Run the following command for each .lu file in `\Deployment\Resources\LU` to parse the files to .json files that can be imported into the LUIS portal:
    ```
    bf luis:convert `
        --in "path-to-lu-file" `
        --culture "culture-code" `
        --out "output-file-name.luis or folder name"
    ```
1. In the LUIS portal, click "Create new app"
1. Provide a name, culture, and description for your app.
1. Click **Manage** > **Versions** > **Import version**
1. Browse to your .json file, then click "Done".
1. Train your LUIS app.
1. Publish your LUIS app.
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
