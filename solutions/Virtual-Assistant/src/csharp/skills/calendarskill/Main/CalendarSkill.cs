// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace CalendarSkill
{
    public class CalendarSkill : IBot
    {
        private bool _skillMode = false;
        private CalendarSkillAccessors _accessors;
        private CalendarSkillServices _services;
        private IServiceManager _serviceManager;
        private DialogSet _dialogs;

        // Skill Mode Constructor
        public CalendarSkill(BotState botState, string stateName = null, Dictionary<string, string> configuration = null)
        {
            // Flag that can be used for Skill specific behaviour (if needed)
            _skillMode = true;
            _serviceManager = new ServiceManager();

            // Create the properties and populate the Accessors. It's OK to call it DialogState as Skill mode creates an isolated area for this Skill so it doesn't conflict with Parent or other skills
            _accessors = new CalendarSkillAccessors
            {
                CalendarSkillState = botState.CreateProperty<CalendarSkillState>(stateName ?? nameof(CalendarSkillState)),
                ConversationDialogState = botState.CreateProperty<DialogState>("DialogState"),
            };

            // Initialise dialogs
            _dialogs = new DialogSet(_accessors.ConversationDialogState);

            if (configuration != null)
            {
                configuration.TryGetValue("LuisAppId", out var luisAppId);
                configuration.TryGetValue("LuisSubscriptionKey", out var luisSubscriptionKey);
                configuration.TryGetValue("LuisEndpoint", out var luisEndpoint);

                if (!string.IsNullOrEmpty(luisAppId) && !string.IsNullOrEmpty(luisSubscriptionKey) && !string.IsNullOrEmpty(luisEndpoint))
                {
                    var luisApplication = new LuisApplication(luisAppId, luisSubscriptionKey, luisEndpoint);

                    _services = new CalendarSkillServices
                    {
                        LuisRecognizer = new LuisRecognizer(luisApplication),
                    };
                }
            }

            _dialogs.Add(new RootDialog(_skillMode, _services, _accessors, _serviceManager));
        }

        // Local Mode Constructor
        public CalendarSkill(CalendarSkillServices services, CalendarSkillAccessors calendarBotAccessors, IServiceManager serviceManager)
        {
            _accessors = calendarBotAccessors;
            _serviceManager = serviceManager;
            _dialogs = new DialogSet(_accessors.ConversationDialogState);
            _services = services;

            _dialogs.Add(new RootDialog(_skillMode, _services, _accessors, _serviceManager));
        }

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
                            await dc.BeginDialogAsync(nameof(RootDialog));
                        }
                    }
                }
                else
                {
                    // if skillMode, begin dialog
                    await dc.BeginDialogAsync(nameof(RootDialog));
                }
            }
        }
    }
}
