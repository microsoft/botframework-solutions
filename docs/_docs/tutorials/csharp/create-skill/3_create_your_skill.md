---
category: Tutorials
subcategory: Create a skill
language: C#
title: Create your skill project
order: 3
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Create your Skill

1. In Visual Studio, click **File > New Project**.
2. Under Bot, select **Skill Template**.
3. Name your project and click **Create**.
4. Build your project to restore your NuGet packages.

You now have your new Skill! Follow the Deployment steps below before you try and run the project as deployment creates key dependencies required for operation.

## What files were created?
```
| - Adapters                           // BotAdapter implementations for configuring Middleware
    | - CustomSkillAdapter.cs               // Configures middleware for skill-mode
    | - DefaultAdapter.cs                   // Configures basic middleware for locale-mode
| - Bots                               // IBot implementations for initializing dialog stack
    | - DialogBot.cs                       // Initializes the dialog stack with a primary dialog (e.g. MainDialog)
| - Controllers                        // API Controllers
    | - BotController.cs                   // API Controller for api/messages and api/skill/messages endpoints
| - Deployment                         // Files for deployment and provisioning
    | - Resources                          // Resources for deployment and provisioning
        | - LU                                 // Files for deploying LUIS language models
            | - General.lu                         // General language model (e.g. Cancel, Help, Escalate, etc.)
            | - Skill.lu                           // Sample language model for your skill
        | - template.json                  // ARM Deployment template
        | - parameters.template.json       // ARM Deployment parameters file
    | - Scripts                        // PowerShell scripts for deployment and provisioning
        | - deploy.ps1                     // Deploys and provisions Azure resources and cognitive models
        | - deploy_cognitive_models.ps1    // Deploys and provisions cognitive models only
        | - update_cognitive_models.ps1    // Updates existing cognitive models
        | - luis_functions.ps1             // Functions used for deploying and updating LUIS models
        | - qna_functions.ps1              // Functions used for deploying and updating QnA Maker knowledgebases
| - Dialogs                            // Bot Framework Dialogs
    | - MainDialog.cs                      // Dialog for routing incoming messages
    | - SampleDialog.cs                    // Sample dialog which prompts user for name
    | - SkillDialogBase.cs                 // Dialog base class for shared steps and config
| - Models                             // Data models
    | - SkillState.cs                      // Model for storing skill state
| - Pipeline                           // Files for setting up an deployment pipeline in Azure DevOps
    | - Skill.yml                          // Build pipeline template for Azure DevOps
| - Responses                          // Classes and files for representing bot responses
    | - Main                                // Main responses                              
        | - MainResponses.tt                    // Main dialog responses text template
        | - MainResponses.json                  // Main dialog responses
    | - Sample                              // Sample responses   
        | - SampleResponses.tt                  // Sample dialog responses text template
        | - SampleResponses.json                // Sample dialog responses
    | - Shared                              // Shared responses   
        | - SharedResponses.tt                  // Shared dialog responses text template
        | - SharedResponses.json                // Shared dialog responses
| - Services                           // Configuration for connected services and service clients
    | - BotServices.cs                     // Class representation of service clients and recognizers
    | - BotSettings.cs                     // Class representation of configuration files
    | - GeneralLuis.cs                     // Class representation of LUIS result from General language model
    | - SkillLuis.cs                       // Class representation of LUIS result from Skill language model
| - appsettings.json                   // Configuration for application and Azure services
| - cognitivemodels.json               // Configuration for language models, knowledgebases, and dispatch model
| - manifestTemplate.json              // Template for generating skill manifest
| - Program.cs                         // Default Program.cs file
| - Startup.cs                         // Initializes dependencies
```