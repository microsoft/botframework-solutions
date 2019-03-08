// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace RestaurantBooking
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Configuration;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Solutions.Proactive;
    using Microsoft.Bot.Solutions.Responses;
    using Microsoft.Bot.Solutions.Skills;
    using Microsoft.Bot.Solutions.TaskExtensions;

    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class RestaurantBooking : IBot
    {
        private readonly SkillConfigurationBase _services;
        private readonly ConversationState _conversationState;
        private readonly IBotTelemetryClient _telemetryClient;
        private readonly UserState _userState;
        private bool _skillMode;
        private IServiceManager _serviceManager;
        private IHttpContextAccessor _httpContext;
        private DialogSet _dialogs;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestaurantBooking"/> class.
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
            _httpContext = httpContext;
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _serviceManager = serviceManager ?? new ServiceManager();
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));

            _dialogs = new DialogSet(_conversationState.CreateProperty<DialogState>(nameof(DialogState)));
            _dialogs.Add(new MainDialog(_services, responseManager, _conversationState, _userState, _serviceManager, httpContext, _telemetryClient, _skillMode));
        }

        /// <summary>
        /// Run every turn of the conversation. Handles orchestration of messages.
        /// </summary>
        /// <param name="turnContext">Bot Turn Context.</param>
        /// <param name="cancellationToken">Task CancellationToken.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var dc = await _dialogs.CreateContextAsync(turnContext);
            var result = await dc.ContinueDialogAsync();

            if (result.Status == DialogTurnStatus.Empty)
            {
                if (!_skillMode)
                {
                    // if localMode, check for conversation update from user before starting dialog
                    if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
                    {
                        var activity = turnContext.Activity.AsConversationUpdateActivity();

                        // if conversation update is not from the bot.
                        if (!activity.MembersAdded.Any(m => m.Id == activity.Recipient.Id))
                        {
                            await dc.BeginDialogAsync(nameof(MainDialog));
                        }
                    }
                }
                else
                {
                    // if skillMode, begin dialog
                    await dc.BeginDialogAsync(nameof(MainDialog));
                }
            }
        }
    }
}