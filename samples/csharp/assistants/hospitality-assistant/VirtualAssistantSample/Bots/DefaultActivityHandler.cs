﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        private LocaleTemplateManager _templateManager;

        public DefaultActivityHandler(IServiceProvider serviceProvider, T dialog)
        {
            _dialog = dialog;
            _conversationState = serviceProvider.GetService<ConversationState>();
            _userState = serviceProvider.GetService<UserState>();
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            _userProfileState = _userState.CreateProperty<UserProfileState>(nameof(UserProfileState));
            _templateManager = serviceProvider.GetService<LocaleTemplateManager>();
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
                await turnContext.SendActivityAsync(_templateManager.GenerateActivityForLocale("NewUserIntroCard", userProfile));
            }
            else
            {
                // Send returning user intro card.
                await turnContext.SendActivityAsync(_templateManager.GenerateActivityForLocale("ReturningUserIntroCard", userProfile));
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

        protected override async Task OnEndOfConversationActivityAsync(ITurnContext<IEndOfConversationActivity> turnContext, CancellationToken cancellationToken)
        {
            await _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }
    }
}