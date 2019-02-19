// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Models.Proactive;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.TaskExtensions;
using PointOfInterestSkill.Dialogs.CancelRoute.Resources;
using PointOfInterestSkill.Dialogs.FindPointOfInterest.Resources;
using PointOfInterestSkill.Dialogs.Main;
using PointOfInterestSkill.Dialogs.Main.Resources;
using PointOfInterestSkill.Dialogs.Route.Resources;
using PointOfInterestSkill.Dialogs.Shared.Resources;
using PointOfInterestSkill.ServiceClients;

namespace PointOfInterestSkill
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class PointOfInterestSkill : IBot
    {
        private readonly SkillConfigurationBase _services;
        private readonly ResponseManager _responseManager;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly IServiceManager _serviceManager;
        private readonly IBotTelemetryClient _telemetryClient;
        private DialogSet _dialogs;
        private bool _skillMode;

        public PointOfInterestSkill(
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
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _serviceManager = serviceManager ?? new ServiceManager();
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));

            if (responseManager == null)
            {
                var supportedLanguages = services.LocaleConfigurations.Keys.ToArray();
                responseManager = new ResponseManager(
                    new IResponseIdCollection[]
                    {
                        new CancelRouteResponses(),
                        new FindPointOfInterestResponses(),
                        new POIMainResponses(),
                        new RouteResponses(),
                        new POISharedResponses(),
                    }, supportedLanguages);
            }

            _responseManager = responseManager;
            _dialogs = new DialogSet(_conversationState.CreateProperty<DialogState>(nameof(DialogState)));
            _dialogs.Add(new MainDialog(_services, _responseManager, _conversationState, _userState, _telemetryClient, _serviceManager, _skillMode));
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