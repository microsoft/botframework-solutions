---
category: Tutorials
subcategory: Create a Virtual Assistant
language: C#
title: Create your assistant
order: 3
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Create your Virtual Assistant project

1. In Visual Studio, replace **File > New Project**.
2. Under Bot, select **Virtual Assistant Template**.
3. Name your project and select **Create**.
4. Build your project to restore the NuGet packages.

## What files were created?
    | - Adapters                           // BotAdapter implementations for configuring Middleware
        | - DefaultAdapter.cs                  // Configures basic middleware
        | - DefaultWebSocketAdapter.cs         // Configures middleware for web socket connection
    | - Bots                               // IBot implementations for initializing dialog stack
        | - DialogBot.cs                       // Initializes the dialog stack with a primary dialog (e.g. MainDialog)
    | - Content                            // Static content used by the assistant including images and Adaptive Cards
        | - NewUserGreeting.json               // Adaptive Card shown to first time users
        | - ReturningUserGreeting.json         // Adaptive Card shown to returning users
    | - Controllers                        // API Controllers
        | - BotController.cs                   // API Controller for api/messages endpoint
    | - Deployment                         // Files for deployment and provisioning
        | - Resources                          // Resources for deployment and provisioning. May be excluded from source control.
            | - LU                                 // Files for deploying LUIS language models
                | - General.lu                         // General language model (e.g. Cancel, Help, Escalate, etc.)
            | - QnA                                // Files for deploying QnA Maker knowledgebases
                | - Chitchat.lu                        // Chitchat knowledgebase (e.g. Hi, How are you?, What's your name?, 
                | - Faq.lu                             // FAQ knowledgebase
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
        | - OnboardingDialog.cs                // Dialog for collecting basic profile information from user
        | - CancelDialog.cs                    // Dialog for confirming cancellation intent
        | - EscalateDialog.cs                  // Dialog for handling user escalation
    | - Models                             // Data models
        | - OnboardingState.cs                 // Model for basic profile information
    | - Pipeline                           // Files for setting up an deployment pipeline in Azure DevOps
        | - Assistant.yml                      // Build pipeline template for Azure DevOps
    | - Responses                          // Classes and files for representing bot responses
        | - Cancel                             // Cancel responses                              
            | - CancelResponses.cs                 // Cancel dialog response manager
            | - CancelString.resx                  // Cancel dialog strings
        | - Escalate                           // Escalate responses   
            | - EscalateResponses.cs               // Escalate dialog response manager
            | - EscalateString.resx                // Escalate dialog strings
        | - Main                               // Main responses   
            | - MainResponses.cs                   // Main dialog response manager
            | - MainString.resx                    // Main dialog strings
        | - Onboarding                         // Onboarding responses   
            | - OnboardingResponses.cs             // Onboarding dialog response manager
            | - OnboardingString.resx              // Onboarding dialog strings
    | - Services                           // Configuration for connected services and service clients
        | - BotServices.cs                     // Class representation of service clients and recognizers
        | - BotSettings.cs                     // Class representation of configuration files
        | - DispatchLuis.cs                    // Class representation of LUIS result from Dispatch language model
        | - GeneralLuis.cs                     // Class representation of LUIS result from General language model
    | - appsettings.json                   // Configuration for application and Azure services
    | - cognitivemodels.json               // Configuration for language models, knowledgebases, and dispatch model
    | - skills.json                        // Configuration for connected skills
    | - Program.cs                         // Default Program.cs file
    | - Startup.cs                         // Initializes dependencies

