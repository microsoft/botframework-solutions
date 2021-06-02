---
category: Skills
subcategory: Handbook
title: Experimental - Adding Bot Framework Composer dialogs to a Skill
description: Add dialogs built using Bot Framework Composer to a Skill enabling side by side composition of Waterfall Dialogs and Composer built Adaptive Dialogs.
order: 1
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

## Introduction

The [Bot Framework Composer](https://aka.ms/bfcomposer) is a visual designer that lets you quickly and easily build sophisticated conversational bots without writing code. Composer is currently in Public Preview and the documentation below covers manual steps to move Dialog management for an existing Skill created using the [Skill Template]({{site.baseurl}}/skills/tutorials/create-skill/csharp/1-intro/) to Composer.

For customers that already have existing Bot Framework Virtual Assistant or Custom Skill projects it's important to ensure that Waterfall dialogs and co-exist with Adaptive Dialogs built using Bot Framework Compopser. This documentation covers initial experimental tests to enable you to test hybrid dialog scenarios.

Moving forward there will be an updated Skill Template that will support Bot Framework Composer out of the box without these changes and you can of course use any Composer built dialog as a Skill without using the Skill Template.

> This guidance is experimental and for testing purposes only.

## Pre-Requisites

- An existing Skill created using the Skill Template, follow [this tutorial]({{site.baseurl}}/skills/tutorials/create-skill/csharp/1-intro/) if needed.
- If you have a Skill created using an older version of the Skill Template, ensure it's updated to the 4.9 Bot Framework SDK as per documentation.

## Build your Composer dialogs

The first step is to create a Composer project and create the appropriate LU, Dialog and LG assets for your scenario. Ensure these work as expected using the `Start Bot` and `Test in Emulator` feature of the Bot Framework Emulator, this will also ensure LUIS resources are published.

## Retrieve the Generated Files

1. Within Composer, and your active project. Click the `Export assets to .zip` option under the Export Menu. This self-contained ZIP file contains all of your declarative assets making up your Composer project.

    ![Export Assets to ZIP File]({{site.baseurl}}/assets/images/composer-export-assets-to-zip.png)

1. Unpack this ZIP file into a new sub-folder of your Skill project called `ComposerDialogs`
1. Copy the `Generated Folder` from your Composer Project into the same `ComposerDialogs` folder. (Temporary)

## Add additional Nuget package references

Add the following additional Nuget packages to your project file

```xml
<PackageReference Include="Microsoft.Bot.Builder.Dialogs.Adaptive" Version="4.9.1" />
<PackageReference Include="Microsoft.Bot.Builder.Dialogs.Declarative" Version="4.9.1" />
```    

## Ensure Composer Dialog resources are configured as project content files

Edit your `.csproj` file to add the following lines under an `ItemGroup` section

```xml
<Content Include="**/*.dialog" Exclude="bin/**">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
</Content>
<Content Include="**/*.lg" Exclude="bin/**">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
</Content>
<Content Include="**/*.lu" Exclude="bin/**">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
</Content>    
```

## Update Startup.cs

1. Add the following class variable
    ```csharp
    private IWebHostEnvironment HostingEnvironment { get; set; }
    ```

1. Add the following to your constructor
    ```csharp
    this.HostingEnvironment = env;
    ```

1. In the main `ConfigureServices` handler add the following lines to initialise Declarative dialog support and enumerate the Composer built resources.
    ```csharp
    // Configure Adaptive           
    ComponentRegistration.Add(new DialogsComponentRegistration());
    ComponentRegistration.Add(new AdaptiveComponentRegistration());
    ComponentRegistration.Add(new DeclarativeComponentRegistration());
    ComponentRegistration.Add(new LanguageGenerationComponentRegistration());
    ComponentRegistration.Add(new LuisComponentRegistration());

    // Resource explorer to manage declarative resources for adaptive dialog
    var resourceExplorer = new ResourceExplorer().LoadProject(this.HostingEnvironment.ContentRootPath);
    services.AddSingleton(resourceExplorer);
    ```

1. Ensure any configuration used by the Composer based dialogs is avialable to use through adding this line to the `builder` section of the constructor
    ```csharp
    .AddJsonFile($"ComposerDialogs\\settings\\appsettings.json", optional:true)
    ```

## Update Adapter

Update the `DefaultAdapter.cs` file under the `Adapters` folder as follows:

1. Add the following additional parameters to the constructor
    ```csharp
    IStorage storage,
    UserState userState,
    IConfiguration configuration
    ```

1. Then add the following lines to the constructor
    ```csharp
    this.Use(new RegisterClassMiddleware<IConfiguration>(configuration));
    this.UseStorage(storage);
    this.UseBotState(userState);
    this.UseBotState(conversationState);
    ```

## Update DefaultActivityHandler

We need to make use of `DialogManager` to ensure that the Composer based dialogs execute correctly and also send the appropriate _EndOfConversation_ event once dialogs are complete within the Skill.

1. Declare two new local variables
    ```csharp
    protected readonly DialogManager _dialogManager;
    protected readonly ResourceExplorer _resourceExplorer;
    ```

1. Update the constructor to includes the following lines
    ```csharp
    _resourceExplorer = serviceProvider.GetService<ResourceExplorer>();
    _dialogManager = new DialogManager(dialog);
    _dialogManager.UseResourceExplorer(_resourceExplorer);
    _dialogManager.UseLanguageGeneration();
    ```

1. Update the OnTurnAsync handler to use `_dialogManager` in place of `_dialog`
    ```csharp
    await _dialogManager.OnTurnAsync(turnContext, cancellationToken: cancellationToken);
    ```

## MainDialog

1. Update the constructor to include the following line
    ```csharp
    ResourceExplorer resourceExplorer
    ```

1. Then register **each** top-level Composer Dialog you wish to make available
    ```csharp
    var dialogResource = resourceExplorer.GetResource("todobotwithluissample-0.dialog");
    var composerDialog = resourceExplorer.LoadType<AdaptiveDialog>(dialogResource);

    // Add the dialog
    AddDialog(composerDialog);
    ```

1. Within the appropriate Intent handler within `MainDialog` you can now `begin` the Composer based dialog of your choice by adding the following code:
    ```csharp
    object adaptiveOptions = null;
    return await stepContext.BeginDialogAsync("todobotwithluissample-0.dialog", adaptiveOptions, cancellationToken);
    ```

## LUIS Key

A different LUIS endpoint key is used for your Composer built dialogs but this must be present within the `ComposerDialogs\settings\appSettings.json` file. Add an `endpointKey` entry to the `luis` section of this configuration file, you can find the right key within Composer - Bot Settings.

```json
"luis": {
  "endpointKey": "YOUR KEY"
},
```

## Updating Composer artifacts

Using Composer, you can now Open the folder containing your updated Skill and see the Dialogs as before enabling you to easily make changes directly within the updated Skill.
