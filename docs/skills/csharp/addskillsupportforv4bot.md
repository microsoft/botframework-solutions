# Adding Skills Support to a V4 Bot (not based on Virtual Assistant Template)

## Table of Contents
- [Adding Skills Support to a V4 Bot (not based on Virtual Assistant Template)](#adding-skills-support-to-a-v4-bot-not-based-on-virtual-assistant-template)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Libraries](#libraries)
  - [Skill Configuration](#skill-configuration)
  - [Skill Dialog Registration](#skill-dialog-registration)
  - [Routing utterances to Skills](#routing-utterances-to-skills)

## Overview

Creating a Bot Framework Bot through the [Virtual Assistant template](/docs/virtual-assistant/README.md) is the easiest way to get started with Skills. If, however you have an existing V4 Bot that you wish to add Skill capability then please follow the steps below.

## Libraries

- Add `Microsoft.Bot.Builder.Solutions` and `Microsoft.Bot.Builder.Skills` Nuget libraries to your solution

## Skill Configuration

The 'Skills' nuget provides a `SkillManifest` type that describes a Skill. You Bot should maintain a collection of registered Skills typically serialised into a JSON configuration file. The Virtual Assistant template uses a `skills.json` file for this purpose.

As part of your Configuration processing you should construct a collection of registered Skills by deserializing this file, for example:
```
public List<SkillManifest> Skills { get; set; }
```

## Skill Dialog Registration

In your `Startup.cs` file register a `SkillDialog` for each registered skill as shown below, this uses the collection of Skills that you created in the previous step.
```
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
```
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

## Routing utterances to Skills

Within your Main/Router dialog you firstly need to ensure the SkillDialogs registered previously are added to the dialog stack:
```
foreach (var skillDialog in skillDialogs)
{
    AddDialog(skillDialog);
}
```

Add the following code after your Dispatcher has executed passing the registered Skills and the Intent returned from the Dispatcher. If the IsSkill method returns true then you start the appropriate SkillDialog instance passing the Skill Manifest Id and the matching intent.
```
// Identify if the dispatch intent matches any Action within a Skill if so, we pass to the appropriate SkillDialog to hand-off
var identifiedSkill = SkillRouter.IsSkill(_settings.Skills, intent.ToString());

if (identifiedSkill != null)
{
    // We have identiifed a skill so initialize the skill connection with the target skill 
    // the dispatch intent is the Action ID of the Skill enabling us to resolve the specific action and identify slots
    await dc.BeginDialogAsync(identifiedSkill.Id, intent);

    // Pass the activity we have
    var result = await dc.ContinueDialogAsync();

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