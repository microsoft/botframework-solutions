﻿using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Telemetry;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using SkillSample.Responses.Shared;
using SkillSample.Services;
using System.Globalization;

namespace SkillSample.Adapters
{
    public class CustomSkillAdapter : SkillAdapter
    {
        public CustomSkillAdapter(
            BotSettings settings,
            ICredentialProvider credentialProvider,
            BotStateSet botStateSet,
            ResponseManager responseManager,
            IBotTelemetryClient telemetryClient,
            UserState userState)
            : base(credentialProvider)
        {
            OnTurnError = async (context, exception) =>
            {
                CultureInfo.CurrentUICulture = new CultureInfo(context.Activity.Locale);
                await context.SendActivityAsync(responseManager.GetResponse(SharedResponses.ErrorMessage));
                await context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Skill Error: {exception.Message} | {exception.StackTrace}"));
                telemetryClient.TrackExceptionEx(exception, context.Activity);
            };

            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
            Use(new AutoSaveStateMiddleware(botStateSet));
            Use(new SkillMiddleware(userState));
        }
    }
}
