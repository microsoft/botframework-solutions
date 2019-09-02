using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills.Contextual.Models;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Skills.Contextual.Dialogs
{
    public class ResolveContextualInfoDialog : ComponentDialog
    {
        private const string _getContextualContactNameDialog = "GetContextualContactName";
        private const string _textPrompt = "TextPrompt";

        public ResolveContextualInfoDialog(
            UserState userState,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ResolveContextualInfoDialog))
         {
            TelemetryClient = telemetryClient;

            ResponseManager = new ResponseManager(
                new string[] { "en", "de", "es", "fr", "it", "zh" },
                new ResolveContextualInfoResponses());
            UserStateAccessor = userState.CreateProperty<UserInfoState>(nameof(UserInfoState));

            var getContextualContactName = new WaterfallStep[]
            {
                GetInfoFromUser,
                PromptIfUnknown,
                AfterPromptIfUnknown
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new TextPrompt(_textPrompt));
            AddDialog(new WaterfallDialog(_getContextualContactNameDialog, getContextualContactName) { TelemetryClient = telemetryClient });
            InitialDialogId = _getContextualContactNameDialog;
        }

        protected IStatePropertyAccessor<UserInfoState> UserStateAccessor { get; set; }

        protected ResponseManager ResponseManager { get; set; }

        public async Task<DialogTurnResult> GetInfoFromUser(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var option = sc.Options as UserInfoOptions;
                var userState = await UserStateAccessor.GetAsync(sc.Context, () => new UserInfoState());
                var result = userState.GetRelationshipContact(option.QueryItem);

                if (string.IsNullOrEmpty(result))
                {
                    return await sc.NextAsync();
                }

                return await sc.EndDialogAsync(result);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> PromptIfUnknown(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var prompt = ResponseManager.GetResponse(ResolveContextualInfoResponses.PromptUnknownContact);

                return await sc.PromptAsync(_textPrompt, new PromptOptions { Prompt = prompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterPromptIfUnknown(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var entity);
                    var entityInput = entity != null ? entity.ToString() : sc.Context.Activity.Text;

                    var userState = await UserStateAccessor.GetAsync(sc.Context);
                    var option = sc.Options as UserInfoOptions;
                    userState.SaveRelationshipContact(option.QueryItem, entityInput);

                    return await sc.EndDialogAsync(entityInput);
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            // send trace back to emulator
            var trace = new Microsoft.Bot.Schema.Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            var state = await UserStateAccessor.GetAsync(sc.Context);
            state.Clear();
        }
    }

}
