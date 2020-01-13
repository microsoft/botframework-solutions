---
layout: tutorial
category: Virtual Assistant
subcategory: Create
language: csharp
title: Create your assistant
order: 3
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Create your Virtual Assistant project

1. In Visual Studio, select **File > New Project**.
2. Search for **Virtual Assistant Template** and select **Next**.
3. Name your project and select **Create**.
4. Build your project to restore the NuGet packages.

## What files were created?
    | - Adapters                            // BotAdapter implementations for configuring Middleware
        | - DefaultAdapter.cs               // Configures basic middleware
    | - Bots                                // IBot implementations for initializing dialog stack
        | - DefaultActivityHandler.cs       // Initializes the dialog stack with a primary dialog (e.g. MainDialog)
    | - Controllers                         // API Controllers
        | - BotController.cs                // API Controller for api/messages endpoint
    | - Deployment                          // Files for deployment and provisioning
        | - Resources                       // Resources for deployment and provisioning.
            | - LU                          // Files for deploying LUIS language models
                | - General.lu              // General language model (e.g. Cancel, Help, Escalate, etc.)
            | - QnA                         // Files for deploying QnA Maker knowledgebases
                | - Chitchat.lu             // Chitchat knowledgebase (e.g. Hi, How are you?, What's your name?, 
                | - Faq.lu                  // FAQ knowledgebase
            | - template.json               // ARM Deployment template
            | - parameters.template.json    // ARM Deployment parameters file
        | - Scripts                         // PowerShell scripts for deployment and provisioning
            | - deploy.ps1                  // Deploys and provisions Azure resources and cognitive models
            | - deploy_cognitive_models.ps1 // Deploys and provisions cognitive models only
            | - update_cognitive_models.ps1 // Updates existing cognitive models
            | - luis_functions.ps1          // Functions used for deploying and updating LUIS models
            | - qna_functions.ps1           // Functions used for deploying and updating QnA Maker knowledgebases
            | - publish.ps1                 // Script to publish your Bot to Azure.
    | - Dialogs                             // Bot Framework Dialogs
        | - MainDialog.cs                   // Dialog for routing incoming messages
        | - OnboardingDialog.cs             // Dialog for collecting basic profile information from user
    | - Models                              // Data models
        | - UserProfileState.cs             // Model for basic profile information
    | - Pipeline                            // Files for setting up an deployment pipeline in Azure DevOps
        | - Assistant.yml                   // Build pipeline template for Azure DevOps
    | - Responses                           // Classes and files for representing bot responses
        | - MainResponses.lg                // Language generation templates for Main Dialog repsonses
        | - OnboardingResponses.lg          // Language generation templates for Onboarding Dialog repsonses 
    | - Services                            // Configuration for connected services and service clients
        | - BotServices.cs                  // Class representation of service clients and recognizers
        | - BotSettings.cs                  // Class representation of configuration files
        | - DispatchLuis.cs                 // Class representation of LUIS result from Dispatch language model
        | - GeneralLuis.cs                  // Class representation of LUIS result from General language model
    | - appsettings.json                    // Configuration for application and Azure services
    | - cognitivemodels.json                // Configuration for language models, knowledgebases, and dispatch model
    | - skills.json                         // Configuration for connected skills
    | - Program.cs                          // Default Program.cs file
    | - Startup.cs                          // Initializes dependencies

