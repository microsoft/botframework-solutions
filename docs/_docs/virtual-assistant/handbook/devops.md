---
category: Virtual Assistant
subcategory: Handbook
title: DevOps
description: Create an Azure DevOps release pipeline
order: 7
---

# {{ page.title }}
{:.no_toc}

## In this topic
{:.no_toc}

* 
{:toc}
	
## Prerequisites
- To have a better knowledge you can read the documentation of Continuous integration about the YAML build Pipelines.
- Set up an Azure DevOps account.
- Have a deployed bot.
- Update the YAML file to generate an Artifact.

## Create an Azure DevOps release pipeline

### Introduction
{:.no_toc}

In the first place, we need to have in mind a few modifications before starting to work on the Release Pipeline configuration. We'll generate an artifact, but what is an artifact? It’s a compressed version of the project or solution which contains all the necessary information to create the base of the Release Pipeline configuration.

### Create a Release Pipeline
{:.no_toc}

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

### Configure the Release Pipeline to update the Bot Services
{:.no_toc}

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



    ### Intro

When trying to develop language models in a distributed team, managing conflicts can be difficult. Refer to the following guidance for some common scenarios when managing cognitive models for a team.

## Manage cognitive models across environments

### I want to protect my production environment against conflicting changes made by multiple editors.
{:.no_toc}

It is recommended that for project being worked on by multiple developers that you protect your production cognitive models by only deploying changes through a build pipeline. This pipeline should run the various scripts/commands needed to update your LUIS models, QnA Maker knowledgebases, and Dispatch model automatically based on your source control. Individual developers should make their changes in their own versions of the models, and push their changes in to source control when they are ready to merge.

![]({{site.baseurl}}/assets/images/model_management_flow.png)

### I want to test changes to my LUIS models and QnA Maker knowledgebases in the portal.
{:.no_toc}

When you want to test changes to your LUIS models and QnA Maker knowledgebases in the portal, it is recommended that you deploy your own personal versions to develop with, and do not make changes directly in the production apps to prevent conflicts with the other developers. After you have made all the changes you want in the portal, follow these steps to share your changes with your team:

1. Run the following command from your project folder:

    ```
    .\Deployment\Scripts\update_cognitive_models.ps1 -RemoteToLocal
    ```

    > This script downloads your modified LUIS models in the .lu schema so it can be published to production by your build pipeline. If you are running this script from a Virtual Assistant project, it also runs `dispatch refresh` and `luisgen` to update your Dispatch model and DispatchLuis.cs files.

2. Check in your updated .lu files to source control. 
    > Your changes should go through a peer review to validate there will be no conflicts. You can also share your LUIS app and/or transcripts of the bot conversation with your changes to help in this conversation.

3. Run your build pipeline to deploy your updated files to your production environment. 
    > This pipeline should update your LUIS models, QnA Maker knowledgebases, and Dispatch model as needed.


### I've changed my skill LUIS model. What next?
{:.no_toc}

If you have added or removed an intent from your skill LUIS model, follow these steps to update your skill manifest:

1. Open the manifestTemplate.json file.
2. If you have added new intents, either add them to an existing `action` or add a new action for the intent like this:

    ```json
    "actions": [
        {
        "id": "toDoSkill_addToDo",
        "definition": {
            "description": "Add a task",
            "slots": [],
            "triggers": {
                "utteranceSources": [
                    {
                        "locale": "en",
                        "source": [ "todo#AddToDo" ]
                    }
                ]
            }
        }
    },
    ```

Once you have updated your manifest, follow these steps to update any Virtual Assistants that are using your skill:

1. Run the following command from your project directory:

    ```
    botskills update --cs
    ```

    > This command updates your skills.json file with the latest manifest definitions for each connected skill, and runs dispatch refresh to update your dispatch model.
