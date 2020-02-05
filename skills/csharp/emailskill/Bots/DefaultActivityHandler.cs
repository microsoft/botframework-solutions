// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Services;
using EmailSkill.Services.AzureMapsAPI;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EmailSkill.Bots
{
    public class DefaultActivityHandler<T> : ActivityHandler
        where T : Dialog
    {
        private readonly Dialog _dialog;
        private readonly BotState _conversationState;
        private readonly BotState _userState;
        private BotSettings _settings;
        private IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private IStatePropertyAccessor<EmailSkillState> _stateAccessor;

        public DefaultActivityHandler(IServiceProvider serviceProvider, T dialog)
        {
            _dialog = dialog;
            _conversationState = serviceProvider.GetService<ConversationState>();
            _userState = serviceProvider.GetService<UserState>();
            _settings = serviceProvider.GetService<BotSettings>();
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            _stateAccessor = _conversationState.CreateProperty<EmailSkillState>(nameof(EmailSkillState));
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        protected override Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            var ev = turnContext.Activity.AsEventActivity();
            var value = ev.Value?.ToString();

            switch (ev.Name)
            {
                case Events.TimezoneEvent:
                    {
                        var state = await _stateAccessor.GetAsync(turnContext, () => new EmailSkillState());
                        state.UserInfo.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(value);

                        break;
                    }

                case Events.LocationEvent:
                    {
                        var state = await _stateAccessor.GetAsync(turnContext, () => new EmailSkillState());

                        var azureMapsClient = new AzureMapsClient(_settings);
                        state.UserInfo.TimeZone = await azureMapsClient.GetTimeZoneInfoByCoordinates(value);

                        break;
                    }

                default:
                    {
                        await _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
                        break;
                    }
            }
        }

        private class Events
        {
            public const string TimezoneEvent = "Timezone";
            public const string LocationEvent = "Location";
        }
    }
}