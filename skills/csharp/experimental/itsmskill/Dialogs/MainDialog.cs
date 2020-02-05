// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Responses.Main;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Skills.Models;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using SkillServiceLibrary.Utilities;

namespace ITSMSkill.Dialogs
{
    public class MainDialog : ActivityHandlerDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private ResponseManager _responseManager;
        private IStatePropertyAccessor<SkillState> _stateAccessor;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            UserState userState,
            ConversationState conversationState,
            CreateTicketDialog createTicketDialog,
            UpdateTicketDialog updateTicketDialog,
            ShowTicketDialog showTicketDialog,
            CloseTicketDialog closeTicketDialog,
            ShowKnowledgeDialog showKnowledgeDialog,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _responseManager = responseManager;
            TelemetryClient = telemetryClient;

            // Initialize state accessor
            _stateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));

            // Register dialogs
            AddDialog(createTicketDialog);
            AddDialog(updateTicketDialog);
            AddDialog(showTicketDialog);
            AddDialog(closeTicketDialog);
            AddDialog(showKnowledgeDialog);
        }

        protected override async Task OnMembersAddedAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.WelcomeMessage));
        }

        protected override async Task OnMessageActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // get current activity locale
            var localeConfig = _services.GetCognitiveModels();

            // Populate state from SemanticAction as required
            await PopulateStateFromSemanticAction(dc.Context);

            // Get skill LUIS model from configuration
            localeConfig.LuisServices.TryGetValue("ITSM", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var result = await luisService.RecognizeAsync<ITSMLuis>(dc.Context, CancellationToken.None);
                var intent = result?.TopIntent().intent;

                if (intent != null && intent != ITSMLuis.Intent.None)
                {
                    var state = await _stateAccessor.GetAsync(dc.Context, () => new SkillState());
                    state.DigestLuisResult(result, (ITSMLuis.Intent)intent);
                }

                switch (intent)
                {
                    case ITSMLuis.Intent.TicketCreate:
                        {
                            await dc.BeginDialogAsync(nameof(CreateTicketDialog));
                            break;
                        }

                    case ITSMLuis.Intent.TicketUpdate:
                        {
                            await dc.BeginDialogAsync(nameof(UpdateTicketDialog));
                            break;
                        }

                    case ITSMLuis.Intent.TicketShow:
                        {
                            await dc.BeginDialogAsync(nameof(ShowTicketDialog));
                            break;
                        }

                    case ITSMLuis.Intent.TicketClose:
                        {
                            await dc.BeginDialogAsync(nameof(CloseTicketDialog));
                            break;
                        }

                    case ITSMLuis.Intent.KnowledgeShow:
                        {
                            await dc.BeginDialogAsync(nameof(ShowKnowledgeDialog));
                            break;
                        }

                    case ITSMLuis.Intent.None:
                        {
                            // No intent was identified, send confused message
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(SharedResponses.DidntUnderstandMessage));
                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.FeatureNotAvailable));
                            break;
                        }
                }
            }
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

            // End active dialog.
            await dc.EndDialogAsync(result);
        }

        protected override async Task OnEventActivityAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var ev = innerDc.Context.Activity.AsEventActivity();
            var value = ev.Value?.ToString();

            switch (ev.Name)
            {
                case TokenEvents.TokenResponseEventName:
                    {
                        // Forward the token response activity to the dialog waiting on the stack.
                        await innerDc.ContinueDialogAsync();
                        break;
                    }

                default:
                    {
                        await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{ev.Name ?? "undefined"}' was received but not processed."));
                        break;
                    }
            }
        }

        protected override async Task OnUnhandledActivityTypeAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown activity was received but not processed."));
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = InterruptionAction.NoAction;

            if (dc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(dc.Context.Activity.Text))
            {
                // get current activity locale
                var localeConfig = _services.GetCognitiveModels();

                // check general luis intent
                localeConfig.LuisServices.TryGetValue("General", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Skill configuration.");
                }
                else
                {
                    var luisResult = await luisService.RecognizeAsync<GeneralLuis>(dc.Context, cancellationToken);

                    var state = await _stateAccessor.GetAsync(dc.Context, () => new SkillState());

                    var topIntent = luisResult.TopIntent();

                    if (topIntent.score > 0.5)
                    {
                        state.GeneralIntent = topIntent.intent;
                        switch (topIntent.intent)
                        {
                            case GeneralLuis.Intent.Cancel:
                                {
                                    result = await OnCancel(dc);
                                    break;
                                }

                            case GeneralLuis.Intent.Help:
                                {
                                    result = await OnHelp(dc);
                                    break;
                                }

                            case GeneralLuis.Intent.Logout:
                                {
                                    result = await OnLogout(dc);
                                    break;
                                }
                        }
                    }
                    else
                    {
                        state.GeneralIntent = GeneralLuis.Intent.None;
                    }
                }
            }

            return result;
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.CancelMessage));
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.End;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.HelpMessage));
            return InterruptionAction.Resume;
        }

        private async Task<InterruptionAction> OnLogout(DialogContext dc)
        {
            IUserTokenProvider tokenProvider;
            var supported = dc.Context.Adapter is IUserTokenProvider;
            if (supported)
            {
                tokenProvider = (IUserTokenProvider)dc.Context.Adapter;

                // Sign out user
                var tokens = await tokenProvider.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
                foreach (var token in tokens)
                {
                    await tokenProvider.SignOutUserAsync(dc.Context, token.ConnectionName);
                }

                // Cancel all active dialogs
                await dc.CancelAllDialogsAsync();
            }
            else
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }

            await dc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.LogOut));

            return InterruptionAction.End;
        }

        private async Task PopulateStateFromSemanticAction(ITurnContext context)
        {
            // Example of populating local state with data passed through semanticAction out of Activity
            var activity = context.Activity;
            var semanticAction = activity.SemanticAction;

            // if (semanticAction != null && semanticAction.Entities.ContainsKey("location"))
            // {
            //    var location = semanticAction.Entities["location"];
            //    var locationObj = location.Properties["location"].ToString();
            //    var state = await _stateAccessor.GetAsync(context, () => new SkillState());
            //    state.CurrentCoordinates = locationObj;
            // }
        }
    }
}