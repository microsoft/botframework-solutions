// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Schema;
using WhoSkill.Services;

namespace WhoSkill.Adapters
{
    public class WhoSkillWebSocketBotAdapter : SkillWebSocketBotAdapter
    {
        public WhoSkillWebSocketBotAdapter(
            BotSettings settings,
            UserState userState,
            ConversationState conversationState,
            //LocaleTemplateEngineManager localeTemplateEngineManager,
            IBotTelemetryClient telemetryClient,
            TelemetryInitializerMiddleware telemetryMiddleware)
            : base(null, telemetryClient)
        {
            OnTurnError = async (context, exception) =>
            {
                CultureInfo.CurrentUICulture = new CultureInfo(context.Activity.Locale);

                //var activity = localeTemplateEngineManager.GenerateActivityForLocale(ToDoSharedResponses.ToDoErrorMessage, context);
                //await context.SendActivityAsync(activity);

                await context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Who Skill Error: {exception.Message} | {exception.StackTrace}"));
                telemetryClient.TrackException(exception);
            };

            Use(telemetryMiddleware);
            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
            Use(new SkillMiddleware(userState, conversationState, conversationState.CreateProperty<DialogState>(nameof(DialogState))));
        }
    }
}