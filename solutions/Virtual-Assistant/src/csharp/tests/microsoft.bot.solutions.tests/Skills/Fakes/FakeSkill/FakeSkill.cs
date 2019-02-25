// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Skills;
using FakeSkill.Dialogs.Main;
using FakeSkill.ServiceClients;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Tests.Skills.Fakes.FakeSkill.Dialogs.Auth.Resources;
using Microsoft.Bot.Solutions.Tests.Skills.Fakes.FakeSkill.Dialogs.Main.Resources;
using Microsoft.Bot.Solutions.Tests.Skills.Fakes.FakeSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Solutions.Tests.Skills.Fakes.FakeSkill.Dialogs.Sample.Resources;
using Microsoft.Bot.Solutions.Models.Proactive;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.TaskExtensions;

namespace FakeSkill
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class FakeSkill : IBot
    {
        private readonly SkillConfigurationBase _services;
        private readonly ResponseManager _responseManager;
        private readonly ConversationState _conversationState;
        private readonly UserState _userState;
        private readonly ProactiveState _proactiveState;
        private readonly IBotTelemetryClient _telemetryClient;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly EndpointService _endpointService;
        private IServiceManager _serviceManager;
        private DialogSet _dialogs;
        private bool _skillMode;

        public FakeSkill(SkillConfigurationBase services, EndpointService endpointService, ConversationState conversationState, UserState userState, ProactiveState proactiveState, IBotTelemetryClient telemetryClient, IBackgroundTaskQueue backgroundTaskQueue, bool skillMode = false, ResponseManager responseManager = null, ServiceManager serviceManager = null)
        {
            _skillMode = skillMode;
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _proactiveState = proactiveState;
            _endpointService = endpointService;
            _backgroundTaskQueue = backgroundTaskQueue;
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            _serviceManager = serviceManager ?? new ServiceManager();

            if(responseManager == null)
            {
                responseManager = new ResponseManager(
                    new IResponseIdCollection[]
                    {
                                    new SampleAuthResponses(),
                                    new MainResponses(),
                                    new SharedResponses(),
                                    new SampleResponses()
                    },
                    new string[] { "en-us", "de-de", "es-es", "fr-fr", "it-it", "zh-cn" });
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