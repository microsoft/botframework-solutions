// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Services;

namespace PointOfInterestSkill.Adapters
{
    public class DefaultAdapter : BotFrameworkHttpAdapter
    {
        private ConversationState _conversationState;

        public DefaultAdapter(
            BotSettings settings,
            ICredentialProvider credentialProvider,
            TelemetryInitializerMiddleware telemetryMiddleware,
            IBotTelemetryClient telemetryClient,
            ResponseManager responseManager,
            ConversationState conversationState)
            : base(credentialProvider)
        {
            _conversationState = conversationState;

            OnTurnError = async (context, exception) =>
            {
                if (await HandleSerializationFailure(context, exception))
                {
                    return;
                }

                await context.SendActivityAsync(responseManager.GetResponse(POISharedResponses.PointOfInterestErrorMessage));
                await context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"PointOfInterest Skill Error: {exception.Message} | {exception.StackTrace}"));
                telemetryClient.TrackException(exception);
            };

            Use(telemetryMiddleware);

            // Uncomment the following line for local development without Azure Storage
            // Use(new TranscriptLoggerMiddleware(new MemoryTranscriptStore()));
            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
        }

        private async Task<bool> HandleSerializationFailure(ITurnContext context, Exception exception)
        {
            if (exception is JsonSerializationException)
            {
                // TODO a stateless branch method
                int hashCode = Math.Abs((context.Activity.From.Id + "/" + context.Activity.Conversation.Id).GetHashCode());
                if (int.TryParse(context.Activity.Text, out int result) && result == hashCode)
                {
                    await _conversationState.ClearStateAsync(context).ConfigureAwait(false);
                    await _conversationState.SaveChangesAsync(context, force: true).ConfigureAwait(false);
                    await context.SendActivityAsync($"Your state is successfully reset.");
                    return true;
                }
                else
                {
                    await context.SendActivityAsync($"Your state is broken. Enter {hashCode} to reset state if you like.");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}