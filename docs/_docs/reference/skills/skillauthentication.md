---
category: Reference
subcategory: Skills
title: Skill Authentication
description: Details on skill authentication approach and flow.
order: 4
---

# {{ page.title }}
{:.no_toc}

## In this reference
{:.no_toc}

* 
{:toc}

A Skill needs to be able to authenticate the request coming from another bot (Virtual Assistant). The Skill model requires two levels of Authentication:

## JWT Authentication

The Virtual Assistant needs to include an Authorization header in the request. 

This is needed as a Skill needs to verify that the request comes from a properly registered bot service, and that the request was intended for the skill. Because every bot service is a Microsoft app, we can leverage AAD to obtain JWT token as well as verification

![Skill Authentication Flow]({{site.baseurl}}/assets/images/virtualassistant-skillauthentication.png)

Between Virtual Assistant and skill bot, we'll use AAD as the authority to generate and validate token. The token will be a JWT token. Virtual Assistant will use this information to request a JWT token:
  1. Microsoft app id - this will become the source appid claim in the token
  2. Microsoft app password
  3. Skill bot's Microsoft app id - this will become the audience claim in the token

The JWT token will be a 'bearer' token so it'll be part of the Authorization header.

When skill bot receives a request, it looks at the Authorization header to retrieve the token. Then it will rely on AAD to decrypt & validate the JWT token. Right now the skill will only verify if the audience claim is the skill's Microsoft app id. If the audience claim verification passes, then the authentication succeeds and the request moves forward into the skill bot for further processing.

By default, a skill that's created out of a Skill Template enables JWT authentication. 

On the Virtual Assistant side, we use the SkillDialog to dispatch requests to the skills. To enable Virtual Assistant to obtain proper JWT token to send a request to skill, you need to have these lines of code when you create the SkillDialog instances:

```csharp
var credentials = new MicrosoftAppCredentialsEx(settings.MicrosoftAppId, settings.MicrosoftAppPassword, skill.MSAappId);
skillDialogs.Add(new SkillDialog(skill, credentials, telemetryClient, userState, authDialog));
```

The `MicrosoftAppCredentialsEx` class provided within the Microsoft.Bot.Builder.Skills package is the central place to manage the information needed for the skill to obtain the AAD token. Once you pass this into the SkillDialog, the SkillDialog will be able to use it to properly retrieve the AAD token. This behavior is the default behavior if you create a Virtual Assistant out of the Virtual Assistant Template VSIX.

## Whitelist Authentication

After the JWT token is verified, the Skill bot needs to verify if the request comes from a bot that's previously included in a whitelist. A Skill needs to have knowledge of it's callers and give permissions to that bot explicitly instead of any bot that could call the Skill. This level of authorization is enabled by default as well, making sure a Skill is well protected from public access. Developers need to do the following to implement the Whitelist mechanism:

Declare a class `WhiteListAuthProvider` in the bot service project that implements the interface `IWhitelistAuthenticationProvider`

```csharp
public HashSet<string> AppsWhitelist
{
    get
    {
        return new HashSet<string>
        {
            // add AppIds of Virtual Assistant here
        };
    }
}
```

By adding the Microsoft App id of the Virtual Assistant that's calling the Skill into the property AppsWhitelist, you are allowing the bot that's associated with that app id to invoke your skill.

In `Startup.cs`, register a singleton of the interface with this class

```csharp
// Register WhiteListAuthProvider
services.AddSingleton<IWhitelistAuthenticationProvider, WhiteListAuthProvider>();
```

In `BotController.cs` (derived from the `SkillController`, add the class as a new parameter to the constructor

```csharp
public BotController(
    IBot bot,
    BotSettingsBase botSettings,
    IBotFrameworkHttpAdapter botFrameworkHttpAdapter,
    SkillWebSocketAdapter skillWebSocketAdapter,
    IWhitelistAuthenticationProvider whitelistAuthenticationProvider)
    : base(bot, botSettings, botFrameworkHttpAdapter, skillWebSocketAdapter, whitelistAuthenticationProvider)
{}
```

With all these changes in place, you're enabling your Skill to allow bots to invoke it as long as the bot's Microsoft App id is included in the whitelist.
