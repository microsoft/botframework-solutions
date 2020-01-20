using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using WhoSkill.Models;
using WhoSkill.Responses.Org;
using WhoSkill.Responses.Shared;
using WhoSkill.Responses.WhoIs;
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
            LocaleTemplateEngineManager localeTemplateEngineManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(dialogId)
        {
            // Initialize state accessor
            WhoStateAccessor = conversationState.CreateProperty<WhoSkillState>(nameof(WhoSkillState));
            MSGraphService = msGraphService;
            TemplateEngine = localeTemplateEngineManager;
            TelemetryClient = telemetryClient;

            var initDialog = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                InitService,
                StartSearchKeyword,
            };

            var searchKeyword = new WaterfallStep[]
            {
                SearchKeyword,
                StartChooseCandidates,
            };

            var chooseCandidates = new WaterfallStep[]
            {
                ChooseCandidates,
                DisplayCandidates,
                CheckRestart,
                CollectUserChoice,
            };

            var displayResult = new WaterfallStep[]
            {
                DisplayResult,
                CheckRestart,
                CollectUserChoiceAfterResult
            };

            AddDialog(new MultiProviderAuthDialog(settings.OAuthConnections, appCredentials));
            AddDialog(new TextPrompt(Actions.Prompt));
            AddDialog(new WaterfallDialog(Actions.InitDialog, initDialog) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.SearchKeyword, searchKeyword) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ChooseCandidates, chooseCandidates) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.DisplayResult, displayResult) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.InitDialog;
        }

        protected IStatePropertyAccessor<WhoSkillState> WhoStateAccessor { get; set; }

        protected MSGraphService MSGraphService { get; set; }

        protected LocaleTemplateEngineManager TemplateEngine { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            await FillLuisResultIntoState(dc);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await FillLuisResultIntoState(dc);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        #region generate card methods.

        // This method is called to generate card for detail of a person.
        protected async Task<Activity> GetCardForDetail(Candidate candidate)
        {
            var photoUrl = await MSGraphService.GetUserPhotoUrlAsyc(candidate.Id) ?? string.Empty;
            var data = new
            {
                DisplayName = candidate.DisplayName ?? string.Empty,
                JobTitle = candidate.JobTitle ?? string.Empty,
                Department = candidate.Department ?? string.Empty,
                OfficeLocation = candidate.OfficeLocation ?? string.Empty,
                MobilePhone = candidate.MobilePhone ?? string.Empty,
                PhotoUrl = photoUrl ?? string.Empty,
            };
            var activity = TemplateEngine.GenerateActivityForLocale("CardForDetail", new
            {
                Person = data
            });
            return activity;
        }

        // This method is called to generate card for a page.
        protected async Task<Activity> GetCardForPage(List<Candidate> candidates)
        {
            var photoUrls = new List<string>();
            foreach (var candidate in candidates)
            {
                photoUrls.Add(await MSGraphService.GetUserPhotoUrlAsyc(candidate.Id) ?? string.Empty);
            }

            var data = new List<object>();
            for (int i = 0; i < candidates.Count(); i++)
            {
                data.Add(new
                {
                    PhotoUrl = photoUrls[i],
                    DisplayName = candidates[i].DisplayName,
                    JobTitle = candidates[i].JobTitle
                });
            }

            var activity = TemplateEngine.GenerateActivityForLocale("CardForPage", new
            {
                Persons = data
            });
            return activity;
        }
        #endregion

        // Search and generate result. Fill in candidates list property.
        protected virtual async Task<DialogTurnResult> SearchKeyword(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.EndDialogAsync();
        }

        // Display final result according to the person user picked.
        protected virtual async Task<DialogTurnResult> DisplayResult(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.EndDialogAsync();
        }

        // Display final result according to the person user picked.
        protected virtual async Task<DialogTurnResult> CollectUserChoiceAfterResult(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.EndDialogAsync();
        }

        #region init steps

        // Init steps
        private async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

        // Init steps
        private async Task<DialogTurnResult> AfterGetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

        // Init steps
        private async Task<DialogTurnResult> InitService(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await WhoStateAccessor.GetAsync(sc.Context);
                sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token);
                MSGraphService.InitServices(token as string);
                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> StartSearchKeyword(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.SearchKeyword);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        #endregion

        private async Task<DialogTurnResult> StartChooseCandidates(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.ChooseCandidates);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        #region choose cadidates steps

        private async Task<DialogTurnResult> ChooseCandidates(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await WhoStateAccessor.GetAsync(sc.Context);

                // Didn't find any matches.
                if (state.Candidates == null || !state.Candidates.Any())
                {
                    var data = new
                    {
                        TargetName = state.Keyword,
                    };
                    await sc.Context.SendActivityAsync(TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.NoSearchResult, new { Person = data }));
                    return await sc.EndDialogAsync();
                }

                // If only find one person, skips following choosing steps.
                if (state.Candidates != null && state.Candidates.Count == 1 && state.Candidates[0] != null)
                {
                    state.PickedPerson = state.Candidates[0];
                    return await sc.ReplaceDialogAsync(Actions.DisplayResult);
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> DisplayCandidates(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);

            var data = new
            {
                TargetName = state.Keyword,
            };
            var activity = TemplateEngine.GenerateActivityForLocale(
                WhoSharedResponses.ShowPage,
                new
                {
                    Person = data,
                    Number = state.Candidates.Count
                });
            await sc.Context.SendActivityAsync(activity);

            var candidates = state.Candidates.Skip(state.PageIndex * state.PageSize).Take(state.PageSize).ToList();
            var card = await GetCardForPage(candidates);
            return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = card });
        }

        private async Task<DialogTurnResult> CheckRestart(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);
            if (state.Restart)
            {
                switch (state.TriggerIntent)
                {
                    case WhoLuis.Intent.WhoIs:
                    case WhoLuis.Intent.JobTitle:
                    case WhoLuis.Intent.Department:
                    case WhoLuis.Intent.Location:
                    case WhoLuis.Intent.PhoneNumber:
                    case WhoLuis.Intent.EmailAddress:
                        {
                            state.Init();
                            return await sc.BeginDialogAsync(nameof(WhoIsDialog));
                        }

                    case WhoLuis.Intent.Manager:
                        {
                            state.Init();
                            return await sc.BeginDialogAsync(nameof(ManagerDialog));
                        }

                    case WhoLuis.Intent.DirectReports:
                        {
                            state.Init();
                            return await sc.BeginDialogAsync(nameof(DirectReportsDialog));
                        }

                    case WhoLuis.Intent.Peers:
                        {
                            state.Init();
                            return await sc.BeginDialogAsync(nameof(PeersDialog));
                        }

                    case WhoLuis.Intent.EmailAbout:
                        {
                            state.Init();
                            return await sc.BeginDialogAsync(nameof(EmailAboutDialog));
                        }

                    case WhoLuis.Intent.MeetAbout:
                        {
                            state.Init();
                            return await sc.BeginDialogAsync(nameof(MeetAboutDialog));
                        }

                    default:
                        {
                            state.Init();
                            var activity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.DidntUnderstandMessage);
                            await sc.Context.SendActivityAsync(activity);
                            return await sc.EndDialogAsync();
                        }
                }
            }
            else
            {
                return await sc.NextAsync();
            }
        }

        private async Task<DialogTurnResult> CollectUserChoice(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);
            var luisResult = sc.Context.TurnState.Get<WhoLuis>(StateProperties.WhoLuisResultKey);
            var topIntent = luisResult.TopIntent().intent;
            var maxPageNumber = ((state.Candidates.Count - 1) / state.PageSize) + 1;

            switch (topIntent)
            {
                case WhoLuis.Intent.ShowDetail:
                    {
                        // If user want to see someone's detail.
                        var index = (state.PageIndex * state.PageSize) + state.Ordinal - 1;
                        if (state.Ordinal > state.PageSize || state.Ordinal <= 0 || index >= state.Candidates.Count)
                        {
                            await sc.Context.SendActivityAsync("Invalid number.");
                            return await sc.ReplaceDialogAsync(Actions.ChooseCandidates);
                        }

                        state.PickedPerson = state.Candidates[index];
                        return await sc.ReplaceDialogAsync(Actions.DisplayResult);
                    }

                case WhoLuis.Intent.ShowNextPage:
                    {
                        // Else if user want to see next page.
                        if (state.PageIndex < maxPageNumber - 1)
                        {
                            state.PageIndex++;
                        }
                        else
                        {
                            var activity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.AlreadyLastPage);
                            await sc.Context.SendActivityAsync(activity);
                        }

                        return await sc.ReplaceDialogAsync(Actions.ChooseCandidates);
                    }

                case WhoLuis.Intent.ShowPreviousPage:
                    {
                        // Else if user want to see previous page.
                        if (state.PageIndex > 0)
                        {
                            state.PageIndex--;
                        }
                        else
                        {
                            var activity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.AlreadyFirstPage);
                            await sc.Context.SendActivityAsync(activity);
                        }

                        return await sc.ReplaceDialogAsync(Actions.ChooseCandidates);
                    }

                default:
                    {
                        var didntUnderstandActivity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.DidntUnderstandMessage);
                        await sc.Context.SendActivityAsync(didntUnderstandActivity);
                        return await sc.ReplaceDialogAsync(Actions.ChooseCandidates);
                    }
            }
        }

        #endregion

        private async Task FillLuisResultIntoState(DialogContext dc)
        {
            try
            {
                var state = await WhoStateAccessor.GetAsync(dc.Context);
                var luisResult = dc.Context.TurnState.Get<WhoLuis>(StateProperties.WhoLuisResultKey);
                var generalLuisResult = dc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var entities = luisResult.Entities;
                var generalEntities = generalLuisResult.Entities;
                var topIntent = luisResult.TopIntent().intent;

                // Save trigger intent.
                if (state.TriggerIntent == WhoLuis.Intent.None)
                {
                    state.TriggerIntent = topIntent;
                }

                // Save the keyword that user want to search.
                if (entities != null && entities.keyword != null && !string.IsNullOrEmpty(entities.keyword[0]))
                {
                    if (string.IsNullOrEmpty(state.Keyword))
                    {
                        state.Keyword = entities.keyword[0];
                    }
                    else
                    {
                        // user want to start a new search.
                        state.TriggerIntent = topIntent;
                        state.Restart = true;
                    }

                    return;
                }

                // Save ordinal
                if (generalEntities != null && generalEntities.number != null && generalEntities.number.Length > 0)
                {
                    var indexOfNumber = (int)generalEntities.number[0];
                    state.Ordinal = indexOfNumber;
                }

                if (entities != null && entities.ordinal != null && entities.ordinal.Length > 0)
                {
                    var indexOfOrdinal = (int)entities.ordinal[0];
                    state.Ordinal = indexOfOrdinal;
                }
            }
            catch
            {
            }
        }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        private async Task HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            var activity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.WhoErrorMessage);
            await sc.Context.SendActivityAsync(activity);

            // clear state
            var state = await WhoStateAccessor.GetAsync(sc.Context);
            state.Init();
        }
    }
}
