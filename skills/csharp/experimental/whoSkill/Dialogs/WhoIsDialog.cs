using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using WhoSkill.Models;
using WhoSkill.Services;
using WhoSkill.Utilities;

namespace WhoSkill.Dialogs
{
    public class WhoIsDialog : WhoSkillDialogBase
    {
        public WhoIsDialog(
                BotSettings settings,
                ConversationState conversationState,
                MSGraphService msGraphService,
                // LocaleTemplateEngineManager localeTemplateEngineManager,
                IBotTelemetryClient telemetryClient,
                MicrosoftAppCredentials appCredentials)
            //IHttpContextAccessor httpContext)
            : base(nameof(WhoIsDialog), settings, conversationState, msGraphService, telemetryClient, appCredentials)
        {
            var initDialog = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                InitService,
                ShowPersons,
            };

            var showPersons = new WaterfallStep[]
            {
                ShowCurrentPage,
                CollectUserChoice,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.InitDialog, initDialog) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowPersons, showPersons) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.InitDialog;
        }

        public async Task<DialogTurnResult> ShowPersons(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.ShowPersons);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // Send different reply according to skill state. But this step doesn't modify skill state.
        public async Task<DialogTurnResult> ShowCurrentPage(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await WhoStateAccessor.GetAsync(sc.Context);

                if (!state.AlreadySearched)
                {
                    if (string.IsNullOrEmpty(state.PersonName))
                    {
                        await sc.Context.SendActivityAsync("Please provide the name of the person you want to look up.");
                        return await sc.EndDialogAsync();
                    }
                    else
                    {
                        var users = await MSGraphService.GetUsers(state.PersonName);
                        state.Persons = users;
                        state.AlreadySearched = true;
                    }
                }

                // Didn't find any candidate.
                if (state.Persons == null || state.Persons.Count == 0)
                {
                    await sc.Context.SendActivityAsync("Sorry, I couldn’t find anyone named");
                    await sc.EndDialogAsync();
                }

                if (state.Persons.Count == 1)
                {
                    await sc.Context.SendActivityAsync($"Here's what I found for {state.PersonName}");
                    return await sc.EndDialogAsync();
                }
                else
                {
                    var replyMessage = $"I found multiple matches for {state.PersonName}. Please pick one. Or you can say 'next' or 'previous' to see more persons.";
                    var prompt = MessageFactory.Text(replyMessage);
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = prompt, RetryPrompt = prompt });
                }
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

        // Accept user's choice. Update skill state.
        public async Task<DialogTurnResult> CollectUserChoice(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);
            var luisResult = sc.Context.TurnState.Get<WhoLuis>(StateProperties.WhoLuisResultKey);
            var topIntent = luisResult.TopIntent().intent;
            var maxPageNumber = ((state.Persons.Count - 1) / state.PageSize) + 1;

            // If user want to see someone's detail.
            if (state.Ordinal != int.MinValue)
            {
                var index = (state.PageIndex * state.PageSize) + state.Ordinal - 1;
                if (state.Ordinal > state.PageSize || state.Ordinal <= 0 || index >= state.Persons.Count)
                {
                    await sc.Context.SendActivityAsync("Invalid number.");
                    state.Ordinal = int.MinValue;
                    return await sc.ReplaceDialogAsync(Actions.ShowPersons);
                }

                var candidate = new List<Person>();
                candidate.Add(state.Persons[index]);
                state.Persons = candidate;
            }

            // Else if user want to see next page.
            else if (topIntent == WhoLuis.Intent.ShowNextPage)
            {
                if (state.PageIndex < maxPageNumber - 1)
                {
                    state.PageIndex++;
                }
                else
                {
                    await sc.Context.SendActivityAsync("Already last page.");
                }
            }

            return await sc.ReplaceDialogAsync(Actions.ShowPersons);

            //var confirmResult = (bool)sc.Result;
            //if (confirmResult)
            //{
            //    state.ShowTaskPageIndex = 0;
            //    state.GoBackToStart = true;
            //    return await sc.ReplaceDialogAsync(Actions.DoShowTasks);
            //}
            //else
            //{
            //    //var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.ActionEnded);
            //    //await sc.Context.SendActivityAsync(activity);
            //    return await sc.EndDialogAsync(true);
            //}
        }
    }
}
