---
layout: tutorial
category: Skills
subcategory: Migrate to 4.7 skill
language: C#
title: Tutorial
order: 1
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

### Purpose

In the 4.7 release of the Bot Builder SDK skill capabilities are introduced as part of the core SDK. Implementations of the Virtual Assistant and Skill Templates built using Bot Builder packages 4.6.2 and below need to be migrated in order to use this new approach. With the 4.7 skill protocol, any bot can become a skill, so these steps only apply to prior implementations.

### Prerequisites

Exising Skill built from the Skill Template v4.6.0.1 and below

### Steps

1. Update BotBuilder version to 4.7.1 for the Skill project

    In the latest Skill Template, the version of the BotBuilder libraries is 4.6.1. They need to be updated to 4.7.1. 

    ```json

        <PackageReference Include="Microsoft.Bot.Builder" Version="4.6.1 -> 4.7.0" />
        <PackageReference Include="Microsoft.Bot.Builder.AI.Luis" Version="4.6.1 -> 4.7.0" />
        <PackageReference Include="Microsoft.Bot.Builder.AI.QnA" Version="4.6.1 -> 4.7.0" />
        <PackageReference Include="Microsoft.Bot.Builder.ApplicationInsights" Version="4.6.1 -> 4.7.0" />
        <PackageReference Include="Microsoft.Bot.Builder.Azure" Version="4.6.1 -> 4.7.0" />
        <PackageReference Include="Microsoft.Bot.Builder.Dialogs" Version="4.6.1 -> 4.7.0" />
        <PackageReference Include="Microsoft.Bot.Builder.Integration.ApplicationInsights.Core" Version="4.6.1 -> 4.7.0" />
        <PackageReference Include="Microsoft.Bot.Builder.Integration.AspNet.Core" Version="4.6.1 -> 4.7.0" />
        <PackageReference Include="Microsoft.Bot.Configuration" Version="4.6.1 -> 4.7.0" />
        <PackageReference Include="Microsoft.Bot.Connector" Version="4.6.1 -> 4.7.0" />
        <PackageReference Include="Microsoft.Bot.Schema" Version="4.6.1 -> 4.7.0" />

    ```

    > Please note that Microsoft.Bot.Builder.Solutions library version 4.6.2 is based on Bot Builder 4.6 and should be removed. If you are dependent on components from the library, you won't be able to do the migration at the moment. We are working on the release of a compatible version of the library so stay tuned.

2. Update SkillController.cs

    In the existing Skill Template, `Controller\SkillController.cs` implements SkillController which includes capabilities of standing up new API for skill invocation. In the 4.7 skill protocol there's no need for new API for skill invocation, so the default controller should be used.

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

3. Remove extra registrations in startup.cs

    In the current Skill Template, there are registrations for classes that are no longer needed in the 4.7 Skill protocol. They should be removed from Startup.cs in the Skill bot. 

    Remove these lines:

    ```csharp

        services.AddTransient<SkillWebSocketBotAdapter, SkillWebSocketBotAdapter>();	
        services.AddTransient<SkillWebSocketAdapter>();	
        services.AddSingleton<IWhitelistAuthenticationProvider, WhitelistAuthenticationProvider>();

    ```

4. Add code to handle EndOfConversation activity from parent bot

    In Skill invocation, a skill needs to handle EndOfConversation activity in order to support cancellation in interruption. This capability will be included in the next release of the Microsoft.Bot.Builder.Solutions package and eventually as part of the core Bot Builder SDK. For now, you can add these lines of code in your IBot implementation class to handle EndOfConversation activity:

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
    
    With this block of code, when your skill receives an EndOfConversation activity it will clear out the existing dialog state so the skill will be in a clean state ready for the next conversation.
