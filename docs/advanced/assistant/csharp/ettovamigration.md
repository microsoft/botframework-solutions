# Migrating from an Enterprise Template based Bot to the Virtual Assistant Template

## Table of Contents
- [Migrating from an Enterprise Template based Bot to the Virtual Assistant Template](#migrating-from-an-enterprise-template-based-bot-to-the-virtual-assistant-template)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Key Changes](#key-changes)
    - [ASP.NET MVC Pattern](#aspnet-mvc-pattern)
    - [Bot File deprecated](#bot-file-deprecated)
    - [Folder Structure](#folder-structure)
    - [Solutions Nuget package](#solutions-nuget-package)
    - [ARM Deployment](#arm-deployment)
  - [Steps](#steps)
    - [Create a new project](#create-a-new-project)
    - [Deployment](#deployment)
    - [Migrate dialogs](#migrate-dialogs)
    - [Responses](#responses)
    - [Adaptive Cards](#adaptive-cards)
    - [State](#state)
    - [LUISGen files](#luisgen-files)

## Overview

Creating a new Bot through the [Virtual Assistant](/docs/virtual-assistant/README.md) is the easiest way to get started with creating a new Assistant. If you have an existing Enterprise Template based Bot, the recommended approach would be to create a new project from the Virtual Assistant template and bring across your customisation dialogs to get started quickly.

The core of both templates is to accelerate creation of your own experience through provision of all the scaffolding required to build a high quality conversational experience. This then enables you to focus on your own dialogs and customisation, the changes we've made to the template are largely in the scaffolding area which should be well separated from your customisation therefore creating a new project is our recommend approach.

## Key Changes

### ASP.NET MVC Pattern

The Virtual Assistant template has adopted the ASP.NET Core MVC approach which has enabled us to further simplify the template code and be more familiar to .NET developers. This has resulted in significant changes to how the Bot is configured and initialised through deeper use of [Dependency Injection (DI)](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.2) which improve extensibility and the ability to automate testing.

### Bot File deprecated

Prior to the Bot Framework SDK 4.3 release, the Bot Framework offered the .bot file as a mechanism to manage resources. However, going forward we recommend that you use `appsettings.json` (C#) or `.env` (Typescript) file for managing these resources. 

In-line with this change to .bot files we have migrated the template configuration across to appSettings.json for general dependencies and cognitiveModels.json to represent the Dispatch, LUIS and QnA models registered for your assistant.

This also enables you to leverage standard approaches such as KeyVault.

### Folder Structure

We have flattened the directory structure, primarily around the Dialogs folders which had a hierarchy enabling Dialogs to have their own resources, responses and state. Through our work building Skills and working with customers/partners it became clear this structure didn't scale and became complex.

The core folder structure is shown below and key concepts such as Dialogs, Models and Responses are grouped together at the root level and aligned with the ASP.NET MVC standards.

```
- YourAssistant
    - Adapters
    - Bots
    - Content
    - Controllers
    - Deployment
    - Dialogs
    - Models
    - Responses
    - Services
    - appSettings.json
    - cognitiveModels.json
    - skills.json
    - startup.cs
```

### Solutions Nuget package

The previous Enterprise Template had a Solutions library which contained extensions to the Bot Framework to simplify creation of advanced experiences. This is now published as an additional Nuget library enabling us to easily make updates which you can pull into your project and avoiding have to perform differential comparison with our sample Enterprise Template project.

### ARM Deployment

Previously we made use of the `msbot` command line tool to automate deployment of dependent resources in Azure. This enabled us to address limitations around automated Azure deployment for some resources and ensure developers had an easy way to get started.

With these limitations addressed we have now moved to a ARM template based approach providing you the same automated approach but also providing a more familiar way to customise deployment to suit your requirements.

## Steps

### Create a new project

Create a new project using the Virtual Assistant template following the instructions [here](gettingstarted.md#create-a-new-project).

### Deployment

It's recommended to deploy your new Virtual Assistant template using the [updated deployment approach](../common/deploymentsteps.md) which now support the ability for multi-locale conversational experiences and the new configuration files which replace the .bot file. This enables you to get started right away with no manual changes.

Alternatively if you wish to re-use existing deployed resources, you can alternatively take your existing .bot file, [decrypt the secrets](https://docs.microsoft.com/en-us/azure/bot-service/bot-file-basics?view=azure-bot-service-4.0&tabs=csharp) and manually move across existing Azure resource information into your new `appSettings.json` and `cognitiveModels.json` files.

### Migrate dialogs

- Copy your custom Dialog code files into the `Dialogs` folder of your new project
  
- Within your `Startup.cs` file add a Transient Service for each of your Dialogs
     ```csharp
    // Register dialogs
    services.AddTransient<AuthenticationDialog>();
    services.AddTransient<CancelDialog>();
    services.AddTransient<EscalateDialog>();
    services.AddTransient<MainDialog>();
    services.AddTransient<OnboardingDialog>();

    services.AddTransient<YOURDIALOG>();
     ``` 

- Add each of these Dialogs into your MainDialog constructor (DI) and call AddDialog for each one to register).

    ```csharp
    public MainDialog(
        BotSettings settings,
        BotServices services,
        OnboardingDialog onboardingDialog,
        EscalateDialog escalateDialog,
        // Your new Dialog
        YourDialog yourDialog,
        List<SkillDialog> skillDialogs,
        IBotTelemetryClient telemetryClient)
        : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            TelemetryClient = telemetryClient;

            AddDialog(onboardingDialog);
            AddDialog(escalateDialog);
            
            // Your new Dialog
            AddDialog(yourDialog);
        }
    ```

### Responses

- Copy Responses for each Dialog into a sub-folder of the Responses folder. Our approach is to have a sub-folder per dialog.

### Adaptive Cards

- Copy any Adaptive Cards used by your Dialogs into the `Content` folder alongside the example introduction cards we have provided.

### State 

- Copy any State classes you may have created into the `Models` folder.

### LUISGen files

- Copy any LuisGen generated classes into the `Services` folder.



