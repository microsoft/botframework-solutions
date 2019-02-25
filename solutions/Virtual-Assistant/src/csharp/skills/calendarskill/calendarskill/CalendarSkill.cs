// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.ChangeEventStatus.Resources;
using CalendarSkill.Dialogs.CreateEvent.Resources;
using CalendarSkill.Dialogs.FindContact.Resources;
using CalendarSkill.Dialogs.JoinEvent.Resources;
using CalendarSkill.Dialogs.Main;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Dialogs.Summary.Resources;
using CalendarSkill.Dialogs.TimeRemaining.Resources;
using CalendarSkill.Dialogs.UpcomingEvent.Resources;
using CalendarSkill.Dialogs.UpdateEvent.Resources;
using CalendarSkill.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Models.Proactive;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.TaskExtensions;

namespace CalendarSkill
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class CalendarSkill : IBot
    {
        private readonly SkillConfigurationBase _services;
        private readonly EndpointService _endpointService;
        private readonly ResponseManager _responseManager;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly ProactiveState _proactiveState;
        private readonly IBotTelemetryClient _telemetryClient;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly IServiceManager _serviceManager;
        private readonly bool _skillMode;
        private DialogSet _dialogs;

        public CalendarSkill(
            SkillConfigurationBase services,
            EndpointService endpointService,
            ConversationState conversationState,
            UserState userState,
            ProactiveState proactiveState,
            IBotTelemetryClient telemetryClient,
            IBackgroundTaskQueue backgroundTaskQueue,
            bool skillMode = false,
            ResponseManager responseManager = null,
            IServiceManager serviceManager = null)
        {
            _skillMode = skillMode;
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _endpointService = endpointService ?? throw new ArgumentNullException(nameof(endpointService));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _proactiveState = proactiveState ?? throw new ArgumentNullException(nameof(proactiveState));
            _serviceManager = serviceManager ?? new ServiceManager(_services);
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));

            if (responseManager == null)
            {
                var supportedLanguages = services.LocaleConfigurations.Keys.ToArray();
                responseManager = new ResponseManager(
                    new IResponseIdCollection[]
                    {
                        new FindContactResponses(),
                        new ChangeEventStatusResponses(),
                        new CreateEventResponses(),
                        new JoinEventResponses(),
                        new CalendarMainResponses(),
                        new CalendarSharedResponses(),
                        new SummaryResponses(),
                        new TimeRemainingResponses(),
                        new UpdateEventResponses(),
                        new UpcomingEventResponses()
                    }, supportedLanguages);
            }

            _responseManager = responseManager;
            _dialogs = new DialogSet(_conversationState.CreateProperty<DialogState>(nameof(DialogState)));
            _dialogs.Add(new MainDialog(_services, _endpointService, _responseManager, _conversationState, _userState, _proactiveState, _telemetryClient, _backgroundTaskQueue, _serviceManager, _skillMode));
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