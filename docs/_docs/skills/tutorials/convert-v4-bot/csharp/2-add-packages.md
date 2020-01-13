---
layout: tutorial
category: Skills
subcategory: Convert a v4 Bot
language: csharp
title: Add Bot Framework Solutions packages
order: 2
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}


1. Implement MVC architecture
    - If you have an existing bot using the v4 SDK, following the MVC approach from this [Bot Builder sample](https://github.com/Microsoft/BotBuilder-Samples/tree/master/samples/csharp_dotnetcore/05.multi-turn-prompt).

1. Enable the Bot Framework Solutions packages
    - Add [`Microsoft.Bot.Builder.Solutions`](https://www.nuget.org/packages/Microsoft.Bot.Builder.Solutions/) and [`Microsoft.Bot.Builder.Skills`](https://www.nuget.org/packages/Microsoft.Bot.Builder.Skills/) NuGet packages to your solution.

2. Create a custom Skill adapter

    - Create a a custom adapter class that derives from the SkillAdapter and add the `SkillMiddleware.cs` class is added to it.

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
                  ...
                  Use(new SkillMiddleware(userState));
              }
          }
      ```

3. Add the Skill services to startup
    - In your `startup.cs` file, add the following Transient adapters:

      ```csharp
      services.AddTransient<IBotFrameworkHttpAdapter, DefaultAdapter>();
      services.AddTransient<SkillAdapter, CustomSkillAdapter>();
      ```

4. Update your BotController class

    - Update your `BotController.cs` class to derive from `SkillController`

      ```csharp
        [ApiController]
        public class BotController : SkillController
        {
            public BotController(IServiceProvider serviceProvider, BotSettingsBase botSettings)
                : base(serviceProvider, botSettings)
            { }
        }
      ```