// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace RestaurantBooking
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::RestaurantBooking.Dialogs.Main;
    using global::RestaurantBooking.Dialogs.Main.Resources;
    using global::RestaurantBooking.Dialogs.Shared.Resources;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Solutions.Proactive;
    using Microsoft.Bot.Builder.Solutions.Responses;
    using Microsoft.Bot.Builder.Solutions.Skills;
    using Microsoft.Bot.Builder.Solutions.TaskExtensions;
    using Microsoft.Bot.Configuration;

    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class RestaurantBooking : IBot
    {
        private readonly SkillConfigurationBase _services;
        private readonly ResponseManager _responseManager;
        private readonly ConversationState _conversationState;
        private readonly UserState _userState;
        private readonly IBotTelemetryClient _telemetryClient;
        private IServiceManager _serviceManager;
        private DialogSet _dialogs;
        private bool _skillMode;
        private IHttpContextAccessor _httpContext;

        public RestaurantBooking(
            SkillConfigurationBase services,
            EndpointService endpointService,
            ConversationState conversationState,
            UserState userState,
            ProactiveState proactiveState,
            IBotTelemetryClient telemetryClient,
            IBackgroundTaskQueue backgroundTaskQueue,
            bool skillMode = false,
            ResponseManager responseManager = null,
            IServiceManager serviceManager = null,
            IHttpContextAccessor httpContext = null)
        {
            _skillMode = skillMode;
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            _serviceManager = serviceManager ?? new ServiceManager();
            _httpContext = httpContext;

            if (responseManager == null)
            {
                responseManager = new ResponseManager(
                    _services.LocaleConfigurations.Keys.ToArray(),
                    new RestaurantBookingSharedResponses(),
                    new RestaurantBookingMainResponses());
            }

            _responseManager = responseManager;
            _dialogs = new DialogSet(_conversationState.CreateProperty<DialogState>(nameof(DialogState)));
            _dialogs.Add(new MainDialog(_services, _responseManager, _conversationState, _userState, _telemetryClient, _serviceManager, _httpContext, _skillMode));
        }

        /// <summary>
        /// Run every turn of the conversation. Handles orchestration of messages.
        /// </summary>
        /// <param name="turnContext">Bot Turn Context.</param>
        /// <param name="cancellationToken">Task CancellationToken.</param>
        /// <returns>A <see cref="TaskItem"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
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