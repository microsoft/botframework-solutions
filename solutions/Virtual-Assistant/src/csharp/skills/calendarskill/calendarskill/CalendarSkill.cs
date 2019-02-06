// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.ChangeEventStatus.Resources;
using CalendarSkill.Dialogs.CreateEvent.Resources;
using CalendarSkill.Dialogs.JoinEvent.Resources;
using CalendarSkill.Dialogs.Main;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Dialogs.Summary.Resources;
using CalendarSkill.Dialogs.TimeRemaining.Resources;
using CalendarSkill.Dialogs.UpdateEvent.Resources;
using CalendarSkill.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;

namespace CalendarSkill
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class CalendarSkill : IBot
    {
        private readonly SkillConfigurationBase _services;
        private readonly ResponseManager _responseManager;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly IBotTelemetryClient _telemetryClient;
        private readonly IServiceManager _serviceManager;
        private readonly bool _skillMode;
        private DialogSet _dialogs;

        public CalendarSkill(SkillConfigurationBase services, ConversationState conversationState, UserState userState, IBotTelemetryClient telemetryClient, ResponseManager responseManager = null, IServiceManager serviceManager = null, bool skillMode = false)
        public CalendarSkill(SkillConfigurationBase services, ConversationState conversationState, UserState userState, IBotTelemetryClient telemetryClient, bool skillMode = false, IServiceManager serviceManager = null)
        {
            _skillMode = skillMode;
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _serviceManager = serviceManager ?? new ServiceManager(_services);
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));

            if (responseManager == null)
            {
                var supportedLanguages = services.LocaleConfigurations.Keys.ToArray();
                responseManager = new ResponseManager(
                    new IResponseIdCollection[]
                    {
                        new ChangeEventStatusResponses(),
                        new CreateEventResponses(),
                        new JoinEventResponses(),
                        new CalendarMainResponses(),
                        new CalendarSharedResponses(),
                        new SummaryResponses(),
                        new TimeRemainingResponses(),
                        new UpdateEventResponses(),
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