// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Dialogs.Declarative.Types;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Solutions.Feedback;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using VirtualAssistantSample.Services;

namespace VirtualAssistantSample.Adapters
{
    public class DefaultAdapter : BotFrameworkHttpAdapter
    {
        public DefaultAdapter(
            IConfiguration configuration,
            BotSettings settings,
            ConversationState conversationState,
            ResourceExplorer resourceExplorer,
            ICredentialProvider credentialProvider,
            IBotTelemetryClient telemetryClient)
            : base(credentialProvider)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                var activityGenerator = turnContext.TurnState.Get<IActivityGenerator>();
                await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"{exception.Message}"));
                await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"{exception.StackTrace}"));
                await turnContext.SendActivityAsync(await activityGenerator.Generate(turnContext, "[errorMessage]", null));
                telemetryClient.TrackException(exception);
            };

            TypeFactory.Configuration = configuration;
            this.UseLanguageGeneration(resourceExplorer, "MainResponses.lg");

            // Uncomment the following line for local development without Azure Storage
            // Use(new TranscriptLoggerMiddleware(new MemoryTranscriptStore()));
            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new ShowTypingMiddleware());
            Use(new FeedbackMiddleware(conversationState, telemetryClient));
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
        }
    }
}