﻿using System.Globalization;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Schema;
using RestaurantBooking.Responses.Shared;
using RestaurantBooking.Services;

namespace RestaurantBooking.Adapters
{
    public class RestaurantSkillWebSocketBotAdapter : SkillWebSocketBotAdapter
    {
        public RestaurantSkillWebSocketBotAdapter(
            BotSettings settings,
            UserState userState,
            ConversationState conversationState,
            ResponseManager responseManager,
            IBotTelemetryClient telemetryClient)
        {
            OnTurnError = async (context, exception) =>
            {
                CultureInfo.CurrentUICulture = new CultureInfo(context.Activity.Locale);
                await context.SendActivityAsync(responseManager.GetResponse(RestaurantBookingSharedResponses.ErrorMessage));
                await context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Restaurant Booking Skill Error: {exception.Message} | {exception.StackTrace}"));
                telemetryClient.TrackException(exception);
            };

            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
            Use(new SkillMiddleware(userState, conversationState, conversationState.CreateProperty<DialogState>(nameof(RestaurantBooking))));
        }
    }
}