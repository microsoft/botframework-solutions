using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.CheckOut;
using HospitalitySkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions.Responses;

namespace HospitalitySkill.Dialogs
{
    public class CheckOutDialog : HospitalityDialogBase
    {
        public CheckOutDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            IBotTelemetryClient telemetryClient)
            : base(nameof(CheckOutDialog), settings, services, responseManager, conversationState, userState, telemetryClient)
        {
            var checkout = new WaterfallStep[]
            {
                CheckOutPrompt,
                CheckOutConfirmed
            };

            AddDialog(new WaterfallDialog(nameof(CheckOutDialog), checkout));
            AddDialog(new ChoicePrompt(DialogIds.CheckOutPrompt, ValidateCheckOutAsync));
            AddDialog(new TextPrompt(DialogIds.ConfirmCheckOutMessage));
            AddDialog(new TextPrompt(DialogIds.ThankYou));
            AddDialog(new TextPrompt(DialogIds.HelpOtherwise));
        }

        private async Task<DialogTurnResult> CheckOutPrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState());

            var options = new PromptOptions()
            {
                Choices = new List<Choice>(),
            };

            options.Choices.Add(new Choice("Yes"));
            options.Choices.Add(new Choice("No"));

            return await sc.PromptAsync(DialogIds.CheckOutPrompt, new PromptOptions()
            {
                Prompt = ResponseManager.GetResponse(CheckOutResponses.ConfirmCheckOut),
                RetryPrompt = ResponseManager.GetResponse(CheckOutResponses.RetryConfirmCheckOut),
                Choices = options.Choices
            });
        }

        private async Task<bool> ValidateCheckOutAsync(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(promptContext.Context, () => new HospitalityUserSkillState());

            if (promptContext.Recognized.Succeeded && promptContext.Recognized.Value != null)
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

            if (userState.CheckedOut == true)
            {
                // check out confirmation message
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CheckOutResponses.CheckOutMessage));
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CheckOutResponses.ThankYou));

                // if user is checked out shouldn't be allowed to do anything else
            }
            else
            {
                // didn't check out, help with something else
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CheckOutResponses.HelpOtherwise));
            }

            return await sc.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string CheckOutPrompt = "checkOutPrompt";
            public const string ConfirmCheckOutMessage = "confirmCheckOutMessage";
            public const string ThankYou = "thankYou";
            public const string HelpOtherwise = "helpOtherwise";
        }
    }
}
