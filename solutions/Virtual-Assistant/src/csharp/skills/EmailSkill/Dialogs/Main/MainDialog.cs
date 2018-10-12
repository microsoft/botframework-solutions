﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.Main.Resources;
using EmailSkill.Dialogs.Shared.Resources;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;

namespace EmailSkill
{
    public class MainDialog : RouterDialog
    {
        private bool _skillMode;
        private SkillConfiguration _services;
        private UserState _userState;
        private ConversationState _conversationState;
        private IMailSkillServiceManager _serviceManager;
        private IStatePropertyAccessor<EmailSkillState> _stateAccessor;
        private IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private EmailSkillResponseBuilder _responseBuilder = new EmailSkillResponseBuilder();

        public MainDialog(SkillConfiguration services, ConversationState conversationState, UserState userState, IMailSkillServiceManager serviceManager, bool skillMode)
            : base(nameof(MainDialog))
        {
            _skillMode = skillMode;
            _services = services;
            _conversationState = conversationState;
            _userState = userState;
            _serviceManager = serviceManager;

            // Initialize state accessor
            _stateAccessor = _conversationState.CreateProperty<EmailSkillState>(nameof(EmailSkillState));
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));

            RegisterDialogs();
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_skillMode)
            {
                // send a greeting if we're in local mode
                await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(EmailMainResponses.EmailWelcomeMessage));
            }
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new EmailSkillState());

            // If dispatch result is general luis model
            _services.LuisServices.TryGetValue("email", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var result = await luisService.RecognizeAsync<Email>(dc.Context, CancellationToken.None);
                var intent = result?.TopIntent().intent;

                var skillOptions = new EmailSkillDialogOptions
                {
                    SkillMode = _skillMode,
                };

                // switch on general intents
                switch (intent)
                {
                    case Email.Intent.SendEmail:
                        {
                            await dc.BeginDialogAsync(nameof(SendEmailDialog), skillOptions);
                            break;
                        }

                    case Email.Intent.Forward:
                        {
                            await dc.BeginDialogAsync(nameof(ForwardEmailDialog), skillOptions);
                            break;
                        }

                    case Email.Intent.Reply:
                        {
                            await dc.BeginDialogAsync(nameof(ReplyEmailDialog), skillOptions);
                            break;
                        }

                    case Email.Intent.SearchMessages:
                    case Email.Intent.ShowNext:
                    case Email.Intent.ShowPrevious:
                    case Email.Intent.CheckMessages:
                        {
                            await dc.BeginDialogAsync(nameof(ShowEmailDialog), skillOptions);
                            break;
                        }

                    case Email.Intent.None:
                        {
                            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(EmailSharedResponses.DidntUnderstandMessage));
                            if (_skillMode)
                            {
                                await CompleteAsync(dc);
                            }

                            break;
                        }

                    default:
                        {
                            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(EmailMainResponses.FeatureNotAvailable));

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
            else
            {
                await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(EmailSharedResponses.ActionEnded));
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
                        var state = await _stateAccessor.GetAsync(dc.Context, () => new EmailSkillState());

                        if (dc.Context.Activity.Value is Dictionary<string, object> userData)
                        {
                            if (userData.TryGetValue("IPA.Timezone", out var timezone))
                            {
                                // we have a timezone
                                state.UserInfo.Timezone = (TimeZoneInfo)timezone;
                            }
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
                var emailLuisResult = await _services.LuisServices["email"].RecognizeAsync<Email>(dc.Context, cancellationToken);
                var state = await _stateAccessor.GetAsync(dc.Context, () => new EmailSkillState());
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
                    var topIntent = luisResult.TopIntent().intent;

                    // check intent
                    switch (topIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                result = await OnCancel(dc);
                                break;
                            }

                        case General.Intent.Help:
                            {
                                result = await OnHelp(dc);
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
            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(EmailMainResponses.HelpMessage));
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
            await adapter.SignOutUserAsync(dc.Context, _services.AuthConnectionName);
            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(EmailMainResponses.LogOut));

            return InterruptionAction.StartedDialog;
        }

        private void RegisterDialogs()
        {
            AddDialog(new ForwardEmailDialog(_services, _stateAccessor, _dialogStateAccessor, _serviceManager));
            AddDialog(new SendEmailDialog(_services, _stateAccessor, _dialogStateAccessor, _serviceManager));
            AddDialog(new ShowEmailDialog(_services, _stateAccessor, _dialogStateAccessor, _serviceManager));
            AddDialog(new ReplyEmailDialog(_services, _stateAccessor, _dialogStateAccessor, _serviceManager));
            AddDialog(new CancelDialog());
        }

        private class Events
        {
            public const string TokenResponseEvent = "tokens/response";
            public const string SkillBeginEvent = "skillBegin";
        }
    }
}
