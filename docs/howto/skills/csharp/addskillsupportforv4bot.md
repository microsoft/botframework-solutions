# Enable Bot Framework Skills on an existing v4 bot (not based on Virtual Assistant Template)

**APPLIES TO:** ✅ SDK v4

## In this how-to

- [Intro](#intro)
- [Update your bot to use Bot Framework Solutions libraries](#update-your-bot-to-use-bot-framework-solutions-libraries)
- [Skill configuration](#skill-configuration)
- [Skill Dialog Registration](#skill-dialog-registration)
- [Route utterances to Skills](#route-utterances-to-skills)

## Intro

### Prerequisites

You have an existing bot using the v4 SDK, following the MVC approach from this [Bot Builder sample](https://github.com/Microsoft/BotBuilder-Samples/tree/master/samples/csharp_dotnetcore/05.multi-turn-prompt).

## Overview

Creating a Bot Framework Bot through the [Virtual Assistant template](../../../tutorials/csharp/virtualassistant.md) is the easiest way to get started with using Skills. If you have an existing v4 based Bot, the recommended approach would be to take the resulting project from this template and bring across your custom dialogs to get started quickly.

 If, however you have an existing V4 Bot that you wish to add Skill capability then please follow the steps below.

## Update your bot to use Bot Framework Solutions libraries

Add [`Microsoft.Bot.Builder.Solutions`](https://www.nuget.org/packages/Microsoft.Bot.Builder.Solutions/) and [`Microsoft.Bot.Builder.Skills`](https://www.nuget.org/packages/Microsoft.Bot.Builder.Skills/) NuGet packages to your solution.

## Skill Configuration

The `Microsoft.Bot.Builder.Skills` package provides a `SkillManifest` type that describes a Skill. Your bot should maintain a collection of registered Skills typically serialised into a JSON configuration file. The Virtual Assistant template uses a `skills.json` file for this purpose.

As part of your Configuration processing you should construct a collection of registered Skills by deserializing this file, for example:
```
public List<SkillManifest> Skills { get; set; }
```

## Skill Dialog Registration

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

## Route utterances to Skills

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
