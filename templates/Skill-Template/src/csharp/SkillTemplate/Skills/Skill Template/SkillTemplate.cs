// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Telemetry;
using Microsoft.Bot.Configuration;
using $safeprojectname$.Dialogs.Main;
using $safeprojectname$.Dialogs.Main.Resources;
using $safeprojectname$.Dialogs.Sample.Resources;
using $safeprojectname$.Dialogs.Shared.Resources;
using $safeprojectname$.ServiceClients;

namespace $safeprojectname$
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class $safeprojectname$ : IBot
    {
        private readonly SkillConfigurationBase _services;
        private readonly EndpointService _endpointService;
        private readonly ResponseManager _responseManager;
        private readonly ConversationState _conversationState;
        private readonly ProactiveState _proactiveState;
        private readonly UserState _userState;
        private readonly IBotTelemetryClient _telemetryClient;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private IServiceManager _serviceManager;
        private DialogSet _dialogs;
        private bool _skillMode;

        public $safeprojectname$(SkillConfigurationBase services,
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
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
            _serviceManager = serviceManager ?? new ServiceManager();

            if (responseManager == null)
            {
                responseManager = new ResponseManager(
                    _services.LocaleConfigurations.Keys.ToArray(),
                    new MainResponses(),
                    new SharedResponses(),
                    new SampleResponses());
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