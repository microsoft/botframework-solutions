---
layout: tutorial
category: Skills
subcategory: Extend a v4 Bot
language: typescript
title: Skill dialog registration
order: 4
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

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