## How To: Customize Deployment

The Virtual Assistant Template relies on a number of Azure resources to run. The included deployment scripts and ARM template use the following default configurations for these services:

Resource | Tier | Notes |
-------- | ---- | ----- |
Azure Bot Service | S1 | The Azure Bot Service resource stores configuration information that allows your Virtual Assistant to be accessed on the supported Channels and provide OAuth authentication. |
Azure Blob Storage | Standard LRS | Used to store conversation transcripts.
Azure Cosmos DB | Standard | Used to store conversation state. |
Azure App Service Plan | S1 | Used to host your Bot Web App and QnA Maker Web App. |
Azure Application Insights | N/A | Used to capture conversation and application telemetry.
Bot Web App | N/A | Hosts your Bot application.
Language Understanding | S0 | Subscription keys for Language Understanding Cognitive Service.
QnA Maker | S0 | Subscription keys for QnA Maker Cognitive Service.
QnA Maker Web App | N/A | Hosts your QnA Maker knowledgebases.
QnA Maker Azure Search Service | Standard | Search index for your QnA Maker knowledgebases.
Content Moderator | S0 | Subscription keys for Content Moderator Cognitive Service.

Any of the following parameters in the ARM template can be overridden with your preferred values using the `parameters.template.json` file provided in the `Deployment\Resources` folder:
- name
- location  
- microsoftAppId
- microsoftAppPassword
- cosmosDbName
- storageAccountName
- appServicePlanName
- appServicePlanSku
- appInsightsName
- appInsightsLocation
- botWebAppName
- botServiceName
- botServiceSku
- contentModeratorName
- contentModeratorSku
- contentModeratorLocation
- luisServiceName
- luisServiceSku
- luisServiceLocation
- qnaMakerServiceName
- qnaMakerServiceSku
- qnaServiceLocation
- qnaMakerSearchName
- qnaMakerSearchSku
- qnaMakerWebAppName

Simply update the parameters.template.json file with your preferred values, like so:

```
{
  "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "appInsightsLocation": {
      "value": "westus2"
    },
    "qnaMakerSearchSku": {
      "value": "basic"
    }
  }
}
```

Then provide the path to the file as an argument on the `deploy.ps1` script:

```
.\Deployment\Scripts\deploy.ps1 -parametersFile .\Deployment\Resources\parameters.template.json
```