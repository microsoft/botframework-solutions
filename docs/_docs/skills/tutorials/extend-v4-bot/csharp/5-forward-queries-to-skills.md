---
layout: tutorial
category: Skills
subcategory: Extend a v4 Bot
title: Forward queries to Skills
language: C#
order: 5
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

Within your Main/Router dialog you firstly need to ensure the SkillDialogs registered previously are added to the dialog stack:

```csharp
foreach (var skillDialog in skillDialogs)
{
    AddDialog(skillDialog);
}
```

Add the following code after your Dispatcher has executed passing the registered Skills and the Intent returned from the Dispatcher. If the IsSkill method returns true then you start the appropriate SkillDialog instance passing the Skill Manifest Id and the matching intent.

```csharp
// Identify if the dispatch intent matches any Action within a Skill if so, we pass to the appropriate SkillDialog to hand-off
var identifiedSkill = SkillRouter.IsSkill(_settings.Skills, intent.ToString());

if (identifiedSkill != null)
{
    // We have identified a skill so initialize the skill connection with the target skill
    // the dispatch intent is the Action ID of the Skill enabling us to resolve the specific action and identify slots
    // Pass the activity we have
    var result = await dc.BeginDialogAsync(identifiedSkill.Id, intent);

    if (result.Status == DialogTurnStatus.Complete)
    {
        await CompleteAsync(dc);
    }
}
else
{
    // Your normal intent routing logic
}
```
