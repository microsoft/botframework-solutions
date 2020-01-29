// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Middleware;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using SkillSample.Extensions;
using SkillSample.Services;

namespace SkillSample.Adapters
{
    public class DefaultAdapter : BotFrameworkHttpAdapter
    {
        public DefaultAdapter(
            BotSettings settings,
            UserState userState,
            ConversationState conversationState,
            IChannelProvider channelProvider,
            ICredentialProvider credentialProvider,
            LocaleTemplateEngineManager templateEngine,
            TelemetryInitializerMiddleware telemetryMiddleware,
            IBotTelemetryClient telemetryClient)
            : base(credentialProvider, channelProvider)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Exception Message: {exception.Message}, Stack: {exception.StackTrace}"));
                await turnContext.SendActivityAsync(templateEngine.GenerateActivityForLocale("ErrorMessage"));
                telemetryClient.TrackException(exception);

                if (turnContext.IsSkill())
                {
                    // Send and EndOfConversation activity to the skill caller with the error to end the conversation
                    // and let the caller decide what to do.
                    var endOfConversation = Activity.CreateEndOfConversationActivity();
                    endOfConversation.Code = "SkillError";
                    endOfConversation.Text = exception.Message;
                    await turnContext.SendActivityAsync(endOfConversation);
                }
            };

            Use(telemetryMiddleware);

            // Uncomment the following line for local development without Azure Storage
            // Use(new TranscriptLoggerMiddleware(new MemoryTranscriptStore()));
            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
            Use(new SkillMiddleware(userState, conversationState, conversationState.CreateProperty<DialogState>(nameof(DialogState))));
            Use(new SetSpeakMiddleware());
        }
    }
}