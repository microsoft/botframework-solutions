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
1. Search for **Virtual Assistant Template** and select **Next**.
1. Name your project and select **Create**.
1. Build your project to restore the NuGet packages.

## What files were created?
    | - Adapters                             // BotAdapter implementations for configuring Middleware
        | - DefaultAdapter.cs                // Configures basic middleware
    | - Authentication                       // Classes for configuring skill authentication
        | - AllowedCallersClaimsValidator.cs // Class for managing allowed skill authentication claims
    | - Bots                                 // IBot implementations for initializing dialog stack
        | - DefaultActivityHandler.cs        // Initializes the dialog stack with a primary dialog (e.g. MainDialog)
    | - Controllers                          // API Controllers
        | - BotController.cs                 // API Controller for api/messages endpoint
        | - SkillController.cs               // API Controller for api/skills endpoint. Skills will call into this endpoint after processing
    | - Deployment                           // Files for deployment and provisioning
        | - Resources                        // Resources for deployment and provisioning
            | - LU                           // Files for deploying LUIS language models
                | - General.lu               // General language model (e.g. Cancel, Help, Escalate, etc.)
            | - QnA                          // Files for deploying QnA Maker knowledgebases
                | - Chitchat.qna             // Chitchat knowledgebase (e.g. Hi, How are you?, What's your name?, etc.)
                | - Faq.qna                  // FAQ knowledgebase
            | - template.json                // ARM Deployment template
            | - parameters.template.json     // ARM Deployment parameters file
        | - Scripts                          // PowerShell scripts for deployment and provisioning
            | - deploy.ps1                   // Deploys and provisions Azure resources and cognitive models
            | - deploy_cognitive_models.ps1  // Deploys and provisions cognitive models only
            | - update_cognitive_models.ps1  // Updates existing cognitive models
            | - luis_functions.ps1           // Functions used for deploying and updating LUIS models
            | - qna_functions.ps1            // Functions used for deploying and updating QnA Maker knowledgebases
            | - publish.ps1                  // Script to publish your Bot to Azure
    | - Dialogs                              // Bot Framework Dialogs
        | - MainDialog.cs                    // Dialog for routing incoming messages
        | - OnboardingDialog.cs              // Dialog for collecting basic profile information from user
    | - Models                               // Data models
        | - StateProperties.cs               // Constants for state property keys
        | - UserProfileState.cs              // Model for basic profile information
    | - Pipeline                             // Files for setting up an deployment pipeline in Azure DevOps
        | - Assistant.yml                    // Sample build pipeline template for Azure DevOps
    | - Responses                            // Classes and files for representing bot responses
        | - AllResponses.lg                  // Combined language generation templates
        | - MainResponses.lg                 // Language generation templates for Main Dialog responses
        | - OnboardingResponses.lg           // Language generation templates for Onboarding Dialog responses 
    | - Services                             // Configuration for connected services and service clients
        | - BotServices.cs                   // Class representation of service clients and recognizers
        | - BotSettings.cs                   // Class representation of configuration files
        | - DispatchLuis.cs                  // Class representation of LUIS result from Dispatch language model
        | - GeneralLuis.cs                   // Class representation of LUIS result from General language model
    | - TokenExchange                        // Classes for managing authentication configuration between assistant bot and connected skills
        | - ITokenExchangeConfig.cs          // Interface representing an authentication configuration
        | - TokenExchangeConfig.cs           // Implementation representing an authentication configuration
        | - TokenExchangeSkillHandler.cs     // Handler for managing single sign-on between assistant bot and configured skills
    | - appsettings.json                     // Configuration for application and Azure services
    | - cognitivemodels.json                 // Configuration for language models, knowledgebases, and dispatch model
    | - Program.cs                           // Default Program.cs file
    | - Startup.cs                           // Initializes dependencies

