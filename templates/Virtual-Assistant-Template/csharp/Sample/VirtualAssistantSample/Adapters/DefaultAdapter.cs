// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Protocol.StreamingExtensions.NetCore;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using VirtualAssistantSample.Responses.Main;
using VirtualAssistantSample.Services;

namespace VirtualAssistantSample.Bots
{
    public class DefaultAdapter : WebSocketEnabledHttpAdapter
    {
        public DefaultAdapter(
            BotSettings settings,
            IConfiguration configuration,
            ICredentialProvider credentialProvider,
            IBotTelemetryClient telemetryClient,
            BotStateSet botStateSet)
            : base(configuration, credentialProvider)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"{exception.Message}"));
                await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"{exception.StackTrace}"));
                await turnContext.SendActivityAsync(MainStrings.ERROR);
                telemetryClient.TrackException(exception);
            };

            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
            Use(new SetSpeakMiddleware(settings.DefaultLocale));
            Use(new AutoSaveStateMiddleware(botStateSet));
        }
    }
}