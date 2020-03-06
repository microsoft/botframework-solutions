---
category: Skills
subcategory: Handbook
title: Experimental - Adding Bot Framework Composer to a Skill
description: Add Bot Framework Composer to a Skill
order: 1
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

## Introduction

The [Bot Framework Composer](https://aka.ms/bfcomposer) is a visual designer that lets you quickly and easily build sophisticated conversational bots without writing code. Composer is currently in Public Preview and the documentation below covers manual steps to move Dialog management for an existing Skill created using the [Skill Template](https://microsoft.github.io/botframework-solutions/skills/tutorials/create-skill/csharp/1-intro/) to Composer.

An approach for hybrid composition of Waterfall and Adaptive/Composer dialogs within the same Assistant or Skill is being tested, in the meantime these steps will move all dialog management to Composer based dialogs.

Moving forward there will be an updated Skill Template that will support Bot Framework Composer out of the box without these changes.

> Note that this guidance is experimental and for testing purposes only.

## Pre-Requisites

- An existing Skill created using the Skill Template, follow [this tutorial](https://microsoft.github.io/botframework-solutions/skills/tutorials/create-skill/csharp/1-intro/) if needed.

## Design your Dialogs

The first step is to create a Composer project and create the appropriate LU, Dialog and LG assets for your scenario. Ensure these work as expected using the `Start Bot` and `Test in Emulator` feature of the Bot Framework Emulator, this will also ensure LUIS resources are published.

## Retrieve the Generated Files

Navigate to the folder where you have cloned the Bot Framework Composer and open the `SampleBots` folder. You should find a sub-folder matching the name of your previously created Composer project. Copy the `ComposerDialogs` folder into the root of your Skill project.

## Add ComposerBot

The `ComposerBot` implementation bootstraps supporting infrastructure for the Composer including state and resource files and is provided as part of Composer.

1. Retrieve the latest source code for `ComposerBot` from [here](https://github.com/microsoft/BotFramework-Composer/blob/stable/BotProject/CSharp/ComposerBot.cs) and add to the `Bots` folder of your Skill project.
2. Add the following additional lines to the constructor:

```csharp
    DeclarativeTypeLoader.AddComponent(new AdaptiveComponentRegistration());
    DeclarativeTypeLoader.AddComponent(new LanguageGenerationComponentRegistration());
    DeclarativeTypeLoader.AddComponent(new DialogComponentRegistration());
```

## Initialise ComposerBot

The next step is to initialise `ComposerBot` making it available to use. Within `Startup.cs` of your Skill add the following lines of code. This will override any other Bots registered, these can be removed from Startup if desired.

```csharp
    TypeFactory.Configuration = this.Configuration;
    var resourceExplorer = new ResourceExplorer().AddFolder("ComposerDialogs");
    services.AddSingleton<ResourceExplorer>(resourceExplorer);

    services.AddSingleton<IBot, ComposerBot>((sp) => new ComposerBot(
        "Main.dialog",
        sp.GetService<ConversationState>(),
        sp.GetService<UserState>(),
        resourceExplorer,
        DebugSupport.SourceMap));
```

## Adapter Changes

The next stage is to provide Adapter configuration for supporting Adaptive Dialogs infrastructure.

1. Within the `Adapters\DefaultAdapter.cs` file of your Skill project file add the following lines to the constructor 

```csharp
    ResourceExplorer resourceExplorer,
    IConfiguration configuration,
```

2. Then add these lines to the constructor implementation

```csharp
    // Register UserState and ConversationState within TurnContext
    this.UseState(userState, conversationState);

    this.Use(new RegisterClassMiddleware<IConfiguration>(configuration));
    this.UseAdaptiveDialogs();
    this.UseResourceExplorer(resourceExplorer);
    this.UseLanguageGeneration(resourceExplorer, "common.lg");
```

## Sending EndOfConversation

The EndOfConversation activity is used to indicate when a Skill has finished execution, handing back control to the caller. Moving forward, this will be automatically handled by the `DialogManager`. At this time the following modifications are required to support this scenario.

1. Add the class to your project.

```csharp
public class AdaptiveDialogEx : AdaptiveDialog
{
    public AdaptiveDialogEx(string dialogId = null, string callerPath = null, int callerLine = 0) : base(dialogId, callerPath, callerLine)
    {
        
    }

    public async override Task EndDialogAsync(ITurnContext turnContext, DialogInstance instance, DialogReason reason, CancellationToken cancellationToken = default)
    {
        var endOfConversation = new Activity(ActivityTypes.EndOfConversation)
        {
            Code = EndOfConversationCodes.CompletedSuccessfully,               
        };

        await turnContext.SendActivityAsync(endOfConversation, cancellationToken);

        await base.EndDialogAsync(turnContext, instance, reason, cancellationToken);
    }
}
```

2. For each Composer Dialog (.dialog file) within your ComposerDialogs folder update the `Microsoft.AdaptiveDialog` reference at the top to `{YourNamespace}.AdaptiveDialogEx`.

```json
{
  "$type": "Microsoft.AdaptiveDialogEx"
}
```

3. Add this extended Component Registration entry

```csharp
public class AdaptiveComponentRegistrationEx : ComponentRegistration
{
    public override IEnumerable<TypeRegistration> GetTypes()
    {
        // Conditionals
        yield return new TypeRegistration<AdaptiveDialogEx>("{YourNamespace}.AdaptiveDialogEx");
    }
}
```

4. Invoke the extended component registration within the constructor of your ComposerBot.cs class

```csharp
    DeclarativeTypeLoader.AddComponent(new AdaptiveComponentRegistrationEx());
```

5. Update `AdaptiveDialog` references to `AdaptiveDialogEx` within ComposerBot.cs

## Configuration

The final step is to ensure configuration settings required by Composer are available in the Skill `appSettings.json` file, otherwise they won't be available at runtime along with addressing slight differences between configuration items currently present.

1. Rename the existing `key` element underneath `luis` to `endpointKey`. Given these steps replace any existing dialogs there is no need to maintain this element for other dialogs.
2. Add an `endpoint` element and set this to the endpoint of the published LUIS resource that Composer has created for you. You can find this through the LUIS portal, e.g. `https://myluisresource.cognitiveservices.azure.com`

3. Add the generated LUIS configuration file to your `Startup.cs` class constructor

```csharp
    var luisAuthoringRegion = Environment.GetEnvironmentVariable("LUIS_AUTHORING_REGION") ?? "westus";
   .AddJsonFile($"ComposerDialogs/Generated/luis.settings.composer.{luisAuthoringRegion}.json", optional: true, reloadOnChange: true)
```

## Testing

Your Skill should now pass all incoming utterances through to Composer. Any interruption or MainDialog logic previously executed by the Skill will now no longer be invoked as Composer is used for all dialog processing.

## Updating Composer artifacts

Using Composer, you can now Open the folder containing your updated Skill and see the Dialogs as before enabling you to easily make changes directly within the updated Skill.