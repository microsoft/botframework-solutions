# Skill Enabling a V4 Bot (not based on Skill Template)

## Table of Contents
- [Table of Contents](#table-of-contents)
- [Overview](#overview)
- [Libraries](#libraries)
- [Adapter](#adapter)
- [Startup](#startup)
- [Add Skill Controller](#add-skill-controller)
- [Manifest Template](#manifest-template)

## Overview

Creating a Skill through the [Skill template](/docs/tutorials/csharp/skill.md#create-your-skill) is the easiest way to get started with creating a new Skill. If you have an existing v4 based Bot, the recommended approach would be to take the resulting project from this template and bring across your custom dialogs to get started quickly.

If however you want to manually enable your Bot to be called as a Skill follow the steps below. This documentation assumes you are using the MVC approach for v4 Bots as detailed in [this sample](https://github.com/Microsoft/BotBuilder-Samples/tree/master/samples/csharp_dotnetcore/05.multi-turn-prompt).

## Libraries
- Add `Microsoft.Bot.Builder.Solutions` and `Microsoft.Bot.Builder.Skills` NuGet libraries to your solution

## Adapter

Create a Custom Adapter that derives from the SkillAdapter and ensure the SkillMiddleware is added
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

## Startup

```csharp
services.AddTransient<IBotFrameworkHttpAdapter, DefaultAdapter>();
services.AddTransient<SkillAdapter, CustomSkillAdapter>();
```

## Add Skill Controller

Update your Bot Controller class to derive from `SkillController`
```csharp
[ApiController]
public class BotController : SkillController
{
    public BotController(IServiceProvider serviceProvider, BotSettingsBase botSettings)
        : base(serviceProvider, botSettings)
    { }
}
```

## Manifest Template

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
