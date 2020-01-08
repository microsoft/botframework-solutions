---
layout: tutorial
category: Skills
subcategory: Migrate to 4.7 skill
title: How-to
language: C#
order: 1
---


# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

### Purpose

In the latest 4.7 release of the BotBuilder library, Skill capabilities are introduced as part of the core SDK. The Skills that were created based on the current and old versions of the Skill Template were built on a different protocol than the 4.7 Skills. So in order to make an old Skill to be a 4.7 Skill, there's some work required for migration. Luckily the migration work is relatively small, because one of the goals of the Skill capabilities out of 4.7 SDK is that any bot can be a skill. That means for a regular bot to become a skill, there is no additional work needed. 

### Prerequisites

Exising Skill that is built from the existing Skill Template

### Steps

1. Update BotBuilder version to 4.7.1 for the Skill project

In the latest Skill Template, the version of the BotBuilder libraries is 4.6.1. They need to be updated to 4.7.1. 

```json

    <PackageReference Include="Microsoft.Bot.Builder" Version="4.6.1 -> 4.7.1" />
    <PackageReference Include="Microsoft.Bot.Builder.AI.Luis" Version="4.6.1 -> 4.7.1" />
    <PackageReference Include="Microsoft.Bot.Builder.AI.QnA" Version="4.6.1 -> 4.7.1" />
    <PackageReference Include="Microsoft.Bot.Builder.ApplicationInsights" Version="4.6.1 -> 4.7.1" />
    <PackageReference Include="Microsoft.Bot.Builder.Azure" Version="4.6.1 -> 4.7.1" />
    <PackageReference Include="Microsoft.Bot.Builder.Dialogs" Version="4.6.1 -> 4.7.1" />
    <PackageReference Include="Microsoft.Bot.Builder.Integration.ApplicationInsights.Core" Version="4.6.1 -> 4.7.1" />
    <PackageReference Include="Microsoft.Bot.Builder.Integration.AspNet.Core" Version="4.6.1 -> 4.7.1" />
    <PackageReference Include="Microsoft.Bot.Configuration" Version="4.6.1 -> 4.7.1" />
    <PackageReference Include="Microsoft.Bot.Connector" Version="4.6.1 -> 4.7.1" />
    <PackageReference Include="Microsoft.Bot.Schema" Version="4.6.1 -> 4.7.1" />

```

Please note that the latest version Microsoft.Bot.Builder.Solutions library is 4.6.2 and it's based on BotBuilder 4.6. So before we release a new version of 4.7 for Microsoft.Bot.Builder.Solutions, please remove it. If you are already using some components from the library, you won't be able to do the migration at the moment. We are working on the release of a new version of the library so stay tuned.

2. Update SkillController.cs

In the existing Skill Template, the SkillController.cs under Controller folder uses SkillController which includes capabilities of standing up new API for skill invocation. Because in the 4.7 skill protocol there's no need for new API for skill invocation, we can just use a regular controller. 

Change the SkillController.cs to

```csharp

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;

namespace Skill.Controllers
{
    [Route("api/messages")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IBot _bot;

        public BotController(IBotFrameworkHttpAdapter httpAdapter, IBot bot)
        {
            _adapter = httpAdapter;
            _bot = bot;
        }

        [HttpPost]
        [HttpGet]
        public async Task PostAsync()
        {
            await _adapter.ProcessAsync(Request, Response, _bot);
        }
    }
}

```

3. Remove extra registration in startup.cs

In the current Skill Template, there's a few classes that are needed for Skill Invocation which are no longer needed in the 4.7 Skill. They should be removed from startup.cs of the Skill bot. 

Remove these lines:

```csharp

    services.AddTransient<SkillWebSocketBotAdapter, SkillWebSocketBotAdapter>();	
    services.AddTransient<SkillWebSocketAdapter>();	
    services.AddSingleton<IWhitelistAuthenticationProvider, WhitelistAuthenticationProvider>();

```

4. Add code to handle EndOfConversation coming from parent bot

In Skill invocation, a skill needs to handle EndOfConversation activity in order to support cancellation in interruption. This capability will be included in the next release of the Microsoft.Bot.Builder.Solutions package and eventually as part of the core BotBuilder SDK. For now, you can add these lines of code in your IBot implementation class to handle EndOfConversation activity:

```csharp

var activity = turnContext.Activity;
var dialogState = _conversationState.CreateProperty<DialogState>(nameof(DialogState)));
if (activity != null && activity.Type == ActivityTypes.EndOfConversation)
{
    await dialogState.DeleteAsync(turnContext).ConfigureAwait(false);
    await _conversationState.ClearStateAsync(turnContext).ConfigureAwait(false);
    await _conversationState.SaveChangesAsync(turnContext, force: true).ConfigureAwait(false);
    return;
}

```
With this block of code when your skill receives an EndOfConversation activity it'll clear out the existing dialog state so the skill will be in a clean state ready for next conversation.
