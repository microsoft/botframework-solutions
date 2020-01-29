// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using SkillServiceLibrary.Utilities;
using ToDoSkill.Models;
using ToDoSkill.Responses.Main;
using ToDoSkill.Services;
using ToDoSkill.Utilities;

namespace ToDoSkill.Dialogs
{
    public class MainDialog : ActivityHandlerDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private IStatePropertyAccessor<ToDoSkillState> _toDoStateAccessor;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            AddToDoItemDialog addToDoItemDialog,
            MarkToDoItemDialog markToDoItemDialog,
            DeleteToDoItemDialog deleteToDoItemDialog,
            ShowToDoItemDialog showToDoItemDialog,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _toDoStateAccessor = conversationState.CreateProperty<ToDoSkillState>(nameof(ToDoSkillState));
            TemplateEngine = localeTemplateEngineManager;
            TelemetryClient = telemetryClient;

            // RegisterDialogs
            AddDialog(addToDoItemDialog ?? throw new ArgumentNullException(nameof(addToDoItemDialog)));
            AddDialog(markToDoItemDialog ?? throw new ArgumentNullException(nameof(markToDoItemDialog)));
            AddDialog(deleteToDoItemDialog ?? throw new ArgumentNullException(nameof(deleteToDoItemDialog)));
            AddDialog(showToDoItemDialog ?? throw new ArgumentNullException(nameof(showToDoItemDialog)));
        }

        private LocaleTemplateEngineManager TemplateEngine { get; set; }

        protected override async Task OnMembersAddedAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = TemplateEngine.GenerateActivityForLocale(ToDoMainResponses.ToDoWelcomeMessage);
            await dc.Context.SendActivityAsync(activity);
        }

        protected override async Task OnMessageActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _toDoStateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());

            // Initialize the PageSize and ReadSize parameters in state from configuration
            InitializeConfig(state);

            var luisResult = dc.Context.TurnState.Get<ToDoLuis>(StateProperties.ToDoLuisResultKey);
            var intent = luisResult?.TopIntent().intent;
            var generalLuisResult = dc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
            var generalTopIntent = generalLuisResult?.TopIntent().intent;

            // switch on general intents
            switch (intent)
            {
                case ToDoLuis.Intent.AddToDo:
                    {
                        await dc.BeginDialogAsync(nameof(AddToDoItemDialog));
                        break;
                    }

                case ToDoLuis.Intent.MarkToDo:
                    {
                        await dc.BeginDialogAsync(nameof(MarkToDoItemDialog));
                        break;
                    }

                case ToDoLuis.Intent.DeleteToDo:
                    {
                        await dc.BeginDialogAsync(nameof(DeleteToDoItemDialog));
                        break;
                    }

                case ToDoLuis.Intent.ShowNextPage:
                case ToDoLuis.Intent.ShowPreviousPage:
                case ToDoLuis.Intent.ShowToDo:
                    {
                        await dc.BeginDialogAsync(nameof(ShowToDoItemDialog));
                        break;
                    }

                    case ToDoLuis.Intent.None:
                        {
                            if (generalTopIntent == General.Intent.ShowNext
                                || generalTopIntent == General.Intent.ShowPrevious)
                            {
                                await dc.BeginDialogAsync(nameof(ShowToDoItemDialog));
                            }
                            else
                            {
                                // No intent was identified, send confused message
                                var activity = TemplateEngine.GenerateActivityForLocale(ToDoMainResponses.DidntUnderstandMessage);
                                await dc.Context.SendActivityAsync(activity);
                            }

                        break;
                    }

                default:
                    {
                        // intent was identified but not yet implemented
                        var activity = TemplateEngine.GenerateActivityForLocale(ToDoMainResponses.FeatureNotAvailable);
                        await dc.Context.SendActivityAsync(activity);
                        break;
                    }
            }
        }

        // Runs on every turn of the conversation.
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("ToDo", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<ToDoLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.ToDoLuisResultKey, skillResult);
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResultKey, generalResult);
                }
                else
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        protected override async Task OnDialogCompleteAsync(DialogContext dc, object result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // workaround. if connect skill directly to teams, the following response does not work.
            if (dc.Context.IsSkill() || Channel.GetChannelId(dc.Context) != Channels.Msteams)
            {
                var response = dc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.EndOfConversation;

                await dc.Context.SendActivityAsync(response);
            }

            // End active dialog
            await dc.EndDialogAsync(result);
        }

        protected override async Task OnEventActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (dc.Context.Activity.Name)
            {
                case TokenEvents.TokenResponseEventName:
                    {
                        // Auth dialog completion
                        var result = await dc.ContinueDialogAsync();

                        // If the dialog completed when we sent the token, end the skill conversation
                        if (result.Status != DialogTurnStatus.Waiting)
                        {
                            var response = dc.Context.Activity.CreateReply();
                            response.Type = ActivityTypes.EndOfConversation;

                            await dc.Context.SendActivityAsync(response);
                        }

                        break;
                    }
            }
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = InterruptionAction.NoAction;

            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                var state = await _toDoStateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());
                var generalLuisResult = dc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var topIntent = generalLuisResult.TopIntent();

                if (topIntent.score > 0.5)
                {
                    switch (topIntent.intent)
                    {
                        case General.Intent.Cancel:
                            {
                                result = await OnCancel(dc);
                                break;
                            }

                        case General.Intent.Help:
                            {
                                // result = await OnHelp(dc);
                                break;
                            }

                        case General.Intent.Logout:
                            {
                                result = await OnLogout(dc);
                                break;
                            }
                    }
                }
            }

            return result;
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            var activity = TemplateEngine.GenerateActivityForLocale(ToDoMainResponses.CancelMessage);
            await dc.Context.SendActivityAsync(activity);
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.End;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            var activity = TemplateEngine.GenerateActivityForLocale(ToDoMainResponses.HelpMessage);
            await dc.Context.SendActivityAsync(activity);
            return InterruptionAction.Resume;
        }

        private async Task<InterruptionAction> OnLogout(DialogContext dc)
        {
            BotFrameworkAdapter adapter;
            var supported = dc.Context.Adapter is BotFrameworkAdapter;
            if (!supported)
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
            else
            {
                adapter = (BotFrameworkAdapter)dc.Context.Adapter;
            }

            await dc.CancelAllDialogsAsync();

            // Sign out user
            var tokens = await adapter.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
            foreach (var token in tokens)
            {
                await adapter.SignOutUserAsync(dc.Context, token.ConnectionName);
            }

            var activity = TemplateEngine.GenerateActivityForLocale(ToDoMainResponses.LogOut);
            await dc.Context.SendActivityAsync(activity);
            return InterruptionAction.End;
        }

        private void InitializeConfig(ToDoSkillState state)
        {
            // Initialize PageSize and TaskServiceType when the first input comes.
            if (state.PageSize <= 0)
            {
                var pageSize = _settings.DisplaySize;
                state.PageSize = pageSize <= 0 ? ToDoCommonUtil.DefaultDisplaySize : pageSize;
            }

            if (state.TaskServiceType == ServiceProviderType.Other)
            {
                state.TaskServiceType = ServiceProviderType.Outlook;
                if (!string.IsNullOrEmpty(_settings.TaskServiceProvider))
                {
                    if (_settings.TaskServiceProvider.Equals(ServiceProviderType.OneNote.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        state.TaskServiceType = ServiceProviderType.OneNote;
                    }
                }
            }
        }
    }
}