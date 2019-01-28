// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::AutomotiveSkill.Dialogs.Main;
    using global::AutomotiveSkill.ServiceClients;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Solutions.Skills;

    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class AutomotiveSkill : IBot
    {
        private readonly SkillConfigurationBase _services;
        private readonly ConversationState _conversationState;
        private readonly UserState _userState;
        private readonly IBotTelemetryClient _telemetryClient;
        private IServiceManager _serviceManager;
        private DialogSet _dialogs;
        private bool _skillMode;
        private IHttpContextAccessor _httpContext;

        public AutomotiveSkill(SkillConfigurationBase services, ConversationState conversationState, UserState userState, IBotTelemetryClient telemetryClient, ServiceManager serviceManager = null, IHttpContextAccessor httpContext = null, bool skillMode = false)
        {
            _skillMode = skillMode;
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _serviceManager = serviceManager ?? new ServiceManager();
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));

            _dialogs = new DialogSet(_conversationState.CreateProperty<DialogState>(nameof(DialogState)));
            _dialogs.Add(new MainDialog(_services, _conversationState, _userState, _serviceManager, _httpContext, _telemetryClient, _skillMode));
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