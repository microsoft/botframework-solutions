// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using ToDoSkill.Dialogs.Main.Resources;
using ToDoSkill.Dialogs.Shared.Resources;

namespace ToDoSkill
{
    public class MainDialog : RouterDialog
    {
        private bool _skillMode;
        private ISkillConfiguration _services;
        private UserState _userState;
        private ConversationState _conversationState;
        private ITaskService _serviceManager;
        private IStatePropertyAccessor<ToDoSkillState> _stateAccessor;
        private ToDoSkillResponseBuilder _responseBuilder = new ToDoSkillResponseBuilder();

        public MainDialog(
            ISkillConfiguration services,
            ConversationState conversationState,
            UserState userState,
            ITaskService serviceManager,
            bool skillMode)
            : base(nameof(MainDialog))
        {
            _skillMode = skillMode;
            _services = services;
            _conversationState = conversationState;
            _userState = userState;
            _serviceManager = serviceManager;

            // Initialize state accessor
            _stateAccessor = _conversationState.CreateProperty<ToDoSkillState>(nameof(ToDoSkillState));

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
            // If dispatch result is general luis model
            _services.LuisServices.TryGetValue("todo", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var result = await luisService.RecognizeAsync<ToDo>(dc.Context, CancellationToken.None);

                var intent = result?.TopIntent().intent;

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
                            // No intent was identified, send confused message
                            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(ToDoSharedResponses.DidntUnderstandMessage));
                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(ToDoMainResponses.FeatureNotAvailable));
                            break;
                        }
                }
            }
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_skillMode)
            {
                var response = dc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.EndOfConversation;

                await dc.Context.SendActivityAsync(response);
            }
            else
            {
                await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(ToDoSharedResponses.ActionEnded));
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
                        var state = await _stateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());

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
                // Update state with email luis result and entities
                var emailLuisResult = await _services.LuisServices["todo"].RecognizeAsync<ToDo>(dc.Context, cancellationToken);
                var state = await _stateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());
                state.LuisResult = emailLuisResult;

                // check luis intent
                _services.LuisServices.TryGetValue("general", out var luisService);

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
            await dc.BeginDialogAsync(nameof(CancelDialog));
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

            await dc.Context.SendActivityAsync("Ok, you're signed out.");

            return InterruptionAction.StartedDialog;
        }

        private void RegisterDialogs()
        {
            AddDialog(new AddToDoItemDialog(_services, _stateAccessor, _serviceManager));
            AddDialog(new MarkToDoItemDialog(_services, _stateAccessor, _serviceManager));
            AddDialog(new DeleteToDoItemDialog(_services, _stateAccessor, _serviceManager));
            AddDialog(new ShowToDoItemDialog(_services, _stateAccessor, _serviceManager));
            AddDialog(new CancelDialog());
        }

        private class Events
        {
            public const string TokenResponseEvent = "tokens/response";
            public const string SkillBeginEvent = "skillBegin";
        }
    }
}
