---
layout: tutorial
category: Skills
subcategory: Extend a v4 Bot
title: Skill dialog registration
language: C#
order: 4
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

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