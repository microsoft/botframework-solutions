---
category: Virtual Assistant
subcategory: Handbook
title: Migration
description: Migrate from the Enterprise template 
order: 13
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

## What happened to the Enterprise Template?

The Enterprise Template, released last year, brought together the required capabilities to provide a solid foundation of the best practices and services necessary to create a high-quality conversational experience. The Virtual Assistant solution was built on top of this template, offering more assistance-focused experiences with skills to supplement it's knowledge base.

Thanks to strong feedback from our customers, we are bringing the two approaches together. These complex, assistant-like conversational experiences are proving critical to digital transformation and customer/employee engagement.

The Enterprise Template is now the [Virtual Assistant Template]({{site.baseurl}}/overview/virtual-assistant-template) and introduces the following capabilities:

- C# template simplified and aligned to ASP.NET MVC pattern with dependency injection
- Typescript generator
- **Microsoft.Bot.Builder.Solutions** NuGet package to enable easy updating of the template core after a project is created
- Works out-of-box with Skills, enabling you to use re-usable conversational capabilities or hand off specific tasks to child Bots within your organization
- [Adaptive Cards](https://adaptivecards.io/) that greet new and returning users
- Native conversational telemetry and Power BI analytics via the Bot Builder SDK
- ARM based automated Azure deployment, including all dependent services

If you have an existing bot based off of the Enterprise Template, we recommend creating a new project from the Virtual Assistant Template and bring your dialogs across to get started quickly.

## Key changes to the template

### ASP.NET MVC Pattern
{:.no_toc}

The Virtual Assistant template has adopted the ASP.NET Core MVC approach which has enabled us to further simplify the template code and be more familiar to .NET developers. This has resulted in significant changes to how the Bot is configured and initialized through deeper use of [Dependency Injection (DI)](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.2) which improve extensibility and the ability to automate testing.

### Bot file deprecation
{:.no_toc}

Prior to the Bot Framework SDK 4.3 release, the Bot Framework offered the .bot file as a mechanism to manage resources. Going forward we recommend that you use **appsettings.json** (C#) or **.env** (Typescript) file for managing these resources.

In-line with this change to .bot files we have migrated the template configuration across to appSettings.json for general dependencies and cognitiveModels.json to represent the Dispatch, LUIS and QnA models registered for your assistant.

This also enables you to leverage standard approaches such as KeyVault.

### Folder structure
{:.no_toc}

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
    - startup.cs
```

### Solutions NuGet package
{:.no_toc}

The previous Enterprise Template had a **Microsoft.Bot.Solutions** library which contained extensions to the Bot Framework to simplify creation of advanced experiences. This is now published as the [**Microsoft.Bot.Builder.Solutions**](https://www.nuget.org/packages/Microsoft.Bot.Builder.Solutions/) is now published as an additional NuGet library enabling us to easily make updates which you can pull into your project and avoiding have to perform differential comparison with our sample Enterprise Template project.

### ARM Deployment
{:.no_toc}

Previously we used the **msbot** command line tool to automate deployment of dependent resources in Azure. This enabled us to address limitations around automated Azure deployment for some resources and ensure developers had an easy way to get started.

With these limitations addressed we have now moved to a ARM template based approach providing you the same automated approach but also providing a more familiar way to customize deployment to suit your requirements.

## How to migrate to the Virtual Assistant template

### Create a new project
{:.no_toc}

[Create a new project]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/csharp/3-create-project) using the Virtual Assistant Template.

### Deployment
{:.no_toc}

It's recommended to deploy your new Virtual Assistant template using the [updated deployment approach]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/csharp/4-provision-your-azure-resources) which now support the ability for multi-locale conversational experiences and the new configuration files which replace the .bot file. This enables you to get started right away with no manual changes.

Alternatively if you wish to re-use existing deployed resources, you can alternatively take your existing .bot file, [decrypt the secrets](https://docs.microsoft.com/en-us/azure/bot-service/bot-file-basics?view=azure-bot-service-4.0&tabs=csharp) and manually move across existing Azure resource information into your new **appSettings.json** and **cognitiveModels.json** files.

### Migrate dialogs
{:.no_toc}

1. Copy your custom dialog class files into the Dialogs directory of your new project.
  
1. Within the **Startup.cs** file, add a Transient Service for each of your dialogs.

     ```csharp
    // Register dialogs
    services.AddTransient<AuthenticationDialog>();
    services.AddTransient<CancelDialog>();
    services.AddTransient<EscalateDialog>();
    services.AddTransient<MainDialog>();
    services.AddTransient<OnboardingDialog>();

    services.AddTransient<YOURDIALOG>();
     ```

1. Add each of these Dialogs into your MainDialog constructor (DI) and call AddDialog for each one to register).

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
{:.no_toc}

Copy Responses for each dialog into a sub-directory of the Responses folder. Focus on having a sub-directory per dialog.

### Adaptive Cards
{:.no_toc}

Copy any Adaptive Cards used by your dialogs into the Content directory with the sample greeting cards.

### State
{:.no_toc}

Copy any state classes you may have created into the Models directory.

### Files generated by the LUISGen tool
{:.no_toc}

Copy any LuisGen-generated classes into the Services directory.

## Extend your assistant with Skills

If your assistant was based on the [Virtual Assistant (Beta Release 0.3) solution](https://github.com/microsoft/botframework-solutions/releases/tag/0.3), continue with [adding back the Skills]({{site.baseurl}}/skills/handbook/add-skills-to-a-virtual-assistant/) 
