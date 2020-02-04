// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using WhoSkill.Models;
using WhoSkill.Responses.Main;
using WhoSkill.Responses.Shared;
using WhoSkill.Services;
using WhoSkill.Utilities;

namespace WhoSkill.Dialogs
{
    public class MainDialog : ActivityHandlerDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private IStatePropertyAccessor<WhoSkillState> _whoStateAccessor;
        private LocaleTemplateEngineManager _templateEngine;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            WhoIsDialog whoIsDialog,
            ManagerDialog managerDialog,
            DirectReportsDialog directReportsDialog,
            PeersDialog peersDialog,
            EmailAboutDialog emailAboutDialog,
            MeetAboutDialog meetAboutDialog,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _whoStateAccessor = conversationState.CreateProperty<WhoSkillState>(nameof(WhoSkillState));
            _templateEngine = localeTemplateEngineManager;
            TelemetryClient = telemetryClient;

            // RegisterDialogs
            AddDialog(whoIsDialog ?? throw new ArgumentNullException(nameof(WhoIsDialog)));
            AddDialog(managerDialog ?? throw new ArgumentNullException(nameof(ManagerDialog)));
            AddDialog(directReportsDialog ?? throw new ArgumentNullException(nameof(DirectReportsDialog)));
            AddDialog(peersDialog ?? throw new ArgumentNullException(nameof(PeersDialog)));
            AddDialog(emailAboutDialog ?? throw new ArgumentNullException(nameof(EmailAboutDialog)));
            AddDialog(meetAboutDialog ?? throw new ArgumentNullException(nameof(MeetAboutDialog)));
        }

        protected override async Task OnMembersAddedAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = _templateEngine.GenerateActivityForLocale(WhoMainResponses.WhoWelcomeMessage);
            await dc.Context.SendActivityAsync(activity);
        }

        protected override async Task OnMessageActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _whoStateAccessor.GetAsync(dc.Context, () => new WhoSkillState());

            // Initialize the PageSize and ReadSize parameters in state from configuration
            await InitializeConfig(dc);

            // Init other properties in state before BeginDialog.
            await InitializeState(dc);

            // Route dialogs.
            var luisResult = dc.Context.TurnState.Get<WhoLuis>(StateProperties.WhoLuisResultKey);
            var intent = luisResult?.TopIntent().intent;
            switch (intent)
            {
                case WhoLuis.Intent.WhoIs:
                case WhoLuis.Intent.JobTitle:
                case WhoLuis.Intent.Department:
                case WhoLuis.Intent.Location:
                case WhoLuis.Intent.PhoneNumber:
                case WhoLuis.Intent.EmailAddress:
                    {
                        await dc.BeginDialogAsync(nameof(WhoIsDialog));
                        break;
                    }

                case WhoLuis.Intent.Manager:
                    {
                        await dc.BeginDialogAsync(nameof(ManagerDialog));
                        break;
                    }

                case WhoLuis.Intent.DirectReports:
                    {
                        await dc.BeginDialogAsync(nameof(DirectReportsDialog));
                        break;
                    }

                case WhoLuis.Intent.Peers:
                    {
                        await dc.BeginDialogAsync(nameof(PeersDialog));
                        break;
                    }

                case WhoLuis.Intent.EmailAbout:
                    {
                        await dc.BeginDialogAsync(nameof(EmailAboutDialog));
                        break;
                    }

                case WhoLuis.Intent.MeetAbout:
                    {
                        await dc.BeginDialogAsync(nameof(MeetAboutDialog));
                        break;
                    }

                default:
                    {
                        var activity = _templateEngine.GenerateActivityForLocale(WhoSharedResponses.DidntUnderstandMessage);
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
                localizedServices.LuisServices.TryGetValue("Who", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<WhoLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.WhoLuisResultKey, skillResult);
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
            if (dc.Context.Adapter is IRemoteUserTokenProvider remoteInvocationAdapter || Channel.GetChannelId(dc.Context) != Channels.Msteams)
            {
                var response = dc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.Handoff;

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
                            response.Type = ActivityTypes.Handoff;

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
                var state = await _whoStateAccessor.GetAsync(dc.Context, () => new WhoSkillState());
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
            var activity = _templateEngine.GenerateActivityForLocale(WhoMainResponses.CancelMessage);
            await dc.Context.SendActivityAsync(activity);
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.End;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            var activity = _templateEngine.GenerateActivityForLocale(WhoMainResponses.HelpMessage);
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

            var activity = _templateEngine.GenerateActivityForLocale(WhoMainResponses.LogOut);
            await dc.Context.SendActivityAsync(activity);
            return InterruptionAction.End;
        }

        private async Task InitializeConfig(DialogContext dc)
        {
            var state = await _whoStateAccessor.GetAsync(dc.Context);

            // Initialize PageSize when the first input comes.
            if (state.PageSize <= 0)
            {
                var pageSize = _settings.DisplaySize;
                state.PageSize = pageSize <= 0 ? WhoCommonUtil.DefaultDisplaySize : pageSize;
            }
        }

        private async Task InitializeState(DialogContext dc)
        {
            var state = await _whoStateAccessor.GetAsync(dc.Context);
            state.Init();

            var luisResult = dc.Context.TurnState.Get<WhoLuis>(StateProperties.WhoLuisResultKey);
            var entities = luisResult.Entities;
            var topIntent = luisResult.TopIntent().intent;

            // Save trigger intent.
            state.TriggerIntent = topIntent;

            // User searchs about current user himself.
            if (entities != null && entities.pron != null && entities.pron.Any())
            {
                state.SearchCurrentUser = true;
            }

            // Save the keyword that user want to search.
            if (entities != null && entities.keyword != null)
            {
                state.Keyword = entities.keyword[0];
            }
        }
    }
}