# Enable Bot Framework Skills on an existing v4 bot (TypeScript)

**APPLIES TO:** ✅ SDK v4

## In this how-to

- [Intro](#intro)
- [Update your bot to use Bot Framework Solutions libraries](#update-your-bot-to-use-bot-framework-solutions-libraries)
- [Skill configuration](#skill-configuration)
- [Skill Dialog registration](#skill-dialog-registration)
- [Routing utterances to Skills](#routing-utterances-to-skills)

## Intro

### Overview

Creating a Bot Framework Bot through the [Virtual Assistant template](/docs/overview/virtualassistant.md) is the easiest way to get started with using Skills. If you have an existing v4 based Bot, the recommended approach would be to take the resulting project from this template and bring across your custom dialogs to get started quickly.

If, however you have an existing V4 Bot that you wish to add Skill capability then please follow the steps below.

## Update your bot to use Bot Framework Solutions libraries

- Add [`botbuilder-solutions`](https://www.npmjs.com/package/botbuilder-solutions) and [`botbuilder-skills`](https://www.npmjs.com/package/botbuilder-skills) npm packages to your solution.

## Skill configuration

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

## Routing utterances to Skills

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
    // We have identiifed a skill so initialize the skill connection with the target skill
    // the dispatch intent is the Action ID of the Skill enabling us to resolve the specific action and identify slots
    await dc.beginDialog(identifiedSkill.id);

    // Pass the activity we have
    const result: DialogTurnResult = await dc.continueDialog();

    if (result.status === DialogTurnStatus.complete) {
        await this.complete(dc);
    }
} else {
    // Your normal intent routing logic
}
```
