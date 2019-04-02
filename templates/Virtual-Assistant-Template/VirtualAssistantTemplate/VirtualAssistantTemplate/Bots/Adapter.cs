﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.Solutions.Telemetry;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using VirtualAssistantTemplate.Responses.Main;
using VirtualAssistantTemplate.Services;

namespace VirtualAssistantTemplate.Bots
{
    public class Adapter : BotFrameworkHttpAdapter
    {
        public Adapter(
            BotSettings settings,
            ICredentialProvider credentialProvider,
            UserState userState,
            ConversationState conversationState,
            IBotTelemetryClient telemetryClient) : base(credentialProvider)
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
            Use(new AutoSaveStateMiddleware(userState, conversationState));
        }
    }
}
