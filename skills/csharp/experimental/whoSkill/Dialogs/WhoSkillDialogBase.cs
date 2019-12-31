using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using WhoSkill.Models;
using WhoSkill.Services;
using WhoSkill.Utilities;

namespace WhoSkill.Dialogs
{
    public class WhoSkillDialogBase : ComponentDialog
    {

        public WhoSkillDialogBase(
            string dialogId,
            BotSettings settings,
            ConversationState conversationState,
            MSGraphService msGraphService,
            //LocaleTemplateEngineManager localeTemplateEngineManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(dialogId)
        {
            // Initialize state accessor
            WhoStateAccessor = conversationState.CreateProperty<WhoSkillState>(nameof(WhoSkillState));
            MSGraphService = msGraphService;
            TelemetryClient = telemetryClient;

            AddDialog(new MultiProviderAuthDialog(settings.OAuthConnections, appCredentials));
            AddDialog(new TextPrompt(Actions.Prompt));
            AddDialog(new ConfirmPrompt(Actions.ConfirmPrompt, null, Culture.English) { Style = ListStyle.SuggestedAction });
        }

        public IStatePropertyAccessor<WhoSkillState> WhoStateAccessor { get; set; }

        public MSGraphService MSGraphService { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            await InitState(dc);
            await FillLuisResultIntoState(dc);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await FillLuisResultIntoState(dc);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        // Shared steps
        protected async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var retryPrompt = MessageFactory.Text("autho");
                return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions() { RetryPrompt = retryPrompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // Shared steps
        protected async Task<DialogTurnResult> AfterGetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (sc.Result is ProviderTokenResponse providerTokenResponse)
                {

                    if (sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token))
                    {
                        sc.Context.TurnState[StateProperties.APIToken] = providerTokenResponse.TokenResponse.Token;
                    }
                    else
                    {
                        sc.Context.TurnState.Add(StateProperties.APIToken, providerTokenResponse.TokenResponse.Token);
                    }
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // Shared steps
        protected async Task<DialogTurnResult> InitService(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await WhoStateAccessor.GetAsync(sc.Context);
                sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token);
                MSGraphService.InitAsync(token as string);
                return await sc.NextAsync();
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
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            //var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.ToDoErrorMessage);
            //await sc.Context.SendActivityAsync(activity);

            // clear state
            var state = await WhoStateAccessor.GetAsync(sc.Context);
            state.Init();
        }

        protected async Task InitState(DialogContext dc)
        {
            try
            {
                var state = await WhoStateAccessor.GetAsync(dc.Context);
                state.Init();
            }
            catch
            {
            }
        }

        private async Task FillLuisResultIntoState(DialogContext dc)
        {
            try
            {
                var state = await WhoStateAccessor.GetAsync(dc.Context);
                var luisResult = dc.Context.TurnState.Get<WhoLuis>(StateProperties.WhoLuisResultKey);
                var generalLuisResult = dc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var entities = luisResult.Entities;
                var generalEntities = generalLuisResult.Entities;

                if (generalEntities != null)
                {
                    if (generalEntities.number != null && generalEntities.number.Length > 0)
                    {
                        var indexOfNumber = (int)generalEntities.number[0];
                        state.Ordinal = indexOfNumber;
                    }
                }

                if (entities != null)
                {
                    if (entities.PersonName != null && !string.IsNullOrEmpty(entities.PersonName[0]))
                    {
                        state.PersonName = entities.PersonName[0];
                    }

                    if (entities.ordinal != null && entities.ordinal.Length > 0)
                    {
                        var indexOfOrdinal = (int)entities.ordinal[0];
                        state.Ordinal = indexOfOrdinal;
                    }
                }
            }
            catch
            {
            }
        }
    }
}
