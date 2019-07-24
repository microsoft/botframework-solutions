# Migrate an existing v4 bot to a Bot Framework Skill (C#)

**APPLIES TO:** ✅ SDK v4

## In this how-to

- [Intro](#intro)
- [Update your bot to use Bot Framework Solutions libraries](#update-your-bot-to-use-bot-framework-solutions-libraries)
- [Add a Skill manifest](#add-a-skill-manifest)

## Intro

### Prerequisites

You have an existing bot using the v4 SDK, following the MVC approach from this [Bot Builder sample](https://github.com/Microsoft/BotBuilder-Samples/tree/master/samples/csharp_dotnetcore/05.multi-turn-prompt).

### Overview

Creating a [Bot Framework Skill](../../../overview/skills.md) through the [Skill template](/docs/tutorials/csharp/skill.md#create-your-skill) is the easiest way to get started with creating a new Skill. If you have an existing v4 based Bot, we  recommended you take the resulting project from this template and copy over across your custom dialogs to get started quickly.

If you want to manually update your existing bot into a Bot Framework Skill, you can continue below.

## Update your bot to use Bot Framework Solutions libraries

### 1. Enable the Bot Framework Solutions packages

Add [`Microsoft.Bot.Builder.Solutions`](https://www.nuget.org/packages/Microsoft.Bot.Builder.Solutions/) and [`Microsoft.Bot.Builder.Skills`](https://www.nuget.org/packages/Microsoft.Bot.Builder.Skills/) NuGet packages to your solution.

### 2. Create a custom Skill adapter

Create a a custom adapter class that derives from the SkillAdapter and add the `SkillMidleware.cs` class is added to it.

```csharp
  public class CustomSkillAdapter : SkillAdapter
    {
        public CustomSkillAdapter(
            BotSettings settings,
            ICredentialProvider credentialProvider,
            BotStateSet botStateSet,
            ResponseManager responseManager,
            IBotTelemetryClient telemetryClient,
            UserState userState)
            : base(credentialProvider)
        {
            // ...
            Use(new SkillMiddleware(userState));
        }
    }
```

### 3. Add the Skill services to startup

In your `startup.cs` file, add the following Transient adapters:

```csharp
services.AddTransient<IBotFrameworkHttpAdapter, DefaultAdapter>();
services.AddTransient<SkillAdapter, CustomSkillAdapter>();
```

### 4. Update your BotController class

Update your `BotController.cs` class to derive from `SkillController`

```csharp
[ApiController]
public class BotController : SkillController
{
    public BotController(IServiceProvider serviceProvider, BotSettingsBase botSettings)
        : base(serviceProvider, botSettings)
    { }
}
```

## Add a Skill manifest

Create a `manifestTemplate.json` file in the root of your Bot. Ensure at a minimum the root level `id`, `name`, `description` and action details are completed.

```csharp
{
  "id": "",
  "name": "",
  "description": "",
  "iconUrl": "",
  "authenticationConnections": [ ],
  "actions": [
    {
      "id": "",
      "definition": {
        "description": "",
        "slots": [ ],
        "triggers": {
          "utteranceSources": [
            {
              "locale": "en",
              "source": [
                "luisModel#intent"
              ]
            }
          ]
        }
      }
    }
  ]
}
```
