// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using ToDoSkill.Models;
using ToDoSkill.Responses.Main;
using ToDoSkill.Services;
using ToDoSkill.Utilities;

namespace ToDoSkill.Dialogs
{
    public class MainDialog : RouterDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private ResponseManager _responseManager;
        private IStatePropertyAccessor<ToDoSkillState> _toDoStateAccessor;
        private ResourceMultiLanguageGenerator _lgMultiLangEngine;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            AddToDoItemDialog addToDoItemDialog,
            MarkToDoItemDialog markToDoItemDialog,
            DeleteToDoItemDialog deleteToDoItemDialog,
            ShowToDoItemDialog showToDoItemDialog,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _responseManager = responseManager;
            TelemetryClient = telemetryClient;
            _toDoStateAccessor = conversationState.CreateProperty<ToDoSkillState>(nameof(ToDoSkillState));

            _lgMultiLangEngine = new ResourceMultiLanguageGenerator("ToDoMainResponses.lg");

            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Recognizer = CreateRecognizer(),
                Rules = new List<IRule>()
                {
                    new IntentRule("AddToDo")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(AddToDoItemDialog)) },
                        Constraint = "turn.dialogEvent.value.intents.AddToDo.score > 0.4",
                    },
                    new IntentRule("MarkToDo")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(MarkToDoItemDialog)) },
                        Constraint = "turn.dialogEvent.value.intents.MarkToDo.score > 0.4",
                    },
                    new IntentRule("DeleteToDo")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(DeleteToDoItemDialog)) },
                        Constraint = "turn.dialogEvent.value.intents.DeleteToDo.score > 0.4",
                    },
                    new IntentRule("ShowNextPage")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(ShowToDoItemDialog)) },
                        Constraint = "turn.dialogEvent.value.intents.ShowNextPage.score > 0.4",
                    },
                    new IntentRule("ShowPreviousPage")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(ShowToDoItemDialog)) },
                        Constraint = "turn.dialogEvent.value.intents.ShowPreviousPage.score > 0.4",
                    },
                    new IntentRule("ShowToDo")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(ShowToDoItemDialog)) },
                        Constraint = "turn.dialogEvent.value.intents.ShowToDo.score > 0.4",
                    },
                    new IntentRule("None")
                    {
                        Steps = new List<IDialog>() { new SendActivity("Sorry, I don't understand.") },
                        Constraint = "turn.dialogEvent.value.intents.None.score > 0.4",
                    },
                    new UnknownIntentRule()
                    {
                        Steps = new List<IDialog>() { new SendActivity("FeatureNotAvailable") }
                    }
                }
            };
            rootDialog.AddDialog(new List<IDialog>()
            {
                addToDoItemDialog,
                markToDoItemDialog,
                deleteToDoItemDialog,
                showToDoItemDialog
            });

            // RegisterDialogs
            AddDialog(rootDialog);
            AddDialog(addToDoItemDialog ?? throw new ArgumentNullException(nameof(addToDoItemDialog)));
            AddDialog(markToDoItemDialog ?? throw new ArgumentNullException(nameof(markToDoItemDialog)));
            AddDialog(deleteToDoItemDialog ?? throw new ArgumentNullException(nameof(deleteToDoItemDialog)));
            AddDialog(showToDoItemDialog ?? throw new ArgumentNullException(nameof(showToDoItemDialog)));
            InitialDialogId = nameof(AdaptiveDialog);
        }

        public static IRecognizer CreateRecognizer()
        {
            return new LuisRecognizer(new LuisApplication()
            {
                Endpoint = "https://westus.api.cognitive.microsoft.com/", // Configuration["LuisAPIHostName"],
                EndpointKey = "897fdde0609d49918e4cc56b684daf26", // Configuration["LuisAPIKey"],
                ApplicationId = "e022ca2a-9429-49a3-a81f-5dbcd1fcce0e", // Configuration["LuisAppId"]
            });
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var a = CultureInfo.CurrentCulture.Name;
            if (dc.Context.TurnState.Get<ILanguageGenerator>() == null)
            {
                dc.Context.TurnState.Add<ILanguageGenerator>(_lgMultiLangEngine);
            }

            //var result = await _lgMultiLangEngine.Generate(dc.Context, "[ToDoWelcomeMessage]", null);

            //await dc.Context.SendActivityAsync(result);

            await dc.Context.SendActivityAsync(_responseManager.GetResponse(ToDoMainResponses.ToDoWelcomeMessage));
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _toDoStateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());

            // get current activity locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = _services.CognitiveModelSets[locale];

            // Initialize the PageSize and ReadSize parameters in state from configuration
            InitializeConfig(state);

            // If dispatch result is general luis model
            localeConfig.LuisServices.TryGetValue("todo", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                if (dc.ActiveDialog == null)
                {
                    await dc.BeginDialogAsync(nameof(AdaptiveDialog));
                }
                else
                {
                    await dc.ContinueDialogAsync();
                }
            }
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // workaround. if connect skill directly to teams, the following response does not work.
            if (dc.Context.Adapter is IRemoteUserTokenProvider remoteInvocationAdapter || Channel.GetChannelId(dc.Context) != Channels.Msteams)
            {
                var response = dc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.EndOfConversation;

                await dc.Context.SendActivityAsync(response);
            }

            // End active dialog
            await dc.EndDialogAsync(result);
        }

        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
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
                // get current activity locale
                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var cognitiveModels = _services.CognitiveModelSets[locale];

                // Update state with email luis result and entities
                var toDoLuisResult = await cognitiveModels.LuisServices["todo"].RecognizeAsync<todoLuis>(dc.Context, cancellationToken);
                var state = await _toDoStateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());
                state.LuisResult = toDoLuisResult;

                // check luis intent
                cognitiveModels.LuisServices.TryGetValue("general", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Skill configuration.");
                }
                else
                {
                    var luisResult = await luisService.RecognizeAsync<General>(dc.Context, cancellationToken);
                    state.GeneralLuisResult = luisResult;
                    var topIntent = luisResult.TopIntent().intent;

                    switch (topIntent)
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
            var state = await _toDoStateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());
            state.Clear();

            await dc.Context.SendActivityAsync(_responseManager.GetResponse(ToDoMainResponses.CancelMessage));
            await CompleteAsync(dc);
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(ToDoMainResponses.HelpMessage));
            return InterruptionAction.MessageSentToUser;
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

            await dc.Context.SendActivityAsync(_responseManager.GetResponse(ToDoMainResponses.LogOut));

            return InterruptionAction.StartedDialog;
        }

        private void InitializeConfig(ToDoSkillState state)
        {
            // Initialize PageSize and TaskServiceType when the first input comes.
            if (state.PageSize <= 0)
            {
                var pageSize = 0;
                if (_settings.Properties.TryGetValue("DisplaySize", out var displaySizeObj))
                {
                    int.TryParse(displaySizeObj.ToString(), out pageSize);
                }

                state.PageSize = pageSize <= 0 ? ToDoCommonUtil.DefaultDisplaySize : pageSize;
            }

            if (state.TaskServiceType == ServiceProviderType.Other)
            {
                state.TaskServiceType = ServiceProviderType.Outlook;
                if (_settings.Properties.TryGetValue("TaskServiceProvider", out var taskServiceProvider))
                {
                    if (taskServiceProvider.ToString().Equals(ServiceProviderType.OneNote.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        state.TaskServiceType = ServiceProviderType.OneNote;
                    }
                }
            }
        }
    }
}