# Migrate an existing v4 bot to a Bot Framework Skill (TypeScript)

**APPLIES TO:** ✅ SDK v4

## In this how-to

- [Intro](#intro)
- [Update your bot to use Bot Framework Solutions libraries](#update-your-bot-to-use-bot-framework-solutions-libraries)
- [Add a Skill manifest](#add-a-skill-manifest)

## Intro

### Overview

Creating a Skill through the [Skill template](/docs/tutorials/typescript/skill.md#create-your-skill) is the easiest way to get started with creating a new Skill. If you have an existing v4 based Bot, the recommended approach would be to take the resulting project from this template and bring across your custom dialogs to get started quickly.

If however you want to manually enable your Bot to be called as a Skill follow the steps below.

## Update your bot to use Bot Framework Solutions libraries

### 1. Enable the Bot Framework Solutions packages

- Add [`botbuilder-solutions`](https://www.npmjs.com/package/botbuilder-solutions) and [`botbuilder-skills`](https://www.npmjs.com/package/botbuilder-skills) npm packages to your solution.

### 2. Create a custom Skill adapter

Create a Custom Adapter that derives from the `SkillHttpBotAdapter` and ensure the `SkillMiddleware` is added

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

### 3. Add the Skill services to startup

Add the new adapter to your `index.ts` file.

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

### 4. Add the Skill endpoint

Update your `index.ts` to handle messages to interact with the bot as a skill.

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

Create a `manifestTemplate.json` file in the root of your Bot. Ensure at a minimum the root level `id`, `name`, `description` and action details are completed. This file should be shared to the bot that will use this bot as a skill.

```json
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
