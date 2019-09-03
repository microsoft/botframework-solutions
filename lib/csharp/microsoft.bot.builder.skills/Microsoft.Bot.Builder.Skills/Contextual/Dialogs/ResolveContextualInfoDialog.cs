using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills.Contextual.Models;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Microsoft.Recognizers.Text;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions.Resources;
using System.Linq;

namespace Microsoft.Bot.Builder.Skills.Contextual.Dialogs
{
    public class ResolveContextualInfoDialog : ComponentDialog
    {
        private const string _getContextualContactNameDialog = "GetContextualContactName";
        private const string _setContextualContactNameDialog = "SetContextualContactName";
        private const string _confirmContextualContactNameDialog = "ConfirmContextualContactName";
        private const string _textPrompt = "TextPrompt";
        private const string _confirmPrompt = "ConfirmPrompt";

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
            };

            var confirmContextualContactName = new WaterfallStep[]
            {
                ConfirmInfoFromUser,
                AfterConfirmInfoFromUser,
            };

            var setContextualContactName = new WaterfallStep[]
            {
                PromptIfUnknown,
                AfterPromptIfUnknown
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new TextPrompt(_textPrompt));
            AddDialog(new WaterfallDialog(_getContextualContactNameDialog, getContextualContactName) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(_setContextualContactNameDialog, setContextualContactName) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(_confirmContextualContactNameDialog, confirmContextualContactName) { TelemetryClient = telemetryClient });
            AddDialog(new ConfirmPrompt(_confirmPrompt, null, Culture.English) { Style = ListStyle.SuggestedAction });
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

                if (result == null || result.Count() == 0)
                {
                    return await sc.BeginDialogAsync(_setContextualContactNameDialog, option);
                }

                option.QueryResult = result;

                return await sc.BeginDialogAsync(_confirmContextualContactNameDialog, option);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> ConfirmInfoFromUser(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var option = sc.Options as UserInfoOptions;

                var nameString = string.Join(", ", option.QueryResult.ToArray().Take(option.QueryResult.Count - 1)) + string.Format(CommonStrings.SeparatorFormat, CommonStrings.And) + option.QueryResult.Last();
                var prompt = ResponseManager.GetResponse(
                    ResolveContextualInfoResponses.PromptUserContact,
                    new StringDictionary()
                    {
                        { "Relationship", option.QueryItem.RelationshipName },
                        { "Contact", nameString },
                    });
                return await sc.PromptAsync(_confirmPrompt, new PromptOptions { Prompt = prompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterConfirmInfoFromUser(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                var option = sc.Options as UserInfoOptions;
                if (confirmResult)
                {
                    return await sc.EndDialogAsync(option.QueryResult);
                }
                else
                {
                    return await sc.BeginDialogAsync(_setContextualContactNameDialog, option);
                }
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
                var option = sc.Options as UserInfoOptions;
                var prompt = ResponseManager.GetResponse(
                    ResolveContextualInfoResponses.PromptUnknownContact,
                    new StringDictionary()
                    {
                        { "Relationship", option.QueryItem.RelationshipName },
                    });

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

                    string[] split = { CommonStrings.And, CommonStrings.Comma };
                    var nameList = entityInput.Split(split, options: StringSplitOptions.None)
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();

                    userState.SaveRelationshipContact(option.QueryItem, nameList);

                    return await sc.EndDialogAsync(nameList);
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
