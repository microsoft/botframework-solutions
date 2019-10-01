---
category: Virtual Assistant
subcategory: How-to
title: Deploy a Virtual Assistant manually
description: How to manually deploy and configuring your Virtual Assistant
order: 1
---

# {{ page.title }}
{:.no_toc}

## In this how-to
{:.no_toc}

* 
{:toc}

### Intro
The Virtual Assistant comes with a set of scripts to simplify the deployment process. However, if you'd like to manually deploy and configure your assistant, you can follow these steps.

### Create MSA App Registration
#### Option 1: Create registration using Az CLI
Run the following command to create your app registration:

```
az ad app create `
    --display-name 'your-app-name' `
    --password 'your-app-pw' `
    --available-to-other-tenants `
    --reply-urls 'https://token.botframework.com/.auth/web/redirect'
```

#### Option 2: Create registration manually in Azure Portal
Follow the [Register an application in Azure AD](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-tutorial-authentication?view=azure-bot-service-3.0&tabs=aadv1#register-an-application-in-azure-ad) instructions.
> Under **Supported account types** you should select either "Accounts in any organizational directory" or "Accounts in any organizational directory and personal Microsoft accounts" to ensure the Azure Bot Service can correctly expose your bot via Bot Channels. 

### Deploy ARM template with parameters
#### Option 1: Deploy arm template using Az CLI
Run the following command to deploy the Virtual Assistant ARM template:
```
az group deployment create `
    --resource-group "resource-group-name" `
    --template-file "path-to-arm-template"`
    --parameters "path-to-arm-parameters-file" `
    --parameters microsoftAppId='ms-app-id' microsoftAppPassword='ms-app-pw'
```

#### Option 2: Deploy arm template using Azure Portal
1. Click on the following button to load the Virtual Assistant ARM template in the Azure Portal:
<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3a%2f%2fraw.githubusercontent.com%2fmicrosoft%2fbotframework-solutions%2fmaster%2ftemplates%2fVirtual-Assistant-Template%2fcsharp%2fSample%2fVirtualAssistantSample%2fDeployment%2fResources%2ftemplate.json" class="btn btn-default">Deploy to Azure</a>
1. Provide your Microsoft App Id and Microsoft App Password, and override any default parameter values as needed.
1. Click "Purchase" to deploy.

### Update appsettings.json with configuration
After your Azure resources have been deployed, fill in the following keys and secrets in appsettings.json with the values from your deployed resources:
```json
{
  "microsoftAppId": "",
  "microsoftAppPassword": "",
  "ApplicationInsights": {
    "InstrumentationKey": ""
  },
  "blobStorage": {
    "connectionString": "",
    "container": "transcripts"
  },
  "cosmosDb": {
    "collectionId": "botstate-collection",
    "databaseId": "botstate-db",
    "cosmosDBEndpoint": "",
    "authKey": ""
  }
}
```

### Deploy LUIS models
#### Option 1: Deploy with BF CLI tools
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

#### Option 2: Deploy manually to LUIS portal
1. Run the following command for each .lu file in `\Deployment\Resources\LU` to parse the files to .json files that can be imported into the LUIS portal:
    ```
    ludown parse toluis `
        --in "path-to-lu-file" `
        --luis_culture "culture-code" `
        --out_folder "output-folder" `
        --out "output-file-name.json"
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

### Deploy QnA Maker knowledgebases
#### Option 1: Deploy with BF CLI tools
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

#### Option 2: Deploy manually to QnA Maker portal
The QnA Maker portal does not accept JSON files as input, so in order to deploy directly to the QnA Maker portal, you should either author new knowledgebases based on your scenario's needs directly in the portal, or import data in TSV format.

After creating your knowledgebases, update the `cognitiveModels.your-locale.knowledgebases` collection in cognitivemodels.json file for each knowledgebase:

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

### Create Dispatch model
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