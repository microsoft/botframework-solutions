---
category: How To
subcategory: Skills
title: Enable skills on an existing v4 bot
description: How to add Skills to an existing v4 bot (not Virtual Assistant template)
order: 2
---

# {{ page.title }}
{:.no_toc}

## In this how-to
{:.no_toc}

* 
{:toc}

## Overview

Creating a Bot Framework Bot through the Virtual Assistant template is the easiest way to get started with using Skills. If you have an existing v4 based Bot, the recommended approach would be to take the resulting project from this template and bring across your custom dialogs to get started quickly.

 If, however you have an existing V4 Bot that you wish to add Skill capability then please follow the steps below.

## Update your bot to use Bot Framework Solutions libraries
#### C#
Add [`Microsoft.Bot.Builder.Solutions`](https://www.nuget.org/packages/Microsoft.Bot.Builder.Solutions/) and [`Microsoft.Bot.Builder.Skills`](https://www.nuget.org/packages/Microsoft.Bot.Builder.Skills/) NuGet packages to your solution.

#### TypeScript
Add [`botbuilder-solutions`](https://www.npmjs.com/package/botbuilder-solutions) and [`botbuilder-skills`](https://www.npmjs.com/package/botbuilder-skills) npm packages to your solution.

## Skill configuration
#### C#
The `Microsoft.Bot.Builder.Skills` package provides a `SkillManifest` type that describes a Skill. Your bot should maintain a collection of registered Skills typically serialized into a JSON configuration file. The Virtual Assistant template uses a `skills.json` file for this purpose.

As part of your Configuration processing you should construct a collection of registered Skills by deserializing this file, for example:

```csharp
public List<SkillManifest> Skills { get; set; }
```

#### TypeScript
The 'botbuilder-skills' package provides a `ISkillManifest` interface that describes a Skill. Your bot should maintain a collection of registered Skills typically serialized into a `JSON` configuration file. The Virtual Assistant template uses a `skills.json` file for this purpose that can be found in the `src` directory.

That file must have the following structure:

```json
{
  "skills": []
}
```

As part of your Configuration processing you should construct a collection of registered Skills by deserializing this file, for example:

```typescript
import { skills as skillsRaw } from './skills.json';
const skills: ISkillManifest[] = skillsRaw;
```

> NOTE: The `botbuilder-skills` package also provides a `IBotSettings` interface that can be used to storage the keys/secrets of the services that will be used to connect services to the bot.

## Skill Dialog registration
#### C#
In your `Startup.cs` file register a `SkillDialog` for each registered skill as shown below, this uses the collection of Skills that you created in the previous step.

```csharp
 // Register skill dialogs
services.AddTransient(sp =>
{
    var userState = sp.GetService<UserState>();
    var skillDialogs = new List<SkillDialog>();

    foreach (var skill in settings.Skills)
    {
        var authDialog = BuildAuthDialog(skill, settings);
        var credentials = new MicrosoftAppCredentialsEx(settings.MicrosoftAppId, settings.MicrosoftAppPassword, skill.MSAappId);
        skillDialogs.Add(new SkillDialog(skill, credentials, telemetryClient, userState, authDialog));
    }

    return skillDialogs;
});
```

For scenarios where Skills require authentication connections you need to create an associated `MultiProviderAuthDialog`

```csharp
 // This method creates a MultiProviderAuthDialog based on a skill manifest.
private MultiProviderAuthDialog BuildAuthDialog(SkillManifest skill, BotSettings settings)
{
    if (skill.AuthenticationConnections?.Count() > 0)
    {
        if (settings.OAuthConnections.Any() && settings.OAuthConnections.Any(o => skill.AuthenticationConnections.Any(s => s.ServiceProviderId == o.Provider)))
        {
            var oauthConnections = settings.OAuthConnections.Where(o => skill.AuthenticationConnections.Any(s => s.ServiceProviderId == o.Provider)).ToList();
            return new MultiProviderAuthDialog(oauthConnections);
        }
        else
        {
            throw new Exception($"You must configure at least one supported OAuth connection to use this skill: {skill.Name}.");
        }
    }

    return null;
}
```

#### TypeScript

In your `index.ts` file register a `SkillDialog` for each registered skill as shown below, this uses the collection of Skills that you created in the previous step.

```typescript
 // Register skill dialogs
const skillDialogs: SkillDialog[] = skills.map((skill: ISkillManifest) => {
    const authDialog: MultiProviderAuthDialog|undefined = buildAuthDialog(skill, botSettings);
    const credentials: MicrosoftAppCredentialsEx = new MicrosoftAppCredentialsEx(
        botSettings.microsoftAppId || '',
        botSettings.microsoftAppPassword || '',
        skill.msAppId);

    return new SkillDialog(skill, credentials, adapter.telemetryClient, skillContextAccessor, authDialog);
});
```

For scenarios where Skills require authentication connections you need to create an associated `MultiProviderAuthDialog`

```typescript
// This method creates a MultiProviderAuthDialog based on a skill manifest.
function buildAuthDialog(skill: ISkillManifest, settings: Partial<IBotSettings>): MultiProviderAuthDialog|undefined {
    if (skill.authenticationConnections !== undefined && skill.authenticationConnections.length > 0) {
        if (settings.oauthConnections !== undefined) {
            const oauthConnections: IOAuthConnection[] | undefined = settings.oauthConnections.filter(
                (oauthConnection: IOAuthConnection) => {
                return skill.authenticationConnections.some((authenticationConnection: IAuthenticationConnection) => {
                    return authenticationConnection.serviceProviderId === oauthConnection.provider;
                });
            });
            if (oauthConnections !== undefined) {
                return new MultiProviderAuthDialog(oauthConnections);
            }
        } else {
            throw new Error(`You must configure at least one supported OAuth connection to use this skill: ${skill.name}.`);
        }
    }

    return undefined;
}
```

## Route utterances to Skills
#### C#
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

#### TypeScript

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