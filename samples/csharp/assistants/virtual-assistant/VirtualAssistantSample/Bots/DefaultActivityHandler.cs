// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtualAssistantSample.Extensions;
using VirtualAssistantSample.Models;

namespace VirtualAssistantSample.Bots
{
    public class DefaultActivityHandler<T> : TeamsActivityHandler
        where T : Dialog
    {
        private readonly Dialog _dialog;
        private readonly BotState _conversationState;
        private readonly BotState _userState;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly IStatePropertyAccessor<UserProfileState> _userProfileState;
        private readonly LocaleTemplateManager _templateManager;
        private readonly SkillHttpClient _skillHttpClient;
        private readonly SkillsConfiguration _skillsConfig;
        private readonly IConfiguration _configuration;
        private readonly string _vaBotId;

        public DefaultActivityHandler(IServiceProvider serviceProvider, T dialog)
        {
            _dialog = dialog;
            _dialog.TelemetryClient = serviceProvider.GetService<IBotTelemetryClient>();
            _conversationState = serviceProvider.GetService<ConversationState>();
            _userState = serviceProvider.GetService<UserState>();
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            _userProfileState = _userState.CreateProperty<UserProfileState>(nameof(UserProfileState));
            _templateManager = serviceProvider.GetService<LocaleTemplateManager>();
            _skillHttpClient = serviceProvider.GetService<SkillHttpClient>();
            _skillsConfig = serviceProvider.GetService<SkillsConfiguration>();
            _configuration = serviceProvider.GetService<IConfiguration>();
            _vaBotId = _configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value;
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
            var userProfile = await _userProfileState.GetAsync(turnContext, () => new UserProfileState(), cancellationToken);

            if (string.IsNullOrEmpty(userProfile.Name))
            {
                // Send new user intro card.
                await turnContext.SendActivityAsync(_templateManager.GenerateActivityForLocale("NewUserIntroCard", userProfile), cancellationToken);
            }
            else
            {
                // Send returning user intro card.
                await turnContext.SendActivityAsync(_templateManager.GenerateActivityForLocale("ReturningUserIntroCard", userProfile), cancellationToken);
            }

            await _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        protected override Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // directline speech occasionally sends empty message activities that should be ignored
            var activity = turnContext.Activity;
            if (activity.ChannelId == Channels.DirectlineSpeech && activity.Type == ActivityTypes.Message && string.IsNullOrEmpty(activity.Text))
            {
                return Task.CompletedTask;
            }

            return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        protected override Task OnTeamsSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            var ev = turnContext.Activity.AsEventActivity();

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
                        await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{ev.Name ?? "undefined"}' was received but not processed."), cancellationToken);
                        break;
                    }
            }
        }

        // Invoked when a "task/fetch" event is received to invoke task module.
        protected override async Task<TaskModuleResponse> OnTeamsTaskModuleFetchAsync(ITurnContext<IInvokeActivity> turnContext, TaskModuleRequest taskModuleRequest, CancellationToken cancellationToken)
        {
            try
            {
                string skillId = (turnContext.Activity as Activity).GetSkillId();
                var skill = _skillsConfig.Skills.Where(s => s.Key == skillId).FirstOrDefault().Value;

                // Forward request to correct skill 
                var invokeResponse = await _skillHttpClient.PostActivityAsync(_vaBotId, skill, _skillsConfig.SkillHostEndpoint, turnContext.Activity as Activity, cancellationToken);
                return invokeResponse.GetTaskModuleRespose();
            }
            catch (Exception)
            {
                await turnContext.SendActivityAsync(_templateManager.GenerateActivityForLocale("ErrorMessage"));
                return null;
            }
        }

        // Invoked when a 'task/submit' invoke activity is received for task module submit actions. 
        protected override async Task<TaskModuleResponse> OnTeamsTaskModuleSubmitAsync(ITurnContext<IInvokeActivity> turnContext, TaskModuleRequest taskModuleRequest, CancellationToken cancellationToken)
        {
            try
            {
                string skillName = (turnContext.Activity as Activity).GetSkillId();
                var skill = _skillsConfig.Skills.Where(s => s.Key == skillName).FirstOrDefault().Value;

                // Forward request to correct skill 
                var invokeResponse = await _skillHttpClient.PostActivityAsync(_vaBotId, skill, _skillsConfig.SkillHostEndpoint, turnContext.Activity as Activity, cancellationToken).ConfigureAwait(false);
                return invokeResponse.GetTaskModuleRespose();
            }
            catch (Exception)
            {
                await turnContext.SendActivityAsync(_templateManager.GenerateActivityForLocale("ErrorMessage"));
                return null;
            }
        }

        protected override async Task OnEndOfConversationActivityAsync(ITurnContext<IEndOfConversationActivity> turnContext, CancellationToken cancellationToken)
        {
            await _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }
    }
}