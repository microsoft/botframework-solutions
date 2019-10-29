﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.StreamingExtensions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using VirtualAssistantSample.Services;
using ActivityGenerator = Microsoft.Bot.Builder.Dialogs.Adaptive.Generators.ActivityGenerator;

namespace VirtualAssistantSample.Adapters
{
    public class DefaultWebSocketAdapter : WebSocketEnabledHttpAdapter
    {
        public DefaultWebSocketAdapter(
            IConfiguration config,
            BotSettings settings,
            LocaleTemplateEngineManager templateEngine,
            ICredentialProvider credentialProvider,
            TelemetryInitializerMiddleware telemetryMiddleware,
            IBotTelemetryClient telemetryClient)
            : base(config, credentialProvider)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Exception Message: {exception.Message}, Stack: {exception.StackTrace}"));
                await turnContext.SendActivityAsync(templateEngine.GenerateActivityForLocale("ErrorMessage"));

                telemetryClient.TrackException(exception);
            };

            Use(telemetryMiddleware);

            // Uncomment the following line for local development without Azure Storage
            // Use(new TranscriptLoggerMiddleware(new MemoryTranscriptStore()));
            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
            Use(new SetSpeakMiddleware(settings.DefaultLocale ?? "en-us"));
        }
    }
}