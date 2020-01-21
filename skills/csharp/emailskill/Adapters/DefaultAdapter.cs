// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace EmailSkill.Adapters
{
    public class DefaultAdapter : BotFrameworkHttpAdapter
    {
        public DefaultAdapter(
            BotSettings settings,
            ICredentialProvider credentialProvider,
            IBotTelemetryClient telemetryClient,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            UserState userState,
            ConversationState conversationState,
            TelemetryInitializerMiddleware telemetryMiddleware)
            : base(credentialProvider)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                var activity = localeTemplateEngineManager.GenerateActivityForLocale(EmailSharedResponses.EmailErrorMessage);
                await turnContext.SendActivityAsync(activity);
                await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Email Skill Error: {exception.Message} | {exception.StackTrace}"));
                telemetryClient.TrackException(exception);
            };

            Use(telemetryMiddleware);
            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
        }
    }
}