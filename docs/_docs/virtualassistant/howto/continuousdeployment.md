---
category: Virtual Assistant
subcategory: How-to
title: Create an Azure DevOps release pipeline
description: Guidance on how to create and configure a release pipeline for your Virtual Assistant
order: 1
---

# {{ page.title }}
{:.no_toc}

## In this how-to
{:.no_toc}

* 
{:toc}
	
## Prerequisites
- To have a better knowledge you can read the documentation of Continuous integration about the YAML build Pipelines.
- Set up an Azure DevOps account.
- Have a deployed bot.
- Update the YAML file to generate an Artifact.

## Scenario

Create a Release Pipeline in Azure DevOps.

## Introduction

In the first place, we need to have in mind a few modifications before starting to work on the Release Pipeline configuration. We'll generate an artifact, but what is an artifact? It’s a compressed version of the project or solution which contains all the necessary information to create the base of the Release Pipeline configuration.

## Create a Release Pipeline

Go to the release section in your DevOps organization and select in the plus icon that will show the following options. In this case, we select create a New release pipeline.

![Create Release Pipeline 1]({{site.baseurl}}/assets/images/create_release_pipeline_1.png)

Select the option add an artifact and that will show the configuration of an artifact and the different options to manage the release pipeline configuration, like select the project, name and what published artifact do you want to use. 
As necessary step you need the build pipeline to generate an artifact configuration. We can only select one after the build pipeline execution process has finished.
	
![Create Release Pipeline 2]({{site.baseurl}}/assets/images/create_release_pipeline_2.png)

Now you have an artifact configuration, you can start to configure the stage section.
The Stage will contain the Agent Job with all the Pipeline tasks, you can select different task configuration in your stage or you can start with an empty project and configurate your release pipeline step by step.

![Create Release Pipeline 3]({{site.baseurl}}/assets/images/create_release_pipeline_3.png)

In this case to verify the functionality of release configuration, we added an example task to check if the release configuration works without problems and verify the artifact configuration was compressed successfully in the build pipeline with the project information.

![Create Release Pipeline 4]({{site.baseurl}}/assets/images/create_release_pipeline_4.png)

Select the option Create new release and will appear the following information, check that is the correct stage and artifact before click create button.

![Create Release Pipeline 5]({{site.baseurl}}/assets/images/create_release_pipeline_5.png)

After the release was executed you can check the log of each tasks added to the Agent Job. You can check the stage configuration and see the log information. Here is the state of each tasks and check if everything is okay or the tasks need a change.

![Create Release Pipeline 6]({{site.baseurl}}/assets/images/create_release_pipeline_6.png)

## Configure the Release Pipeline to update the Bot Services

1. As first step, to have a more clear Agent Job and tasks add the variable section, the Pipelines Variables that you will use in the Release configuration. The variables with the highlight are from the Az Login and the others are about the cognitivemodels.json file.

    ![Configure Release Pipeline 1]({{site.baseurl}}/assets/images/configure_release_pipeline_1.png)

2. You need to create a task with the Az account data. We need to complete with our information like Tenant, ID, Password.
    ```
    az login --user $(botUsername) --password $(botPassword) --tenant $(botTenant)
    ```

    ![Configure Release Pipeline 2]({{site.baseurl}}/assets/images/configure_release_pipeline_2.png)

3. Add a task that contains the commands necessaries to have the environment ready to update the Bot Services, which needs to execute the following command:
    ```
    npm install -g botdispatch ludown luis-apis qnamaker luisgen@2.0.2
    ```

    ![Configure Release Pipeline 3]({{site.baseurl}}/assets/images/configure_release_pipeline_3.png)

4. To complete this step, it's a pre-requisite have a deployed bot to execute this Release successfully, because the task needs to check if the resource group exists before run without problems the update cognitive models task.
    ```
    Get-AzureRmResourceGroup -Name $(ResourceGroup) -ErrorVariable notPresent -ErrorAction SilentlyContinue

    if ($notPresent)
    {
        Write-Host "ResourceGroup $(ResourceGroup) doesn't exist"
    }
    else
    {
        Write-Host "ResourceGroup $(ResourceGroup) exists."
    }
    ```

    ![Configure Release Pipeline 4]({{site.baseurl}}/assets/images/configure_release_pipeline_4.png)

5. In this step to avoid the upload of sensitive keys on a public repository, we used variables groups on the Update cognitivemodels.json task. This is the form to replace the configuration of the cognitivemodels.json that is stored in the artifact configuration. VATest is the example name of the folder that the artifact is located.
    ```
    Set-Content -Path "$(System.DefaultWorkingDirectory)/$(Release.PrimaryArtifactSourceAlias)/drop/VATest/cognitivemodels.json" -Value '{
        "cognitiveModels": {
        "en": {
            "dispatchModel": {
            "subscriptionkey": "$(SubscriptionKey)",
            "type": "dispatch",
            "region": "$(Region)",
            "authoringRegion": "$(AuthoringRegion)",
            "appid": "$(AppId)",
            "authoringkey": "$(LuisAuthoringKey)",
            "name": "$(BotName)_Dispatch"
            },
            "languageModels": [
            {
                "region": "$(Region)",
                "id": "General",
                "name": "$(botName)_General",
                "authoringRegion": "$(AuthoringRegion)",
                "authoringkey": "$(LuisAuthoringKey)",
                "appid": "$(AppIdLanguageModels)",
                "subscriptionkey": "$(SubscriptionKey)",
                "version": "0.1"
            }
            ],
            "knowledgebases": [
            {
                "kbId": "$(KbIdChitchat)",
                "id": "Chitchat",
                "subscriptionKey": "$(SubscriptionKeyKb)",
                "hostname": "$(hostname)",
                "endpointKey": "$(endpointKey)",
                "name": "Chitchat"
            },
            {
                "kbId": "$(KbIdFaq)",
                "id": "Faq",
                "subscriptionKey": "$(SubscriptionKeyKb)",
                "hostname": "$(hostname)",
                "endpointKey": "$(endpointKey)",
                "name": "Faq"
            }
            ]
        }
        },
        "defaultLocale": "en-us"
    }' | ConvertFrom-Json
    ```

    ![Configure Release Pipeline 5]({{site.baseurl}}/assets/images/configure_release_pipeline_5.png)

6. In the last step we run the script to update the Bot Services (LUIS, Qna, Dispatch).
    ```
    pwsh.exe -ExecutionPolicy Bypass -File $(System.DefaultWorkingDirectory)/csharp-Virtual-Assistant-Sample/drop/VATest/Deployment/Scripts/update_cognitive_models.ps1
    ```

    ![Configure Release Pipeline 6]({{site.baseurl}}/assets/images/configure_release_pipeline_6.png)
