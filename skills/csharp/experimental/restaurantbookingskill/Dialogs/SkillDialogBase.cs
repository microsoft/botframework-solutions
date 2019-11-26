﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Microsoft.Recognizers.Text.DateTime;
using RestaurantBookingSkill.Models;
using RestaurantBookingSkill.Responses.Shared;
using RestaurantBookingSkill.Services;
using Constants = Microsoft.Recognizers.Text.DataTypes.TimexExpression.Constants;

namespace RestaurantBookingSkill.Dialogs
{
    public class SkillDialogBase : ComponentDialog
    {
        public SkillDialogBase(
            string dialogId,
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            IBotTelemetryClient telemetryClient)
            : base(dialogId)
        {
            Settings = settings;
            Services = services;
            ResponseManager = responseManager;
            ConversationStateAccessor = conversationState.CreateProperty<RestaurantBookingState>(nameof(BookingDialog));
            UserStateAccessor = userState.CreateProperty<SkillUserState>(nameof(SkillUserState));
            TelemetryClient = telemetryClient;
        }

        protected BotSettings Settings { get; set; }

        protected BotServices Services { get; set; }

        protected IStatePropertyAccessor<RestaurantBookingState> ConversationStateAccessor { get; set; }

        protected IStatePropertyAccessor<SkillUserState> UserStateAccessor { get; set; }

        protected ResponseManager ResponseManager { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            await GetLuisResult(dc);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await GetLuisResult(dc);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        // Helpers
        protected async Task GetLuisResult(DialogContext dc)
        {
            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Adaptive card responses come through with empty text properties
                if (!string.IsNullOrEmpty(dc.Context.Activity.Text))
                {
                    var state = await ConversationStateAccessor.GetAsync(dc.Context);

                    // Get luis service for current locale
                    var localeConfig = Services.GetCognitiveModels();
                    var luisService = localeConfig.LuisServices["Restaurant"];

                    // Get intent and entities for activity
                    var result = await luisService.RecognizeAsync<ReservationLuis>(dc.Context, CancellationToken.None);
                    state.LuisResult = result;

                    // Extract key data out into state ready for use
                    await DigestLuisResult(dc, result);
                }
            }
        }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(RestaurantBookingSharedResponses.ErrorMessage));

            // clear state
            var state = await ConversationStateAccessor.GetAsync(sc.Context);
            state.Clear();
        }

        protected async Task DigestLuisResult(DialogContext dc, ReservationLuis luisResult)
        {
            try
            {
                var state = await ConversationStateAccessor.GetAsync(dc.Context);

                // Extract entities and store in state here.
                if (luisResult != null)
                {
                    var entities = luisResult.Entities;

                    // Extract the cuisines out (already normalized to canonical form) and put in State thus slot-filling for the dialog.
                    if (entities.cuisine != null)
                    {
                        foreach (var cuisine in entities.cuisine)
                        {
                            var type = cuisine.First<string>();
                            state.Booking.Category = type;
                        }
                    }

                    if (entities.datetime != null)
                    {
                        var results = DateTimeRecognizer.RecognizeDateTime(dc.Context.Activity.Text, CultureInfo.CurrentUICulture.ToString());
                        if (results.Count > 0)
                        {
                            // We only care about presence of one DateTime
                            var result = results.First();

                            // The resolution could include two example values: one for AM and one for PM.
                            var distinctTimexExpressions = new HashSet<string>();
                            var values = (List<Dictionary<string, string>>)result.Resolution["values"];
                            foreach (var value in values)
                            {
                                // Each result includes a TIMEX expression that captures the inherent date but not time ambiguity.
                                // We are interested in the distinct set of TIMEX expressions.
                                if (value.TryGetValue("timex", out var timex))
                                {
                                    distinctTimexExpressions.Add(timex);
                                }
                            }

                            // Now we have the timex properties let's see if we have a definite date and time
                            // If so we slot-fill this and move on, if we don't we'll ignore for now meaning the user will be prompted
                            var timexProperty = new TimexProperty(distinctTimexExpressions.First());

                            if (timexProperty.Types.Contains(Constants.TimexTypes.Date) && timexProperty.Types.Contains(Constants.TimexTypes.Definite))
                            {
                                // We have definite date (no ambiguity)
                                state.Booking.ReservationDate = new DateTime(timexProperty.Year.Value, timexProperty.Month.Value, timexProperty.DayOfMonth.Value);

                                // Timex doesn't capture time ambiguity (e.g. 4 rather than 4pm)
                                if (timexProperty.Types.Contains(Constants.TimexTypes.Time))
                                {
                                    // If we have multiple TimeX
                                    if (distinctTimexExpressions.Count == 1)
                                    {
                                        // We have definite time (no ambiguity)
                                        state.Booking.ReservationTime = DateTime.Parse($"{timexProperty.Hour.Value}:{timexProperty.Minute.Value}:{timexProperty.Second.Value}");
                                    }
                                    else
                                    {
                                        // We don't have a distinct time so add the TimeEx expressions to enable disambiguation later and prepare the natural language versions
                                        foreach (var timex in distinctTimexExpressions)
                                        {
                                            var property = new TimexProperty(timex);
                                            state.AmbiguousTimexExpressions.Add(timex, property.ToNaturalLanguage(DateTime.Now));
                                        }
                                    }
                                }
                            }
                            else if (timexProperty.Types.Contains(Constants.TimexTypes.Time))
                            {
                                // We might have a time but no date (e.g. book a table for 4pm)
                                // If we have multiple timex (and time) this means we have a AM and PM component (e.g. ambiguous - book a table at 9)
                                if (distinctTimexExpressions.Count == 1)
                                {
                                    state.Booking.ReservationTime = DateTime.Parse($"{timexProperty.Hour.Value}:{timexProperty.Minute.Value}:{timexProperty.Second.Value}");
                                }
                            }
                            else
                            {
                                // We don't have a distinct time so add the TimeEx expressions to enable disambiguation later and prepare the natural language versions
                                foreach (var timex in distinctTimexExpressions)
                                {
                                    var property = new TimexProperty(timex);
                                    state.AmbiguousTimexExpressions.Add(timex, property.ToNaturalLanguage(DateTime.Now));
                                }
                            }
                        }
                    }

                    if (entities.geographyV2 != null)
                    {
                        state.Booking.Location = entities.geographyV2.First().Location;
                    }

                    // Establishing attendee count can be problematic as the number entity can be picked up for poorly qualified
                    // times, e.g. book a restaurant tomorrow at 2 for 4 people so we rely on a composite entity
                    if (entities.attendees != null)
                    {
                        var attendeesComposite = entities.attendees.First();
                        if (attendeesComposite != null)
                        {
                            int.TryParse(attendeesComposite.number.First().ToString(), out var attendeeCount);
                            if (attendeeCount > 0)
                            {
                                state.Booking.AttendeeCount = attendeeCount;
                            }
                        }
                    }
                }
            }
            catch
            {
                // put log here
            }
        }

        // Workaround until adaptive card renderer in teams is upgraded to v1.2
        protected string GetDivergedCardName(ITurnContext turnContext, string card)
        {
            if (Channel.GetChannelId(turnContext) == Channels.Msteams)
            {
                return card + ".1.0";
            }
            else
            {
                return card;
            }
        }
    }
}