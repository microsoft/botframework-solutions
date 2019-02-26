// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Models.Proactive;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Extensions.DependencyInjection;
using VirtualAssistant.Dialogs.Escalate;
using VirtualAssistant.Dialogs.Main;
using VirtualAssistant.Dialogs.Onboarding;
using VirtualAssistant.Dialogs.Shared;

namespace VirtualAssistant
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class VirtualAssistant : IBot
    {
        private readonly BotServices _services;
        private readonly ConversationState _conversationState;
        private readonly UserState _userState;
        private readonly ProactiveState _proactiveState;
        private readonly EndpointService _endpointService;
        private readonly IBotTelemetryClient _telemetryClient;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private DialogSet _dialogs;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualAssistant"/> class.
        /// </summary>
        /// <param name="serviceCollection">Service collection.</param>
        /// <param name="serviceProvider">Service provider.</param>
        public VirtualAssistant(IServiceCollection serviceCollection, IServiceProvider serviceProvider)
        {
            _conversationState = serviceProvider.GetService<ConversationState>() ?? throw new ArgumentNullException(nameof(ConversationState));
            _userState = serviceProvider.GetService<UserState>() ?? throw new ArgumentNullException(nameof(UserState));
            _proactiveState = serviceProvider.GetService<ProactiveState>() ?? throw new ArgumentNullException(nameof(ProactiveState));
            _services = serviceProvider.GetService<BotServices>() ?? throw new ArgumentNullException(nameof(BotServices));
            _endpointService = serviceProvider.GetService<EndpointService>() ?? throw new ArgumentNullException(nameof(EndpointService));
            _telemetryClient = serviceProvider.GetService<IBotTelemetryClient>() ?? throw new ArgumentNullException(nameof(IBotTelemetryClient));
            _backgroundTaskQueue = serviceProvider.GetService<IBackgroundTaskQueue>() ?? throw new ArgumentNullException(nameof(IBackgroundTaskQueue));

            RegisterDialogs(serviceCollection);

            _dialogs = new DialogSet(_conversationState.CreateProperty<DialogState>(nameof(VirtualAssistant)));
            _dialogs.Add(new MainDialog(serviceProvider));
        }

        /// <summary>
        /// Run every turn of the conversation. Handles orchestration of messages.
        /// </summary>
        /// <param name="turnContext">Bot Turn Context.</param>
        /// <param name="cancellationToken">Task CancellationToken.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Client notifying this bot took to long to respond (timed out)
            if (turnContext.Activity.Code == EndOfConversationCodes.BotTimedOut)
            {
                _services.TelemetryClient.TrackTrace($"Timeout in {turnContext.Activity.ChannelId} channel: Bot took too long to respond.");
                return;
            }

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

        private void RegisterDialogs(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<OnboardingDialog>();
            serviceCollection.AddSingleton<EnterpriseDialog>();
            serviceCollection.AddSingleton<EscalateDialog>();
        }
    }
}