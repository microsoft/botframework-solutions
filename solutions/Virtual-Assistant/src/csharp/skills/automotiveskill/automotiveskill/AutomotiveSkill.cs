// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::AutomotiveSkill.Dialogs.Main;
    using global::AutomotiveSkill.Dialogs.Main.Resources;
    using global::AutomotiveSkill.Dialogs.Shared.Resources;
    using global::AutomotiveSkill.Dialogs.VehicleSettings.Resources;
    using global::AutomotiveSkill.ServiceClients;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Configuration;
    using Microsoft.Bot.Solutions.Middleware.Telemetry;
    using Microsoft.Bot.Solutions.Models.Proactive;
    using Microsoft.Bot.Solutions.Responses;
    using Microsoft.Bot.Solutions.Skills;
    using Microsoft.Bot.Solutions.TaskExtensions;

    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class AutomotiveSkill : IBot
    {
        private readonly SkillConfigurationBase _services;
        private readonly EndpointService _endpointService;
        private readonly ResponseManager _responseManager;
        private readonly ConversationState _conversationState;
        private readonly UserState _userState;
        private readonly IBotTelemetryClient _telemetryClient;
        private IServiceManager _serviceManager;
        private DialogSet _dialogs;
        private bool _skillMode;
        private IHttpContextAccessor _httpContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomotiveSkill"/> class.
        /// </summary>
        /// <param name="services">Skill Configuration information.</param>
        /// <param name="endpointService">Endpoint service for the bot.</param>
        /// <param name="conversationState">Conversation State.</param>
        /// <param name="userState">User State.</param>
        /// <param name="proactiveState">Proative state.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <param name="backgroundTaskQueue">Background task queue.</param>
        /// <param name="serviceManager">Service Manager.</param>
        /// <param name="skillMode">Indicates whether the skill is running in skill or local mode.</param>
        /// <param name="responseManager">The responses for the bot.</param>
        /// <param name="httpContext">HttpContext accessor used to create relative URIs for images when in local mode.</param>
        public AutomotiveSkill(
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
            _endpointService = endpointService ?? throw new ArgumentNullException(nameof(endpointService));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));

            // If we are running in local-mode we need the HttpContext to create image file paths
            if (!skillMode)
            {
                _httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            }

            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _serviceManager = serviceManager ?? new ServiceManager();
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));

            if (responseManager == null)
            {
                var supportedLanguages = services.LocaleConfigurations.Keys.ToArray();
                responseManager = new ResponseManager(
                    new IResponseIdCollection[]
                    {
                        new AutomotiveSkillMainResponses(),
                        new AutomotiveSkillSharedResponses(),
                        new VehicleSettingsResponses(),
                    }, supportedLanguages);
            }

            _responseManager = responseManager;
            _dialogs = new DialogSet(_conversationState.CreateProperty<DialogState>(nameof(DialogState)));
            _dialogs.Add(new MainDialog(_services, _responseManager, _conversationState, _userState, _serviceManager, _httpContext, _telemetryClient, _skillMode));
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