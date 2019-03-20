// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.DailyBrief.Resources;
using EmailSkill.Dialogs.DeleteEmail.Resources;
using EmailSkill.Dialogs.FindContact.Resources;
using EmailSkill.Dialogs.ForwardEmail.Resources;
using EmailSkill.Dialogs.Main;
using EmailSkill.Dialogs.Main.Resources;
using EmailSkill.Dialogs.ReplyEmail.Resources;
using EmailSkill.Dialogs.SendEmail.Resources;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Dialogs.ShowEmail.Resources;
using EmailSkill.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Telemetry;
using Microsoft.Bot.Configuration;

namespace EmailSkill
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class EmailSkill : IBot
    {
        private readonly SkillConfigurationBase _services;
        private readonly EndpointService _endpointService;
        private readonly ResponseManager _responseManager;
        private readonly ConversationState _conversationState;
        private readonly UserState _userState;
        private readonly ProactiveState _proactiveState;
        private readonly IBotTelemetryClient _telemetryClient;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly ScheduledTask _scheduledTask;
        private IServiceManager _serviceManager;
        private DialogSet _dialogs;
        private bool _skillMode;

        public EmailSkill(
            SkillConfigurationBase services,
            EndpointService endpointService,
            ConversationState conversationState,
            UserState userState,
            ProactiveState proactiveState,
            IBotTelemetryClient telemetryClient,
            IBackgroundTaskQueue backgroundTaskQueue,
            bool skillMode = false,
            ScheduledTask scheduledTask = null,
            ResponseManager responseManager = null,
            IServiceManager serviceManager = null)
        {
            _skillMode = skillMode;
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _endpointService = endpointService ?? throw new ArgumentNullException(nameof(endpointService));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _proactiveState = proactiveState ?? throw new ArgumentNullException(nameof(proactiveState));
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            _serviceManager = serviceManager ?? new ServiceManager(services);
            _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
            _scheduledTask = scheduledTask;

            if (responseManager == null)
            {
                var supportedLanguages = services.LocaleConfigurations.Keys.ToArray();
                responseManager = new ResponseManager(
                    supportedLanguages,
                    new FindContactResponses(),
                    new DeleteEmailResponses(),
                    new ForwardEmailResponses(),
                    new EmailMainResponses(),
                    new ReplyEmailResponses(),
                    new SendEmailResponses(),
                    new EmailSharedResponses(),
                    new ShowEmailResponses(),
                    new DailyBriefResponses());
            }

            _responseManager = responseManager;
            _dialogs = new DialogSet(_conversationState.CreateProperty<DialogState>(nameof(DialogState)));
            _dialogs.Add(new MainDialog(_services, _endpointService, _responseManager, _conversationState, _userState, _proactiveState, _telemetryClient, _backgroundTaskQueue, _serviceManager, _skillMode, _scheduledTask));
        }

        /// <summary>
        /// Run every turn of the conversation. Handles orchestration of messages.
        /// </summary>
        /// <param name="turnContext">Bot Turn Context.</param>
        /// <param name="cancellationToken">Task CancellationToken.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            turnContext.TurnState.TryAdd(TelemetryLoggerMiddleware.AppInsightsServiceKey, _telemetryClient);

            var dc = await _dialogs.CreateContextAsync(turnContext);

            if (dc.ActiveDialog != null)
            {
                var result = await dc.ContinueDialogAsync();
            }
            else
            {
                await dc.BeginDialogAsync(nameof(MainDialog));
            }
        }
    }
}