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
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using WhoSkill.Models;
using WhoSkill.Services;
using WhoSkill.Utilities;

namespace WhoSkill.Dialogs
{
    public class MainDialog : ActivityHandlerDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private IStatePropertyAccessor<WhoSkillState> _whoStateAccessor;

        //private IStatePropertyAccessor<ToDoSkillState> _toDoStateAccessor;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            //LocaleTemplateEngineManager localeTemplateEngineManager,
            WhoIsDialog whoIsDialog,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _whoStateAccessor = conversationState.CreateProperty<WhoSkillState>(nameof(WhoSkillState));
            // TemplateEngine = localeTemplateEngineManager;
            TelemetryClient = telemetryClient;

            // RegisterDialogs
            AddDialog(whoIsDialog ?? throw new ArgumentNullException(nameof(WhoIsDialog)));
        }

        private LocaleTemplateEngineManager TemplateEngine { get; set; }

        protected override async Task OnMembersAddedAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await dc.Context.SendActivityAsync("hello");
        }

        protected override async Task OnMessageActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _whoStateAccessor.GetAsync(dc.Context, () => new WhoSkillState());

            // Initialize the PageSize and ReadSize parameters in state from configuration
            InitializeConfig(state);

            var luisResult = dc.Context.TurnState.Get<WhoLuis>(StateProperties.WhoLuisResultKey);
            var intent = luisResult?.TopIntent().intent;
            var generalLuisResult = dc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
            var generalTopIntent = generalLuisResult?.TopIntent().intent;

            // switch on general intents
            switch (intent)
            {
                case WhoLuis.Intent.WhoIs:
                    {
                        await dc.BeginDialogAsync(nameof(WhoIsDialog));
                        break;
                    }

                default:
                    {
                        // intent was identified but not yet implemented

                        //var activity = TemplateEngine.GenerateActivityForLocale(ToDoMainResponses.FeatureNotAvailable);
                        //await dc.Context.SendActivityAsync(activity);

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
            return InterruptionAction.NoAction;
        }

        private void InitializeConfig(WhoSkillState state)
        {
            // Initialize PageSize when the first input comes.
            if (state.PageSize <= 0)
            {
                var pageSize = _settings.DisplaySize;
                state.PageSize = pageSize <= 0 ? WhoCommonUtil.DefaultDisplaySize : pageSize;
            }
        }
    }
}