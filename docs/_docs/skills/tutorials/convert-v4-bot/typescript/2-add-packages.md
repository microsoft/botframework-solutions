---
layout: tutorial
category: Skills
subcategory: Convert a v4 Bot
language: TypeScript
title: Add Bot Framework Solutions packages
order: 2
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

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