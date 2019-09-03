---
category: How To
subcategory: Skills
title: Convert an existing v4 bot to a skill
description: Steps required to take an existing Bot and make it available as a skill.
order: 3
---

# {{ page.title }}
{:.no_toc}

## In this how-to
{:.no_toc}

* 
{:toc}

## Overview

Creating a [Bot Framework Skill]({{site.baseurl}}/overview/skills) through the [Skill template]({{site.baseurl}}/tutorials/csharp/create-skill/1_intro) is the easiest way to get started with creating a new Skill. If you have an existing v4 based Bot, we  recommended you take the resulting project from this template and copy over across your custom dialogs to get started quickly.

If you want to manually update your existing bot into a Bot Framework Skill, you can continue below.

## Update your bot to use the Bot Framework Solutions libraries
#### C#

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

#### TypeScript
1. Enable the Bot Framework Solutions packages
    - Add [`botbuilder-solutions`](https://www.npmjs.com/package/botbuilder-solutions) and [`botbuilder-skills`](https://www.npmjs.com/package/botbuilder-skills) npm packages to your solution.

2. Create a custom Skill adapter
    - Create a Custom Adapter that derives from the `SkillHttpBotAdapter` and ensure the `SkillMiddleware` is added

      ```typescript
      export class CustomSkillAdapter extends SkillHttpBotAdapter {
          constructor(
              telemetryClient: TelemetryClient,
              conversationState: ConversationState,
              skillContextAccessor: StatePropertyAccessor<SkillContext>,
              dialogStateAccessor: StatePropertyAccessor<DialogState>,
              ...
          ) {
              super(telemetryClient);
              [...]
              this.use(new SkillMiddleware(conversationState, skillContextAccessor, dialogStateAccessor));
              [...]
          }
      }
      ```

3. Add the Skill services to startup
    - Add the new adapter to your `index.ts` file.

      ```typescript
      const skillBotAdapter: CustomSkillAdapter = new CustomSkillAdapter(
          telemetryClient,
          conversationState,
          skillContextAccessor,
          dialogStateAccessor,
          ...);
      const skillAdapter: SkillHttpAdapter = new SkillHttpAdapter(
          skillBotAdapter
      );
      ```

4. Add the Skill endpoint
    - Update your `index.ts` to handle messages to interact with the bot as a skill.

      ```typescript
      // Listen for incoming assistant requests
      server.post('/api/skill/messages', (req: restify.Request, res: restify.Response) => {
          // Route received a request to adapter for processing
          skillAdapter.processActivity(req, res, async (turnContext: TurnContext) => {
              // route to bot activity handler.
              await bot.run(turnContext);
          });
      });
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
