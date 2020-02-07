---
category: Overview
subcategory: What's New
language: 0_8_release
date: 2020-02-03
title: Deploy Virtual Assistant to Azure US Cloud
description: Steps to deploy the Virtual Assistant template to Azure US Government Cloud
order: 6
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}


## Prerequisites
- Azure US Government Azure Subscription

## Steps
1. Connect with Azure CLI
Connect to Azure Government by setting the cloud with the name `AzureUSGovernment`.

    ```powershell
    az cloud set --name AzureUSGovernment
    ```
    Once the cloud has been set, you can continue logging in:

    ```powershell
    az login
    ```

2. Add the following to your `parameters.template.json`:

    ```json
    "qnaMakerServiceLocation": {
      "value": "usgovvirginia"
    }
    ```

3. Run the deploy command from your **project directory** with the following parameters:

    ```powershell 
    .\Deployment\Scripts\deploy.ps1 `
        -parametersFile .\Deployment\Resources\parameters.template.json `
        -luisAuthoringKey your-authoring-key `
        -luisAuthoringRegion virginia `
        -armLuisAuthoringRegion usgovvirginia `
        -qnaEndpoint https://virginia.api.cognitive.microsoft.us/qnamaker/v4.0 `
        -useGov
    ```
