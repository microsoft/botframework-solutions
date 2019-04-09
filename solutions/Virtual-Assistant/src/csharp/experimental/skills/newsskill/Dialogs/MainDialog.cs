// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Schema;
using NewsSkill.Models;
using NewsSkill.Responses.Main;
using NewsSkill.Services;

namespace NewsSkill.Dialogs
{
    public class MainDialog : RouterDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private UserState _userState;
        private IBotTelemetryClient _telemetryClient;
        private ConversationState _conversationState;
        private MainResponses _responder = new MainResponses();
        private IStatePropertyAccessor<NewsSkillState> _stateAccessor;
        private IStatePropertyAccessor<DialogState> _dialogStateAccessor;

        public MainDialog(BotSettings settings, BotServices services, ConversationState conversationState, UserState userState, IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));

            _telemetryClient = telemetryClient;

            // Initialize state accessor
            _stateAccessor = _conversationState.CreateProperty<NewsSkillState>(nameof(NewsSkillState));
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));

            RegisterDialogs();
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // send a greeting if we're in local mode
            await _responder.ReplyWith(dc.Context, MainResponses.Intro);
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new NewsSkillState());

            // If dispatch result is general luis model
            _services.CognitiveModelSets["en"].LuisServices.TryGetValue("news", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var turnResult = EndOfTurn;
                var result = await luisService.RecognizeAsync<NewsLuis>(dc.Context, CancellationToken.None);
                state.LuisResult = result;

                var intent = result?.TopIntent().intent;

                // switch on general intents
                switch (intent)
                {
                    case NewsLuis.Intent.FindArticles:
                        {
                            // send greeting response
                            turnResult = await dc.BeginDialogAsync(nameof(FindArticlesDialog));
                            break;
                        }

                    case NewsLuis.Intent.None:
                        {
                            // No intent was identified, send confused message
                            await _responder.ReplyWith(dc.Context, MainResponses.Confused);
                            turnResult = new DialogTurnResult(DialogTurnStatus.Complete);

                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await dc.Context.SendActivityAsync("This feature is not yet implemented in this skill.");
                            turnResult = new DialogTurnResult(DialogTurnStatus.Complete);

                            break;
                        }
                }

                if (turnResult != EndOfTurn)
                {
                    await CompleteAsync(dc);
                }
            }
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = dc.Context.Activity.CreateReply();
            response.Type = ActivityTypes.EndOfConversation;

            await dc.Context.SendActivityAsync(response);

            // End active dialog
            await dc.EndDialogAsync(result);
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = InterruptionAction.NoAction;

            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                // check luis intent
                _services.CognitiveModelSets["en"].LuisServices.TryGetValue("general", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Skill configuration.");
                }
                else
                {
                    var luisResult = await luisService.RecognizeAsync<General>(dc.Context, cancellationToken);
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
                    }
                }
            }

            return result;
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            await _responder.ReplyWith(dc.Context, MainResponses.Cancelled);
            await CompleteAsync(dc);
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await _responder.ReplyWith(dc.Context, MainResponses.Help);
            return InterruptionAction.MessageSentToUser;
        }

        private void RegisterDialogs()
        {
            AddDialog(new FindArticlesDialog(_settings, _services, _stateAccessor, _telemetryClient));
        }
    }
}