using HospitalitySkill.Models;
using HospitalitySkill.Services;
using HospitalitySkill.Responses.Reservation;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace HospitalitySkill.Dialogs
{
    public class ReservationDialog : HospitalityDialogBase
    {
        public ReservationDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ReservationDialog), settings, services, responseManager, conversationState, userState, telemetryClient)
        {
            var reservation = new WaterfallStep[]
            {
                CheckOut,
                CheckOutConfirmed
            };

            AddDialog(new WaterfallDialog(nameof(ReservationDialog), reservation));
            AddDialog(new ChoicePrompt(DialogIds.CheckOutPrompt, ValidateCheckOutAsync));
        }

        private async Task<DialogTurnResult> CheckOut(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState());
            
            if (state.LuisResult.TopIntent().intent == Luis.HospitalitySkillLuis.Intent.CheckOut)
            {
                var options = new PromptOptions()
                {
                    Choices = new List<Choice>(),
                };

                options.Choices.Add(new Choice("Yes"));
                options.Choices.Add(new Choice("No"));

                return await sc.PromptAsync(DialogIds.CheckOutPrompt, new PromptOptions()
                {
                    Prompt = ResponseManager.GetResponse(ReservationResponses.ConfirmCheckOut),
                    RetryPrompt = ResponseManager.GetResponse(ReservationResponses.RetryConfirmCheckOut),
                    Choices = options.Choices
                });
            }
            return null;
        }

        private async Task<bool> ValidateCheckOutAsync(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken) {

            var userState = await UserStateAccessor.GetAsync(promptContext.Context, () => new HospitalityUserSkillState());

            if (promptContext.Context.Responded && promptContext.Recognized.Value != null)
            {
                string response = promptContext.Recognized.Value.Value;
                if (response.Equals("Yes"))
                {
                    // set checkout value 
                    userState.CheckedOut = true;
                    return await Task.FromResult(true);
                }
                else if (response.Equals("No"))
                {
                    // if no just don't set state
                    return await Task.FromResult(true);
                }
            }
            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> CheckOutConfirmed(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState());
            var state = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState());

            if (state.LuisResult.TopIntent().intent == Luis.HospitalitySkillLuis.Intent.CheckOut)
            {
                if (userState.CheckedOut == true)
                {
                    // check out confirmation message
                }
                else
                {
                    // didn't check out, help with somethign else
                }
            } 
            return null;
        }

        private class DialogIds
        {
            public const string CheckOutPrompt = "checkOutPrompt";
        }
    }
}
