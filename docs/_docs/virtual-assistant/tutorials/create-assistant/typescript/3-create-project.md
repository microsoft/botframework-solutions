---
layout: tutorial
category: Virtual Assistant
subcategory: Create
language: typescript
title: Create your assistant
order: 3
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Create your Virtual Assistant project

1. Execute the Virtual Assistant generator with this command.

```bash
yo bot-virtualassistant
```

**At this point you can proceed with two different options:**

### Generate the assistant using prompts

- The generator will start prompting for some information that is needed for generating the sample:
  - `What's the name of your assistant? (sample-assistant)`
        > The name of your assistant (also used as your project's name and for the root folder's name).
    - `What's the description of your assistant? ()`
        > The description of your assistant.
    - `Which languages will your assistant use? (by default takes all the languages)`
      - [x] Chinese (`zh-cn`)
      - [x] Deutsch (`de-de`)
      - [x] English (`en-us`)
      - [x] French (`fr-fr`)
      - [x] Italian (`it-it`)
      - [x] Spanish (`es-es`)
    - `Do you want to change the new assistant's location?`
        > A confirmation to change the destination for the generation.
    - `Where do you want to generate the assistant? (By default, it takes the path where you are running the generator)`
      > The destination path for the generation.
    - `Looking good. Shall I go ahead and create your new assistant?`
        > Final confirmation for creating the desired assistant.

### Generate the sample using CLI parameters

| Option                            | Description                                                                                                  |
|-----------------------------------|--------------------------------------------------------------------------------------------------------------|
| -n, --assistantName <name>              | The name you want to give to yor assistant (by default takes `sample-assistant`)                                                          |
| -d, --assistantDesc <description>       | A brief bit of text used to describe what your assistant does (by default is empty) |
| -l, --assistantLang <array of languages>| The languages you want to use with your assistant. Possible values are `de-de`, `en-us`, `es-es`, `fr-fr`, `it-it`, `zh-zh` (by default takes all the languages)|
| -p, --assistantGenerationPath <path>    | The path where the assistant will be generated (by default takes the path where you are running the generator)            |
| --noPrompt                        | Do not prompt for any information or confirmation                                                                               |

**NOTE:** If you don't use the _--noPrompt_ option, the process will keep prompting, but will still be using the input values by default.

#### Example

```bash
> yo bot-virtualassistant -n "Virtual Assistant" -d "A description for my new assistant" -l "en-us,es-es" -p "\aPath" --noPrompt
```

After this, you can check the summary on your screen:

```bash
- Name: <aName>
- Description: <aDescription>
- Selected languages: <languages>
- Path: <aPath>
```

>**WARNING:** The process will fail if it finds another folder with the same name of the new assistant.

>**NOTE:** Remember to have an **unique** assistant's name for deployment steps.

## What files were created?
    | - deployment                              // Files for deployment and provisioning
        | - resources                           // Resources for deployment and provisioning.
            | - LU                              // Files for deploying LUIS language models
                | - general.lu                  // General language model (e.g. Cancel, Help, Escalate, etc.)
            | - QnA                             // Files for deploying QnA Maker knowledgebases
                | - chitchat.qna                // Chitchat knowledgebase (e.g. Hi, How are you?, What's your name?, 
                | - faq.qna                     // FAQ knowledgebase
            | - template.json                   // ARM Deployment template
            | - parameters.template.json        // ARM Deployment parameters file
        | - scripts                             // PowerShell scripts for deployment and provisioning
            | - deploy.ps1                      // Deploys and provisions Azure resources and cognitive models
            | - deploy_cognitive_models.ps1     // Deploys and provisions cognitive models only
            | - update_cognitive_models.ps1     // Updates existing cognitive models
            | - luis_functions.ps1              // Functions used for deploying and updating LUIS models
            | - qna_functions.ps1               // Functions used for deploying and updating QnA Maker knowledgebases
            | - publish.ps1                     // Script to publish your Bot to Azure.
    | - pipeline                                // Files for setting up an deployment pipeline in Azure DevOps
        | - sample-assistant.yml                // Sample build pipeline template for Azure DevOps
    | - src                                     // Folder which contains all the Virtual Assistant code before compilation
        | - adapters                            // BotAdapter implementations for configuring Middleware
            | - defaultAdapter.ts               // Configures basic middleware
        | - authentication                      // Classes for configuring skill authentication
            | - allowedCallersClaimsValidator   // Class for managing allowed skill authentication claims
        | - bots                                // IBot implementations for initializing dialog stack
            | - defaultActivityHandler.ts       // Initializes the dialog stack with a primary dialog (e.g. mainDialog)
        | - dialogs                             // Bot Framework Dialogs
            | - mainDialog.ts                   // Dialog for routing incoming messages
            | - onboardingDialog.ts             // Dialog for collecting basic profile information from user
        | - models                              // Data models
            | - stateProperties.ts              // Constants for state property keys
            | - userProfileState.ts             // Model for basic profile information
        | - responses                           // Classes and files for representing bot responses
            | - AllResponses.lg                 // Combined language generation templates
            | - MainResponses.lg                // Language generation templates for Main Dialog responses
            | - OnboardingResponses.lg          // Language generation templates for Onboarding Dialog responses 
        | - services                            // Configuration for connected services and service clients
            | - botServices.ts                  // Class representation of service clients and recognizers
            | - botSettings.ts                  // Class representation of configuration files
        | - appsettings.json                    // Configuration for application and Azure services
        | - cognitivemodels.json                // Configuration for language models, knowledgebases, and dispatch model
        | - index.ts                            // Initializes dependencies

You now have your own Virtual Assistant! Before trying to run your assistant locally, continue with the deployment steps (it creates vital dependencies required to run correctly).