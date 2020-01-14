---
layout: tutorial
category: Skills
subcategory: Extend a v4 Bot
language: typescript
title: Forward queries to Skills
order: 5
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

Within your Main/Router dialog you firstly need to ensure the SkillDialogs registered previously are added to the dialog stack:

```typescript
skillDialogs.forEach((skillDialog: SkillDialog) => {
    this.addDialog(skillDialog);
});
```

Add the following code after your Dispatcher has executed passing the registered Skills and the Intent returned from the Dispatcher. If the `isSkill` method returns true then you start the appropriate SkillDialog instance passing the Skill Manifest Id and the matching intent.

```typescript
// Identify if the dispatch intent matches any Action within a Skill if so, we pass to the appropriate SkillDialog to hand-off
const identifiedSkill: ISkillManifest | undefined = SkillRouter.isSkill(this.settings.skills, intent);
if (identifiedSkill !== undefined) {
    // We have identified a skill so initialize the skill connection with the target skill
    // the dispatch intent is the Action ID of the Skill enabling us to resolve the specific action and identify slots
    // Pass the activity we have
    const result: DialogTurnResult = await dc.beginDialog(identifiedSkill.id);

    if (result.status === DialogTurnStatus.complete) {
        await this.complete(dc);
    }
} else {
    // Your normal intent routing logic
}
```