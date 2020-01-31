---
category: Overview
subcategory: What's New
language: 0_8_release
title: migrate old VA to 0.8
description: explains the steps to take to migrate an older version VA to use the capabilitites offered by the latest 0.8 release
order: 3
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

# New Virtual Assistant Template

As part of the 0.8 release, we published a new version of the Virtual Assistant template. In this new version, we are starting to use the Skills capabilities that was introduced as part of the 4.7 release of BotBuilder SDK (https://github.com/microsoft/botbuilder-dotnet/releases/tag/v4.7.0). For versions prior to the 0.8 release, we've always been relying on the Skills component under the Microsoft.Bot.Builder.Solutions library. If you want to use the latest Virtual Assistant without going through the process of creating a Virtual Assistant using the new template and applying all local changes manually, this documentation explains how to migrate your existing Virtual Assistant to take advantage of the Skill capabilities that's included as part of our 0.8 release.

### Prerequisites

The Virtual Assistant you are migrating from has to be created with the Virtual Assistant Template from a version that's greater or equals to 4.5.4, or from a version that's greater or equals to 0.6 release. 

### Steps

1. Open your old Virtual Assistant solution using Visual Studio
2. In the .csproj file, Change the project to a .net core app 3.0

```xml

  <PropertyGroup>
    <TargetFramework>netcoreapp3.0</TargetFramework>
    <NoWarn>NU1701</NoWarn>
  </PropertyGroup>

```

3. Add a few mandatory package references

```xml

    <PackageReference Include="Microsoft.AspNetCore.Mvc.Core" Version="2.2.2" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Formatters.Json" Version="2.1.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.NewtonsoftJson" Version="3.0.0" />

```
4. Update all the BotBuilder references to 4.7.2. If you have Microsoft.Bot.Builder.LanguageGeneration referenced, please use the version 4.7.2-preview

```xml

    <PackageReference Include="Microsoft.Bot.Builder" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Builder.AI.Luis" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Builder.AI.QnA" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Builder.ApplicationInsights" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Builder.Azure" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Builder.Dialogs" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Builder.Integration.ApplicationInsights.Core" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Builder.Integration.AspNet.Core" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Builder.LanguageGeneration" Version="4.7.2-preview" />
    <PackageReference Include="Microsoft.Bot.Configuration" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Connector" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Schema" Version="4.7.2" />

```

5. Change the Microsoft.Bot.Builder.Solutions package to Microsoft.Bot.Solutions with the version 0.8.0-preview. If your Virtual Assistant project has a reference to the package Microsoft.Bot.Builder.Skills, please remove it

```xml

    <PackageReference Include="Microsoft.Bot.Builder.Solutions->Microsoft.Bot.Solutions" Version="0.8.0-preview1" />

```

6. Under Adapters/DefaultAdapter.cs, add SetSpeakMiddleware into the middleware list of the adapter

```csharp

    Use(new SetSpeakMiddleware());

```

7. Add AllowedCallersClaimsValidator.cs under Authentication folder.

```csharp

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Solutions.Skills;

namespace {YourVirtualAssistant}.Authentication
{
    /// <summary>
    /// Sample claims validator that loads an allowed list from configuration if present
    /// and checks that responses are coming from configured skills.
    /// </summary>
    public class AllowedCallersClaimsValidator : ClaimsValidator
    {
        private readonly List<string> _allowedSkills;

        public AllowedCallersClaimsValidator(SkillsConfiguration skillsConfig)
        {
            if (skillsConfig == null)
            {
                throw new ArgumentNullException(nameof(skillsConfig));
            }

            // Load the appIds for the configured skills (we will only allow responses from skills we have configured).
            _allowedSkills = (from skill in skillsConfig.Skills.Values select skill.AppId).ToList();
        }

        public override Task ValidateClaimsAsync(IList<Claim> claims)
        {
            if (SkillValidation.IsSkillClaim(claims))
            {
                // Check that the appId claim in the skill request is in the list of skills configured for this bot.
                var appId = JwtTokenValidation.GetAppIdFromClaims(claims);
                if (!_allowedSkills.Contains(appId))
                {
                    throw new UnauthorizedAccessException($"Received a request from an application with an appID of \"{appId}\". To enable requests from this skill, add the skill to your configuration file.");
                }
            }

            return Task.CompletedTask;
        }
    }
}

8. Under Bots folder, change the existing IBot implementation to DefaultActivityHandler.cs 

```csharp

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using {YourVirtualAssistant}.Models;

namespace {YourVirtualAssistant}.Bots
{
    public class DefaultActivityHandler<T> : TeamsActivityHandler
        where T : Dialog
    {
        private readonly Dialog _dialog;
        private readonly BotState _conversationState;
        private readonly BotState _userState;
        private IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private IStatePropertyAccessor<UserProfileState> _userProfileState;
        private LocaleTemplateEngineManager _templateEngine;

        public DefaultActivityHandler(IServiceProvider serviceProvider, T dialog)
        {
            _dialog = dialog;
            _conversationState = serviceProvider.GetService<ConversationState>();
            _userState = serviceProvider.GetService<UserState>();
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            _userProfileState = _userState.CreateProperty<UserProfileState>(nameof(UserProfileState));
            _templateEngine = serviceProvider.GetService<LocaleTemplateEngineManager>();
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userProfileState.GetAsync(turnContext, () => new UserProfileState());

            if (string.IsNullOrEmpty(userProfile.Name))
            {
                // Send new user intro card.
                await turnContext.SendActivityAsync(_templateEngine.GenerateActivityForLocale("NewUserIntroCard", userProfile));
            }
            else
            {
                // Send returning user intro card.
                await turnContext.SendActivityAsync(_templateEngine.GenerateActivityForLocale("ReturningUserIntroCard", userProfile));
            }

            await _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        protected override Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        protected override Task OnTeamsSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            var ev = turnContext.Activity.AsEventActivity();
            var value = ev.Value?.ToString();

            switch (ev.Name)
            {
                case TokenEvents.TokenResponseEventName:
                    {
                        // Forward the token response activity to the dialog waiting on the stack.
                        await _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
                        break;
                    }

                default:
                    {
                        await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{ev.Name ?? "undefined"}' was received but not processed."));
                        break;
                    }
            }
        }
    }
}

```

9. Under Controllers folder, add a class SkillController.cs

```csharp

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;

namespace {YourVirtualAssistant}.Controllers
{
    /// <summary>
    /// A controller that handles skill replies to the bot.
    /// This example uses the <see cref="SkillHandler"/> that is registered as a <see cref="ChannelServiceHandler"/> in startup.cs.
    /// </summary>
    [Route("api/skills")]
    [ApiController]
    public class SkillController : ChannelServiceController
    {
        private ChannelServiceHandler _handler;

        public SkillController(ChannelServiceHandler handler)
            : base(handler)
        {
            _handler = handler;
        }

        /// <summary>
        /// ReplyToActivity.
        /// </summary>
        /// <param name="conversationId">Conversation ID.</param>
        /// <param name="activityId">activityId the reply is to (OPTIONAL).</param>
        /// <param name="activity">Activity to send.</param>
        /// <returns>TODO Document.</returns>
        [HttpPost("v3/conversations/{conversationId}/activities/{activityId}")]
        public override async Task<IActionResult> ReplyToActivityAsync(string conversationId, string activityId, [FromBody] Activity activity)
        {
            var result = await _handler.HandleReplyToActivityAsync(HttpContext.Request.Headers["Authorization"], conversationId, activityId, activity).ConfigureAwait(false);
            return new JsonResult(result);
        }

        /// <summary>
        /// SendToConversation.
        /// </summary>
        /// <param name="conversationId">Conversation ID.</param>
        /// <param name="activity">Activity to send.</param>
        /// <returns>TODO Document.</returns>
        [HttpPost("v3/conversations/{conversationId}/activities")]
        public override async Task<IActionResult> SendToConversationAsync(string conversationId, [FromBody] Activity activity)
        {
            var result = await _handler.HandleSendToConversationAsync(HttpContext.Request.Headers["Authorization"], conversationId, activity).ConfigureAwait(false);
            return new JsonResult(result);
        }
    }
}

```

10. Under Dialogs/MainDialog.cs, considering that you probably have local changes, please refer to the latest of MainDialog.cs: https://github.com/microsoft/botframework-solutions/blob/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Dialogs/MainDialog.cs to make corresponding changes.

11. In Startup.cs, add these changes

```csharp

    // Register the skills configuration class
    services.AddSingleton<SkillsConfiguration>();

    // Register AuthConfiguration to enable custom claim validation.
    services.AddSingleton(sp => new AuthenticationConfiguration { ClaimsValidator = new AllowedCallersClaimsValidator(sp.GetService<SkillsConfiguration>()) });
    
```

if you have added code to use SkillSwitchDialog in #10, please add this in Startup.cs

```csharp

    services.AddTransient<SwitchSkillDialog>();

```

change the IBot registration to

```csharp

    // Configure bot
    services.AddTransient<IBot, DefaultActivityHandler<MainDialog>>();

```

add these for skill capabilities

```csharp

    // Register the skills conversation ID factory, the client and the request handler.
    services.AddSingleton<SkillConversationIdFactoryBase, SkillConversationIdFactory>();
    services.AddHttpClient<SkillHttpClient>();
    services.AddSingleton<ChannelServiceHandler, SkillHandler>();

    // Register the SkillDialogs (remote skills).
    var section = Configuration?.GetSection("BotFrameworkSkills");
    var skills = section?.Get<EnhancedBotFrameworkSkill[]>();
    if (skills != null)
    {
        var hostEndpointSection = Configuration?.GetSection("SkillHostEndpoint");
        if (hostEndpointSection == null)
        {
            throw new ArgumentException($"{hostEndpointSection} is not in the configuration");
        }
        else
        {
            var hostEndpoint = new Uri(hostEndpointSection.Value);

            foreach (var skill in skills)
            {
                services.AddSingleton(sp =>
                {
                    return new SkillDialog(sp.GetService<ConversationState>(), sp.GetService<SkillHttpClient>(), skill, Configuration, hostEndpoint);
                });
            }
        }
    }

```