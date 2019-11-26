﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using NewsSkill.Models;
using NewsSkill.Responses.Main;
using NewsSkill.Services;

namespace NewsSkill.Dialogs
{
    public class MainDialog : ActivityHandlerDialog
    {
        private BotServices _services;
        private IBotTelemetryClient _telemetryClient;
        private ConversationState _conversationState;
        private MainResponses _responder = new MainResponses();
        private IStatePropertyAccessor<NewsSkillState> _stateAccessor;

        public MainDialog(
            BotServices services,
            ConversationState conversationState,
            FindArticlesDialog findArticlesDialog,
            TrendingArticlesDialog trendingArticlesDialog,
            FavoriteTopicsDialog favoriteTopicsDialog,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            _telemetryClient = telemetryClient;

            // Initialize state accessor
            _stateAccessor = _conversationState.CreateProperty<NewsSkillState>(nameof(NewsSkillState));

            AddDialog(findArticlesDialog ?? throw new ArgumentNullException(nameof(findArticlesDialog)));
            AddDialog(trendingArticlesDialog ?? throw new ArgumentNullException(nameof(trendingArticlesDialog)));
            AddDialog(favoriteTopicsDialog ?? throw new ArgumentNullException(nameof(favoriteTopicsDialog)));
        }

        protected override async Task OnMembersAddedAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // send a greeting if we're in local mode
            await _responder.ReplyWith(dc.Context, MainResponses.Intro);
        }

        protected override async Task OnMessageActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new NewsSkillState());

            // get current activity locale
            var localeConfig = _services.GetCognitiveModels();

            // Populate state from SemanticAction as required
            await PopulateStateFromSemanticAction(dc.Context);

            // If dispatch result is general luis model
            localeConfig.LuisServices.TryGetValue("News", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var result = await luisService.RecognizeAsync<NewsLuis>(dc.Context, CancellationToken.None);
                state.LuisResult = result;

                var intent = result?.TopIntent().intent;

                // switch on general intents
                switch (intent)
                {
                    case NewsLuis.Intent.TrendingArticles:
                        {
                            // send articles in response
                            await dc.BeginDialogAsync(nameof(TrendingArticlesDialog));
                            break;
                        }

                    case NewsLuis.Intent.SetFavoriteTopics:
                    case NewsLuis.Intent.ShowFavoriteTopics:
                        {
                            // send favorite news categories
                            await dc.BeginDialogAsync(nameof(FavoriteTopicsDialog));
                            break;
                        }

                    case NewsLuis.Intent.FindArticles:
                        {
                            // send greeting response
                            await dc.BeginDialogAsync(nameof(FindArticlesDialog));
                            break;
                        }

                    case NewsLuis.Intent.None:
                        {
                            // No intent was identified, send confused message
                            await _responder.ReplyWith(dc.Context, MainResponses.Confused);
                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await dc.Context.SendActivityAsync("This feature is not yet implemented in this skill.");
                            break;
                        }
                }
            }
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

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = InterruptionAction.NoAction;

            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                // check luis intent
                _services.CognitiveModelSets["en"].LuisServices.TryGetValue("General", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Skill configuration.");
                }
                else
                {
                    var luisResult = await luisService.RecognizeAsync<General>(dc.Context, cancellationToken);
                    var topIntent = luisResult.TopIntent();

                    if (topIntent.score > 0.5)
                    {
                        switch (topIntent.intent)
                        {
                            case General.Intent.Cancel:
                                {
                                    result = await OnCancel(dc);
                                    break;
                                }
                        }
                    }
                }
            }

            return result;
        }

        protected override async Task OnEventActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var ev = dc.Context.Activity.AsEventActivity();
            var value = ev.Value?.ToString();

            var state = await _stateAccessor.GetAsync(dc.Context, () => new NewsSkillState());

            switch (ev.Name)
            {
                case Events.Location:
                    {
                        // Test trigger with
                        // /event:{ "Name": "Location", "Value": "34.05222222222222,-118.2427777777777" }
                        if (!string.IsNullOrEmpty(value))
                        {
                            var coords = value.Split(',');
                            if (coords.Length == 2)
                            {
                                if (double.TryParse(coords[0], out var lat) && double.TryParse(coords[1], out var lng))
                                {
                                    state.CurrentCoordinates = value;
                                }
                            }
                        }

                        break;
                    }

                default:
                    {
                        await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{ev.Name ?? "undefined"}' was received but not processed."));
                        break;
                    }
            }
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            await _responder.ReplyWith(dc.Context, MainResponses.Cancelled);
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.End;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await _responder.ReplyWith(dc.Context, MainResponses.Help);
            return InterruptionAction.Resume;
        }

        private async Task PopulateStateFromSemanticAction(ITurnContext context)
        {
            // Populating local state with data passed through semanticAction out of Activity
            var activity = context.Activity;
            var semanticAction = activity.SemanticAction;
            if (semanticAction != null && semanticAction.Entities.ContainsKey("location"))
            {
                var location = semanticAction.Entities["location"];
                var locationObj = location.Properties["location"].ToString();
                var state = await _stateAccessor.GetAsync(context, () => new NewsSkillState());
                state.CurrentCoordinates = locationObj;
            }
        }

        public class Events
        {
            public const string Location = "Location";
        }
    }
}