// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using ToDoSkill.Common;
using ToDoSkill.Dialogs.AddToDo;
using ToDoSkill.Dialogs.DeleteToDo;
using ToDoSkill.Dialogs.Main.Resources;
using ToDoSkill.Dialogs.MarkToDo;
using ToDoSkill.Dialogs.Shared;
using ToDoSkill.Dialogs.Shared.DialogOptions;
using ToDoSkill.Dialogs.ShowToDo;
using ToDoSkill.ServiceClients;
using static ToDoSkill.Dialogs.Shared.ServiceProviderTypes;

namespace ToDoSkill.Dialogs.Main
{
    public class MainDialog : RouterDialog
    {
        private bool _skillMode;
        private SkillConfigurationBase _services;
        private UserState _userState;
        private ConversationState _conversationState;
        private IServiceManager _serviceManager;
        private IStatePropertyAccessor<ToDoSkillState> _toDoStateAccessor;
        private IStatePropertyAccessor<ToDoSkillUserState> _userStateAccessor;
        private ToDoSkillResponseBuilder _responseBuilder = new ToDoSkillResponseBuilder();

        public MainDialog(
            SkillConfigurationBase services,
            ConversationState conversationState,
            UserState userState,
            IBotTelemetryClient telemetryClient,
            IServiceManager serviceManager,
            bool skillMode)
            : base(nameof(MainDialog), telemetryClient)
        {
            _skillMode = skillMode;
            _services = services;
            _conversationState = conversationState;
            _userState = userState;
            _serviceManager = serviceManager;
            TelemetryClient = telemetryClient;

            // Initialize state accessor
            _toDoStateAccessor = _conversationState.CreateProperty<ToDoSkillState>(nameof(ToDoSkillState));
            _userStateAccessor = _userState.CreateProperty<ToDoSkillUserState>(nameof(ToDoSkillUserState));

            // RegisterDialogs
            RegisterDialogs();
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_skillMode)
            {
                // send a greeting if we're in local mode
                await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(ToDoMainResponses.ToDoWelcomeMessage));
            }
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _toDoStateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());

            // get current activity locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = _services.LocaleConfigurations[locale];

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
                var result = await luisService.RecognizeAsync<ToDo>(dc.Context, CancellationToken.None);
                var intent = result?.TopIntent().intent;
                var generalTopIntent = state.GeneralLuisResult?.TopIntent().intent;

                var skillOptions = new ToDoSkillDialogOptions
                {
                    SkillMode = _skillMode,
                };

                // switch on general intents
                switch (intent)
                {
                    case ToDo.Intent.AddToDo:
                        {
                            await dc.BeginDialogAsync(nameof(AddToDoItemDialog), skillOptions);
                            break;
                        }

                    case ToDo.Intent.MarkToDo:
                        {
                            await dc.BeginDialogAsync(nameof(MarkToDoItemDialog), skillOptions);
                            break;
                        }

                    case ToDo.Intent.DeleteToDo:
                        {
                            await dc.BeginDialogAsync(nameof(DeleteToDoItemDialog), skillOptions);
                            break;
                        }

                    case ToDo.Intent.ShowToDo:
                        {
                            await dc.BeginDialogAsync(nameof(ShowToDoItemDialog), skillOptions);
                            break;
                        }

                    case ToDo.Intent.None:
                        {
                            if (generalTopIntent == General.Intent.Next
                                || generalTopIntent == General.Intent.Previous
                                || generalTopIntent == General.Intent.ReadMore)
                            {
                                await dc.BeginDialogAsync(nameof(ShowToDoItemDialog), skillOptions);
                            }
                            else
                            {
                                // No intent was identified, send confused message
                                await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(ToDoMainResponses.DidntUnderstandMessage));
                                if (_skillMode)
                                {
                                    await CompleteAsync(dc);
                                }
                            }

                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(ToDoMainResponses.FeatureNotAvailable));
                            if (_skillMode)
                            {
                                await CompleteAsync(dc);
                            }

                            break;
                        }
                }
            }
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_skillMode)
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
                case Events.SkillBeginEvent:
                    {
                        var state = await _toDoStateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());

                        if (dc.Context.Activity.Value is Dictionary<string, object> userData)
                        {
                            // Capture user data from event if needed
                        }

                        break;
                    }

                case Events.TokenResponseEvent:
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
                var localeConfig = _services.LocaleConfigurations[locale];

                // Update state with email luis result and entities
                var toDoLuisResult = await localeConfig.LuisServices["todo"].RecognizeAsync<ToDo>(dc.Context, cancellationToken);
                var state = await _toDoStateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());
                state.LuisResult = toDoLuisResult;

                // check luis intent
                localeConfig.LuisServices.TryGetValue("general", out var luisService);

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
            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(ToDoMainResponses.CancelMessage));
            await CompleteAsync(dc);
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(ToDoMainResponses.HelpMessage));
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

            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(ToDoMainResponses.LogOut));

            return InterruptionAction.StartedDialog;
        }

        private void RegisterDialogs()
        {
            AddDialog(new AddToDoItemDialog(_services, _toDoStateAccessor, _userStateAccessor, _serviceManager, TelemetryClient));
            AddDialog(new MarkToDoItemDialog(_services, _toDoStateAccessor, _userStateAccessor, _serviceManager, TelemetryClient));
            AddDialog(new DeleteToDoItemDialog(_services, _toDoStateAccessor, _userStateAccessor, _serviceManager, TelemetryClient));
            AddDialog(new ShowToDoItemDialog(_services, _toDoStateAccessor, _userStateAccessor, _serviceManager, TelemetryClient));
        }

        private void InitializeConfig(ToDoSkillState state)
        {
            // Initialize PageSize, ReadSize and TaskServiceType when the first input comes.
            if (state.PageSize <= 0)
            {
                int pageSize = 0;
                if (_services.Properties.TryGetValue("DisplaySize", out object displaySizeObj))
                {
                    int.TryParse(displaySizeObj.ToString(), out pageSize);
                }

                state.PageSize = pageSize <= 0 || pageSize > ToDoCommonUtil.MaxDisplaySize ? ToDoCommonUtil.MaxDisplaySize : pageSize;
            }

            if (state.ReadSize <= 0)
            {
                int readSize = 0;
                if (_services.Properties.TryGetValue("ReadSize", out object readSizeObj))
                {
                    int.TryParse(readSizeObj.ToString(), out readSize);
                }

                state.ReadSize = readSize <= 0 || readSize > ToDoCommonUtil.MaxReadSize ? ToDoCommonUtil.MaxReadSize : readSize;
            }

            if (state.TaskServiceType == ProviderTypes.Other)
            {
                state.TaskServiceType = ProviderTypes.Outlook;
                if (_services.Properties.TryGetValue("TaskServiceProvider", out object taskServiceProvider))
                {
                    if (taskServiceProvider.ToString().Equals(ProviderTypes.OneNote.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        state.TaskServiceType = ProviderTypes.OneNote;
                    }
                }
            }
        }

        private class Events
        {
            public const string TokenResponseEvent = "tokens/response";
            public const string SkillBeginEvent = "skillBegin";
        }
    }
}