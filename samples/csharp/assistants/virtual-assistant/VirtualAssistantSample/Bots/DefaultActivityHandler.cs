// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using VirtualAssistantSample.Models;

namespace VirtualAssistantSample.Bots
{
    public class DefaultActivityHandler<T> : TeamsActivityHandler
        where T : Dialog
    {
        private readonly Dialog _dialog;
        private readonly BotState _conversationState;
        private readonly BotState _userState;
        private IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private IStatePropertyAccessor<UserProfileState> _userProfileState;
        private IStatePropertyAccessor<SkillContext> _skillContext;
        private LocaleTemplateEngineManager _templateEngine;

        public DefaultActivityHandler(IServiceProvider serviceProvider, T dialog)
        {
            _dialog = dialog;
            _conversationState = serviceProvider.GetService<ConversationState>();
            _userState = serviceProvider.GetService<UserState>();
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            _userProfileState = _userState.CreateProperty<UserProfileState>(nameof(UserProfileState));
            _skillContext = _userState.CreateProperty<SkillContext>(nameof(SkillContext));
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
                case Events.Location:
                    {
                        var locationObj = new JObject();
                        locationObj.Add(StateProperties.Location, JToken.FromObject(value));

                        // Store location for use by skills.
                        var skillContext = await _skillContext.GetAsync(turnContext, () => new SkillContext());
                        skillContext[StateProperties.Location] = locationObj;
                        await _skillContext.SetAsync(turnContext, skillContext);

                        break;
                    }

                case Events.TimeZone:
                    {
                        try
                        {
                            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(value);
                            var timeZoneObj = new JObject();
                            timeZoneObj.Add(StateProperties.TimeZone, JToken.FromObject(timeZoneInfo));

                            // Store location for use by skills.
                            var skillContext = await _skillContext.GetAsync(turnContext, () => new SkillContext());
                            skillContext[StateProperties.TimeZone] = timeZoneObj;
                            await _skillContext.SetAsync(turnContext, skillContext);
                        }
                        catch
                        {
                            await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Received time zone could not be parsed. Property not set."));
                        }

                        break;
                    }

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

        private class Events
        {
            public const string Location = "VA.Location";
            public const string TimeZone = "VA.Timezone";
        }
    }
}
