using System;
using System.Collections.Generic;
using System.Linq;
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
            BotSettings settings,
            ConversationState conversationState,
            MSGraphService msGraphService,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(WhoSkillDialogBase))
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
                InitService
            };

            var searchKeyword = new WaterfallStep[]
            {
                SearchKeyword,
            };

            var showSearchResult = new WaterfallStep[]
            {
                ShowSearchResult,
            };

            var showNoResult = new WaterfallStep[]
            {
                ShowNoResult,
            };

            var showCertainPerson = new WaterfallStep[]
            {
                ShowCurrentPerson,
                CollectUserChoiceForPerson,
            };

            var showCandidates = new WaterfallStep[]
            {
                ShowCurrentPage,
                CollectUserChoiceForPage,
            };

            AddDialog(new MultiProviderAuthDialog(settings.OAuthConnections, appCredentials));
            AddDialog(new TextPrompt(Actions.Prompt));
            AddDialog(new WaterfallDialog(Actions.InitDialog, initDialog) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.SearchKeyword, searchKeyword) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowSearchResult, showSearchResult) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowNoResult, showNoResult) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowCertainPerson, showCertainPerson) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowCandidates, showCandidates) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.InitDialog;
        }

        protected IStatePropertyAccessor<WhoSkillState> WhoStateAccessor { get; set; }

        protected MSGraphService MSGraphService { get; set; }

        protected LocaleTemplateEngineManager TemplateEngine { get; set; }

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
                return await sc.ReplaceDialogAsync(Actions.SearchKeyword);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
        #endregion

        #region search keyword

        // Search and generate result:
        // Fill in candidates list property. If only one candidate, fill in pickedPerson property.
        private async Task<DialogTurnResult> SearchKeyword(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await WhoStateAccessor.GetAsync(sc.Context);

                if (string.IsNullOrEmpty(state.TargetName))
                {
                    var activity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.NoKeyword);
                    await sc.Context.SendActivityAsync(activity);
                    return await sc.EndDialogAsync();
                }

                List<Candidate> candidates = null;
                switch (state.TriggerIntent)
                {
                    case WhoLuis.Intent.WhoIs:
                    case WhoLuis.Intent.JobTitle:
                    case WhoLuis.Intent.Department:
                    case WhoLuis.Intent.Location:
                    case WhoLuis.Intent.PhoneNumber:
                    case WhoLuis.Intent.EmailAddress:
                        {
                            candidates = await MSGraphService.GetUsers(state.TargetName);
                            break;
                        }

                    case WhoLuis.Intent.Manager:
                        {
                            if (!state.FirstSearchCompleted)
                            {
                                candidates = await MSGraphService.GetUsers(state.TargetName);
                            }
                            else
                            {
                                var manager = await MSGraphService.GetManager(state.PickedPerson.Id);
                                if (manager != null)
                                {
                                    candidates = new List<Candidate>() { manager };
                                }

                                state.SecondSearchCompleted = true;
                            }

                            break;
                        }

                    case WhoLuis.Intent.DirectReports:
                        {
                            if (!state.FirstSearchCompleted)
                            {
                                candidates = await MSGraphService.GetUsers(state.TargetName);
                            }
                            else
                            {
                                candidates = await MSGraphService.GetDirectReports(state.PickedPerson.Id);
                                state.PageIndex = 0;
                                state.SecondSearchCompleted = true;
                            }

                            break;
                        }

                    default:
                        {
                            var didntUnderstandActivity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.DidntUnderstandMessage);
                            await sc.Context.SendActivityAsync(didntUnderstandActivity);
                            return await sc.EndDialogAsync();
                        }
                }

                state.FirstSearchCompleted = true;
                state.Candidates = candidates;
                if (state.Candidates != null && state.Candidates.Count == 1 && state.Candidates[0] != null)
                {
                    state.PickedPerson = state.Candidates[0];
                }

                return await sc.ReplaceDialogAsync(Actions.ShowSearchResult);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ShowSearchResult(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);

            if (state.PickedPerson != null)
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
                            return await sc.ReplaceDialogAsync(Actions.ShowCertainPerson);
                        }

                    // If trigger intent is manager/ directReports, we need to search again for this keyword's manager/ directReports.
                    case WhoLuis.Intent.Manager:
                    case WhoLuis.Intent.DirectReports:
                        {
                            if (!state.SecondSearchCompleted)
                            {
                                return await sc.ReplaceDialogAsync(Actions.SearchKeyword);
                            }
                            else
                            {
                                if (state.Candidates == null || state.Candidates.Count == 0)
                                {
                                    return await sc.ReplaceDialogAsync(Actions.ShowNoResult);
                                }
                                else if (state.Candidates.Count == 1)
                                {
                                    state.PickedPerson = state.Candidates[0];
                                    return await sc.ReplaceDialogAsync(Actions.ShowCertainPerson);
                                }
                                else
                                {
                                    return await sc.ReplaceDialogAsync(Actions.ShowCandidates);
                                }
                            }
                        }

                    default:
                        {
                            var didntUnderstandActivity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.DidntUnderstandMessage);
                            await sc.Context.SendActivityAsync(didntUnderstandActivity);
                            return await sc.EndDialogAsync();
                        }
                }
            }
            else
            {
                if (state.TriggerIntent != WhoLuis.Intent.None)
                {
                    if (state.Candidates == null || state.Candidates.Count == 0)
                    {
                        return await sc.ReplaceDialogAsync(Actions.ShowNoResult);
                    }
                    else if (state.Candidates.Count == 1)
                    {
                        state.PickedPerson = state.Candidates[0];
                        return await sc.ReplaceDialogAsync(Actions.ShowCertainPerson);
                    }
                    else
                    {
                        return await sc.ReplaceDialogAsync(Actions.ShowCandidates);
                    }
                }
                else
                {
                    var didntUnderstandActivity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.DidntUnderstandMessage);
                    await sc.Context.SendActivityAsync(didntUnderstandActivity);
                    return await sc.EndDialogAsync();
                }
            }
        }
        #endregion

        // Didn't find any matches. Send different reply according to intent.
        private async Task<DialogTurnResult> ShowNoResult(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);
            string templateName = string.Empty;
            switch (state.TriggerIntent)
            {
                case WhoLuis.Intent.WhoIs:
                case WhoLuis.Intent.JobTitle:
                case WhoLuis.Intent.Department:
                case WhoLuis.Intent.Location:
                case WhoLuis.Intent.PhoneNumber:
                case WhoLuis.Intent.EmailAddress:
                    {
                        templateName = WhoIsResponses.NoSearchResult;
                        break;
                    }

                case WhoLuis.Intent.Manager:
                    {
                        templateName = OrgResponses.NoManager;
                        break;
                    }

                case WhoLuis.Intent.DirectReports:
                    {
                        templateName = OrgResponses.NoDirectReports;
                        break;
                    }

                default:
                    {
                        var didntUnderstandActivity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.DidntUnderstandMessage);
                        await sc.Context.SendActivityAsync(didntUnderstandActivity);
                        return await sc.EndDialogAsync();
                    }
            }

            var data = new
            {
                TargetName = state.TargetName,
            };
            await sc.Context.SendActivityAsync(TemplateEngine.GenerateActivityForLocale(templateName, new { Person = data }));
            return await sc.EndDialogAsync();
        }

        #region show a person flow

        // Show a person's detail
        private async Task<DialogTurnResult> ShowCurrentPerson(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await WhoStateAccessor.GetAsync(sc.Context);
                string templateName = string.Empty;
                switch (state.TriggerIntent)
                {
                    case WhoLuis.Intent.WhoIs:
                        templateName = WhoIsResponses.WhoIs;
                        break;
                    case WhoLuis.Intent.JobTitle:
                        templateName = WhoIsResponses.JobTitle;
                        break;
                    case WhoLuis.Intent.Department:
                        templateName = WhoIsResponses.Department;
                        break;
                    case WhoLuis.Intent.Location:
                        templateName = WhoIsResponses.Location;
                        break;
                    case WhoLuis.Intent.PhoneNumber:
                        templateName = WhoIsResponses.PhoneNumber;
                        break;
                    case WhoLuis.Intent.EmailAddress:
                        templateName = WhoIsResponses.EmailAddress;
                        break;
                    case WhoLuis.Intent.Manager:
                        templateName = OrgResponses.Manager;
                        break;
                    case WhoLuis.Intent.DirectReports:
                        templateName = OrgResponses.DirectReports;
                        break;
                    default:
                        {
                            var didntUnderstandActivity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.DidntUnderstandMessage);
                            await sc.Context.SendActivityAsync(didntUnderstandActivity);
                            return await sc.EndDialogAsync();
                        }
                }

                var data = new
                {
                    TargetName = state.TargetName,
                    JobTitle = state.PickedPerson.JobTitle ?? string.Empty,
                    Department = state.PickedPerson.Department ?? string.Empty,
                    OfficeLocation = state.PickedPerson.OfficeLocation ?? string.Empty,
                    MobilePhone = state.PickedPerson.MobilePhone ?? string.Empty,
                    EmailAddress = state.PickedPerson.Mail ?? string.Empty,
                };

                var reply = TemplateEngine.GenerateActivityForLocale(templateName, new { Person = data });
                await sc.Context.SendActivityAsync(reply);
                var card = await GetCardForDetail(state.PickedPerson);
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = card });
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectUserChoiceForPerson(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);
            if (state.Restart)
            {
                state.Restart = false;
                return await sc.ReplaceDialogAsync(Actions.SearchKeyword);
            }

            var luisResult = sc.Context.TurnState.Get<WhoLuis>(StateProperties.WhoLuisResultKey);
            var topIntent = luisResult.TopIntent().intent;

            switch (topIntent)
            {
                case WhoLuis.Intent.Manager:
                    {
                        state.TargetName = state.PickedPerson.DisplayName;
                        state.TriggerIntent = WhoLuis.Intent.Manager;
                        state.FirstSearchCompleted = true;
                        state.SecondSearchCompleted = false;
                        return await sc.ReplaceDialogAsync(Actions.SearchKeyword);
                    }

                default:
                    {
                        var didntUnderstandActivity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.DidntUnderstandMessage);
                        await sc.Context.SendActivityAsync(didntUnderstandActivity);
                        return await sc.EndDialogAsync();
                    }
            }
        }
        #endregion

        #region show page flow

        // Send a page to show multi search results.
        private async Task<DialogTurnResult> ShowCurrentPage(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await WhoStateAccessor.GetAsync(sc.Context);
                string templateName = string.Empty;
                switch (state.TriggerIntent)
                {
                    case WhoLuis.Intent.WhoIs:
                    case WhoLuis.Intent.JobTitle:
                    case WhoLuis.Intent.Department:
                    case WhoLuis.Intent.Location:
                    case WhoLuis.Intent.PhoneNumber:
                    case WhoLuis.Intent.EmailAddress:
                    case WhoLuis.Intent.Manager:
                        {
                            templateName = WhoSharedResponses.ShowPage;
                            break;
                        }

                    case WhoLuis.Intent.DirectReports:
                        {
                            if (!state.SecondSearchCompleted)
                            {
                                templateName = WhoSharedResponses.ShowPage;
                            }
                            else
                            {
                                templateName = OrgResponses.DirectReports;
                            }

                            break;
                        }

                    default:
                        {
                            var didntUnderstandActivity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.DidntUnderstandMessage);
                            await sc.Context.SendActivityAsync(didntUnderstandActivity);
                            return await sc.EndDialogAsync();
                        }
                }

                var data = new
                {
                    TargetName = state.TargetName,
                };
                var activity = TemplateEngine.GenerateActivityForLocale(templateName, new { Person = data, Number = state.Candidates.Count });
                await sc.Context.SendActivityAsync(activity);
                var candidates = state.Candidates.Skip(state.PageIndex * state.PageSize).Take(state.PageSize).ToList();
                var card = await GetCardForPage(candidates);
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = card });
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // Accept user's choice for a page. Update skill state.
        private async Task<DialogTurnResult> CollectUserChoiceForPage(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);
            if (state.Restart)
            {
                state.Restart = false;
                return await sc.ReplaceDialogAsync(Actions.SearchKeyword);
            }

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
                            return await sc.ReplaceDialogAsync(Actions.ShowCandidates);
                        }

                        state.PickedPerson = state.Candidates[index];
                        if (state.SecondSearchCompleted)
                        {
                            state.TriggerIntent = WhoLuis.Intent.WhoIs;
                            state.TargetName = state.PickedPerson.DisplayName;
                            state.FirstSearchCompleted = true;
                            state.SecondSearchCompleted = false;
                        }

                        return await sc.ReplaceDialogAsync(Actions.ShowSearchResult);
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

                        return await sc.ReplaceDialogAsync(Actions.ShowCandidates);
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

                        return await sc.ReplaceDialogAsync(Actions.ShowCandidates);
                    }

                default:
                    {
                        var didntUnderstandActivity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.DidntUnderstandMessage);
                        await sc.Context.SendActivityAsync(didntUnderstandActivity);
                        return await sc.EndDialogAsync();
                    }
            }
        }
        #endregion

        private async Task InitState(DialogContext dc)
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
                var topIntent = luisResult.TopIntent().intent;

                // save trigger intent.
                if (state.TriggerIntent == WhoLuis.Intent.None)
                {
                    state.TriggerIntent = topIntent;
                }

                // save the keyword that user want to search.
                if (entities != null && entities.keyword != null && !string.IsNullOrEmpty(entities.keyword[0]))
                {
                    if (string.IsNullOrEmpty(state.TargetName))
                    {
                        state.TargetName = entities.keyword[0];
                    }
                    else
                    {
                        // user want to start a new search.
                        state.Init();
                        state.TargetName = entities.keyword[0];
                        state.TriggerIntent = topIntent;
                        state.Restart = true;
                    }
                }

                // save ordinal
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
