// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Skills;
using PointOfInterestSkill.Dialogs.Main;
using PointOfInterestSkill.ServiceClients;

namespace PointOfInterestSkill
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class PointOfInterestSkill : IBot
    {
        private readonly SkillConfigurationBase _services;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly IServiceManager _serviceManager;
        private readonly IBotTelemetryClient _telemetryClient;
        private DialogSet _dialogs;
        private bool _skillMode;

        public PointOfInterestSkill(SkillConfigurationBase services, ConversationState conversationState, UserState userState, IBotTelemetryClient telemetryClient, IServiceManager serviceManager = null, bool skillMode = false)
        {
            _skillMode = skillMode;
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _serviceManager = serviceManager ?? new ServiceManager();
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));

            _dialogs = new DialogSet(_conversationState.CreateProperty<DialogState>(nameof(DialogState)));
            _dialogs.Add(new MainDialog(_services, _conversationState, _userState, _telemetryClient, _serviceManager, _skillMode));
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