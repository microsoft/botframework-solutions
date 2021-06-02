---
layout: tutorial
category: Skills
subcategory: Create
language: csharp
title: Create your skill project
order: 3
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

1. In Visual Studio, select **File > New Project**.
1. Search for **Skill Template** and select **Next**.
1. Name your project and select **Create**.
1. Build your project to restore the NuGet packages.

## What files were created?
    | - Adapters                            // BotAdapter implementations for configuring Middleware
        | - DefaultAdapter.cs               // Configures basic middleware
    | - Bots                                // IBot implementations for initializing dialog stack
        | - DefaultActivityHandler.cs       // Initializes the dialog stack with a primary dialog (e.g. MainDialog)
    | - Controllers                         // API Controllers
        | - BotController.cs                // API Controller for api/messages endpoint
    | - Deployment                          // Files for deployment and provisioning
        | - Resources                       // Resources for deployment and provisioning
            | - LU                          // Files for deploying LUIS language models
                | - General.lu              // General language model (e.g. Cancel, Help, Escalate, etc.)
                | - Skill.lu                // Sample language model for your skill
            | - template.json               // ARM Deployment template
            | - parameters.template.json    // ARM Deployment parameters file
        | - Scripts                         // PowerShell scripts for deployment and provisioning
            | - deploy.ps1                  // Deploys and provisions Azure resources and cognitive models
            | - deploy_cognitive_models.ps1 // Deploys and provisions cognitive models only
            | - update_cognitive_models.ps1 // Updates existing cognitive models
            | - luis_functions.ps1          // Functions used for deploying and updating LUIS models
            | - qna_functions.ps1           // Functions used for deploying and updating QnA Maker knowledgebases
            | - publish.ps1                 // Script to publish your Bot to Azure
    | - Dialogs                             // Bot Framework Dialogs
        | - MainDialog.cs                   // Dialog for routing incoming messages
        | - SampleAction.cs                 // Sample action which prompts user for name
        | - SampleDialog.cs                 // Sample dialog which prompts user for name
        | - SkillDialogBase.cs              // Dialog base class for shared steps and config
    | - Models                              // Data models
        | - SkillState.cs                   // Model for storing skill state
        | - StateProperties.cs              // Constants for state property keys
    | - Pipeline                            // Files for setting up an deployment pipeline in Azure DevOps
        | - Skill.yml                       // Sample build pipeline template for Azure DevOps
    | - Responses                           // Classes and files for representing bot responses
        | - AllResponses.lg                 // Combined language generation templates
        | - MainResponses.lg                // Language generation templates for Main Dialog responses
        | - SampleResponses.lg              // Language generation templates for Sample Dialog responses
    | - Services                            // Configuration for connected services and service clients
        | - BotServices.cs                  // Class representation of service clients and recognizers
        | - BotSettings.cs                  // Class representation of configuration files
        | - GeneralLuis.cs                  // Class representation of LUIS result from General language model
        | - SkillLuis.cs                    // Class representation of LUIS result from Skill language model
    | - appsettings.json                    // Configuration for application and Azure services
    | - cognitivemodels.json                // Configuration for language models, knowledgebases, and dispatch model
    | - Program.cs                          // Default Program.cs file
    | - Startup.cs                          // Initializes dependencies