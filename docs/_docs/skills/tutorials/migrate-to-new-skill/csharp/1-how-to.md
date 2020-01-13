---
layout: tutorial
category: Skills
subcategory: Migrate to GA Bot Framework Skills
language: C#
title: Tutorial
order: 1
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

### Purpose

In the Bot Framework 4.7 release, the Bot Framework Skills capability was transitioned into a core part of the core SDK and reached the General Availability (GA) milestone. Existing Virtual Assistant and Skill Template projects built using Bot Builder packages 4.6.2 and below need to be migrated in order to use this new approach. With the 4.7 skill protocol, any bot can become a skill without adapter changes hence the simplification that was achieved.

### Prerequisites

An existing Bot Framework Skill built from using  Skill Template v4.6.0.1 and below.

### Steps

1. Update the Bot Framework SDK version to 4.7.0 for the Skill project

    In the latest Skill Template, the version of the BotBuilder libraries will be 4.6.1 or below. These need to be updated to 4.7.0. 

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

    > Please note that Microsoft.Bot.Builder.Solutions library version 4.6.2 is compatible with BotBuilder 4.7.0. A new package will be published moving forwards.

2. Update BotController.cs

    Within your Skill project, `Controller\BotController.cs` implements SkillController which includes capabilities of standing up new APIs for skill invocation. This requirement has now been removed, therefore a default controller should be used.

    Change the BotController.cs as shown below.

    ```csharp

    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;

    namespace {YourSkill}.Controllers
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

3. Remove extra registrations in `Startup.cs`

    In the current Skill Template, there are registrations for classes that are no longer needed in the 4.7 Skill protocol. They should be removed from Startup.cs within your Skill project.

    Remove these lines:

    ```csharp

        services.AddTransient<SkillWebSocketBotAdapter, CustomSkillAdapter>();	
        services.AddTransient<SkillWebSocketAdapter>();	
        services.AddSingleton<IWhitelistAuthenticationProvider, WhitelistAuthenticationProvider>();

    ```

4. Remove CustomSkillAdapter.cs

    Since a custom adapter is no longer needed, you can remove `Adapters\CustomSkillAdapter.cs` from your project.

5. Remove custom implementation of `IWhitelistAuthenticationProvider`

    If you have implemented your own class for the interface `IWhitelistAuthenticationProvider` instead of using the WhitelistAuthenticationProvider class from the Solutions lib this can be removed.

6. Keep using the MultiProviderAuthDialog (Non-Action)

    In the previous model the parent bot (VA) is the responsible for performing OAuth tasks by acting on behalf of a skill thus ensuring a common, shared authentication experience across an assistant. With this new release, Skills can now perform their own authentication requests and still benefit from a shared trust boundary.

    The existing `MultiProviderAuthDialog` if used will automatically adapt to this change and no changes are required. As required you can switch to using the `OAuthPrompt` directly.

7. Add code to handle the `EndOfConversation` activity from parent bot

    In Skill invocation, a skill needs to handle an `EndOfConversation` activity in order to support cancellation for interruption scenarios at the parent bot level. This capability will be included in the next release of the `Microsoft.Bot.Builder.Solutions` package and eventually as part of the core Bot Builder SDK. For now, add these lines of code within the `OnTurnAsync` handler in your `IBot` implementation class (within the Bots folder of your project) to handle the `EndOfConversation` activity:

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

8. Update to use `EndOfConversation` instead of Handoff when a conversation completed

    In the `OnDialogCompleteAsync` function of `MainDialog.cs`, instead of sending back a 'Handoff' activity, update it to be `EndOfConversation` inline with the new Skills changes.
    
    ```csharp
        var response = outerDc.Context.Activity.CreateReply();
        response.Type = ActivityTypes.Handoff -> ActivityTypes.EndOfConversation;
        await outerDc.Context.SendActivityAsync(response);

    ```

9. Add code in the exception handle of the adapter to send an EndOfConversation activity back

    In the exception handler of the `DefaultAdapter` normally located in the `Adapters` folder, add code to send an `EndOfConversation` activity back to complete a conversation when exception happens:

    ```csharp
    OnTurnError = async (turnContext, exception) =>
    {
        var eocActivity = turnContext.Activity.CreateReply();
        eocActivity.Type = ActivityTypes.EndOfConversation;
        await outerDc.Context.SendActivityAsync(eocActivity);

        ...
    };

    ```

Please Note that this document doesn't contain the step to update the skill manifest to the latest schema. The work is still in progress and this document will be updated once that's finished.