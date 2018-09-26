// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace PointOfInterestSkill
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class PointOfInterestSkill : IBot
    {
        private bool _skillMode = false;
        private PointOfInterestSkillAccessors _accessors;
        private PointOfInterestSkillServices _services;
        private IServiceManager _serviceManager;
        private DialogSet _dialogs;
        private Dictionary<string, string> _configuration;

        // Skill Mode Constructor
        public PointOfInterestSkill(BotState botState, string stateName = null, Dictionary<string, string> configuration = null)
        {
            // Flag that can be used for Skill specific behaviour (if needed)
            _skillMode = true;
            _serviceManager = new ServiceManager();
            _configuration = configuration;
            _services = new PointOfInterestSkillServices();

            // Create the properties and populate the Accessors. It's OK to call it DialogState as Skill mode creates an isolated area for this Skill so it doesn't conflict with Parent or other skills
            _accessors = new PointOfInterestSkillAccessors
            {
                PointOfInterestSkillState = botState.CreateProperty<PointOfInterestSkillState>(stateName ?? nameof(PointOfInterestSkillState)),
                ConversationDialogState = botState.CreateProperty<DialogState>("DialogState"),
            };

            // Initialise dialogs
            _dialogs = new DialogSet(_accessors.ConversationDialogState);

            if (configuration != null)
            {
                configuration.TryGetValue("AzureMapsKey", out var azureMapsKey);

                if (!string.IsNullOrEmpty(azureMapsKey))
                {
                    _services.AzureMapsKey = azureMapsKey;
                }
            }

            _dialogs.Add(new RootDialog(_skillMode, _services, _accessors, _serviceManager));
        }

        // Local Mode Constructor
        public PointOfInterestSkill(PointOfInterestSkillServices services, PointOfInterestSkillAccessors pointOfInterestSkillAccessors, IServiceManager serviceManager)
        {
            _accessors = pointOfInterestSkillAccessors;
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
