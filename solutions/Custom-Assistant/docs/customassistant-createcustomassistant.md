---
title: Creating your own Custom Assistant | Microsoft Docs
description: Learn about how to get started with creating your Custom Assistant
author: darrenj
ms.author: darrenj
manager: kamrani
ms.topic: article
ms.prod: bot-framework
ms.date: 13/12/2018
monikerRange: 'azure-bot-service-3.0'
---

# Custom Assistant Creation

## Overview

# Prerequisites

- Ensure the [Node Package manager](https://nodejs.org/en/) is installed.

- Install the Azure Bot Service command line (CLI) tools. It's important to do this even if you've used the tools before to ensure you have the latest versions.

```shell
npm i -g ludown luis-apis qnamaker botdispatch msbot luisgen chatdown
```

> Internal Only
> npm config set registry https://botbuilder.myget.org/F/botbuilder-tools-daily/npm/
> npm install -g ludown@1.0.36-12 luis-apis@2.0.11-2.5.12 luisgen@1.0.2-12 qnamaker@1.0.33-12 msbot@2.0.4-16

- Install the Azure Command Line Tools (CLI) from [here](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)

- Install the AZ Extension for Bot Service
```shell
az extension add -n botservice
```

## Clone the Repo

## Configuration

1. Retrieve your LUIS Authoring Key
   - Go to https://www.luis.ai and signin.
   - Once signed in click on your name in the top right hand corner.
   - Choose Settings and make a note of the Authoring Key for the next step.

## Deployment

>If you have multiple Azure subscriptions and want to ensure the deployment selects the correct one, run the following commands before continuing.

 Follow the browser login process into your Azure Account
```shell
az login
az account list
az account set --subscription "YOUR_SUBSCRIPTION_NAME"
```

Then to deploy your Custom Assistant including all dependencies - e.g. CosmosDb, Application Insights, etc. run the following command from a command prompt within your project folder. Ensure you update the authoring key from the previous step and choose the Azure datacenter location you wish to use.

```shell
msbot clone services --name "MyCustomAssistant" --luisAuthoringKey "YOUR_AUTHORING_KEY" --folder "DeploymentScripts\msbotClone" --location "westus" --verbose
```

## Testing

See the [Testing](./customassistant-testing.md) section for information on how to test your Custom Assistant.

> Note that the Deployment will deploy your Custom Assistant but will not configure Skills. These are an optional step which are documented [here](./customassistant-addingskills.md).