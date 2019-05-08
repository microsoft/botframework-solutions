# Skill Enabling a V4 Bot (not based on Skill Template)

## Table of Contents
- [Table of Contents](#table-of-contents)
- [Overview](#overview)
- [Libraries](#libraries)
- [Adapter](#adapter)
- [Startup](#startup)
- [Add Skill Endpoint](#add-skill-endpoint)
- [Manifest Template](#manifest-template)

## Overview

Creating a Skill through the [Skill template](/docs/tutorials/typescript/skill.md#create-your-skill) is the easiest way to get started with creating a new Skill. If you have an existing v4 based Bot, the recommended approach would be to take the resulting project from this template and bring across your custom dialogs to get started quickly.

If however you want to manually enable your Bot to be called as a Skill follow the steps below.

## Libraries

- Add `botbuilder-solutions` and `botbuilder-skills` npm packages to your solution.

## Adapter

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

## Startup

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

## Add Skill Endpoint

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

## Manifest Template

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
