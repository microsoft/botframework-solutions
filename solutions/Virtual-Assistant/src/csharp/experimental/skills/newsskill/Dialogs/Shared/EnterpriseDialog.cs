// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Skills;

namespace NewsSkill
{
    public class EnterpriseDialog : InterruptableDialog
    {
        protected const string LuisResultKey = "LuisResult";

        // Fields
        private readonly SkillConfiguration _services;

        public EnterpriseDialog(SkillConfiguration botServices, string dialogId)
            : base(dialogId)
        {
            _services = botServices;
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            // check luis intent
            _services.LocaleConfigurations["en"].LuisServices.TryGetValue("general", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var luisResult = await luisService.RecognizeAsync<General>(dc.Context, cancellationToken);
                var intent = luisResult.TopIntent().intent;

                // Add the luis result (intent and entities) for further processing in the derived dialog
                dc.Context.TurnState.Add(LuisResultKey, luisResult);

                switch (intent)
                {
                    case General.Intent.Cancel:
                        {
                            return await OnCancel(dc);
                        }

                    case General.Intent.Help:
                        {
                            return await OnHelp(dc);
                        }
                }
            }

            return InterruptionAction.NoAction;
        }

        protected virtual async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            return InterruptionAction.NoAction;
        }

        protected virtual async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            var view = new MainResponses();
            await view.ReplyWith(dc.Context, MainResponses.Help);

            // Signal the conversation was interrupted and should immediately continue
            return InterruptionAction.MessageSentToUser;
        }
    }
}
