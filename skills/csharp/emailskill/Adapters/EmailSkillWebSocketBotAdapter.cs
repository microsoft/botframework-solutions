// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Schema;

namespace EmailSkill.Adapters
{
    public class EmailSkillWebSocketBotAdapter : SkillWebSocketBotAdapter
    {
        public EmailSkillWebSocketBotAdapter(
            BotSettings settings,
            UserState userState,
            ConversationState conversationState,
            IBotTelemetryClient telemetryClient,
            ResourceExplorer resourceExplorer,
            TelemetryInitializerMiddleware telemetryMiddleware)
            : base(null, telemetryClient)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                CultureInfo.CurrentUICulture = new CultureInfo(turnContext.Activity.Locale);
                var activity = await LGHelper.GenerateMessageAsync(turnContext, EmailSharedResponses.EmailErrorMessage, null);
                await turnContext.SendActivityAsync(activity);
                await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Email Skill Error: {exception.Message} | {exception.StackTrace}"));
                telemetryClient.TrackException(exception);
            };

            Use(telemetryMiddleware);
            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
            Use(new SkillMiddleware(userState, conversationState, conversationState.CreateProperty<DialogState>(nameof(DialogState))));

            this.UseState(userState, conversationState);
            this.UseResourceExplorer(resourceExplorer);
            this.UseLanguageGeneration(resourceExplorer, "ResponsesAndTexts.lg");
        }
    }
}