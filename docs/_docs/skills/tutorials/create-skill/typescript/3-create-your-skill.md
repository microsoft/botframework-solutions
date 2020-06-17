---
layout: tutorial
category: Skills
subcategory: Create
language: typescript
title: Create your skill project
order: 3
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

1. Execute the Skill generator with this command.

```bash
yo bot-virtualassistant:skill
```

**At this point you have two different options to proceed:**

### Generate the skill using prompts

- The generator will start prompting for some information that is needed for generating the sample:
  - `What's the name of your skill? (sample-skill)`
    > The name of your skill (used also as your project's name and for the root folder's name).
  - `What's the description of your skill? ()`
    > The description of your skill.
  - `Which languages will your skill use? (by default takes all the languages`
    - [x] Chinese (`zh-cn`)
    - [x] Deutsch (`de-de`)
    - [x] English (`en-us`)
    - [x] French (`fr-fr`)
    - [x] Italian (`it-it`)
    - [x] Spanish (`es-es`)
  - `Do you want to change the new skill's location?`
    > A confirmation to change the destination for the generation.
  - `Where do you want to generate the skill? (by default takes the path where you are running the generator)`
    > The destination path for the generation.
  - `Looking good. Shall I go ahead and create your new skill?`
    > Final confirmation for creating the desired skill.

### Generate the sample using CLI parameters

| Option                            | Description                                                                                                  |
|-----------------------------------|--------------------------------------------------------------------------------------------------------------|
| -n, --skillName <name>              | The name you want to give to your skill (by default takes `sample-skill`)                                                          |
| -d, --skillDesc <description>       | A brief bit of text used to describe what your skill does (by default is empty) |
| -l, --skillLang <array of languages>| The languages you want to use with your skill. Possible values are `de`, `en`, `es`, `fr`, `it`, `zh` (by default takes all the languages)|
| -p, --skillGenerationPath <path>    | The path where the skill will be generated (by default takes the path where you are running the generator)            |
| --noPrompt                        | Do not prompt for any information or confirmation                                                                               |

**NOTE:** If you don't use the _--noPrompt_ option, the process will keep prompting, but using the input values by default.

#### Example

```bash
> yo bot-virtualassistant:skill -n "My skill" -d "A description for my new skill" -l "en-us,es-es" -p "\aPath" --noPrompt
```

After this, you can check the summary in your screen:

```bash
- Name: <aName>
- Description: <aDescription>
- Selected languages: <languages>
- Path: <aPath>
```

>**WARNING:** The process will fail if it finds another folder with the same name of the new skill.

>**NOTE:** Remind to have an **unique** skill's name for deployment steps. 

## What files were created?
```
| - deployment                              // Files for deployment and provisioning
    | - resources                           // Resources for deployment and provisioning.
        | - LU                              // Files for deploying LUIS language models
            | - general.lu                  // General language model (e.g. Cancel, Help, Escalate, etc.)
            | - sample-skill.lu             // Sample language model for your skill
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
    | - sample-skill.yml                    // Sample build pipeline template for Azure DevOps
| - src                                     // Folder which contains all the Skill code before compilation
    | - adapters                            // BotAdapter implementations for configuring Middleware
        | - defaultAdapter.ts               // Configures basic middleware for local mode
    | - bots                                // ActivityHandler implementations for initializing dialog stack
        | - defaultActivityHandler.ts       // Initializes the dialog stack with a primary dialog (e.g. mainDialog)
    | - dialogs                             // Bot Framework Dialogs
        | - mainDialog.ts                   // Dialog for routing incoming messages
        | - sampleDialog.ts                 // Sample dialog which prompts user for name
        | - sampleAction.ts                 // Sample action which prompts user for name
        | - skillDialogBase.ts              // Dialog base class for shared steps and config
    | - manifest                            // Source of manifests
        | - manifest-1.0.json               // Manifest version 1.0
        | - manifest-1.1.json               // Manifest version 1.1
    | - models                              // Data models
        | - skillState.ts                   // Model for storing skill state
        | - stateProperties.ts              // Constants for state property keys
    | - responses                           // Classes and files for representing bot responses
        | - MainResponses.lg                // LG templates for MainDialog 
        | - SampleResponses.lg              // LG templates for SampleDialog 
    | - services                            // Configuration for connected services and service clients
        | - botServices.ts                  // Class representation of service clients and recognizers
        | - botSettings.ts                  // Class representation of configuration files
    | - appsettings.json                    // Configuration for application and Azure services
    | - cognitivemodels.json                // Configuration for language models, knowledgebases, and dispatch model
    | - index.ts                            // Initializes dependencies
```

You now have your own Skill! Follow the Deployment steps below before you try and run the project as deployment creates key dependencies required for operation.
