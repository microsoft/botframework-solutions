using System.Globalization;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Schema;

namespace EmailSkill.Adapters
{
    public class EmailSkillWebSocketBotAdapter : SkillWebSocketBotAdapter
    {
        public EmailSkillWebSocketBotAdapter(
            BotSettings settings,
            UserState userState,
            ConversationState conversationState,
            ResponseManager responseManager,
            IBotTelemetryClient telemetryClient,
            TelemetryInitializerMiddleware telemetryMiddleware)
            : base(null, telemetryClient)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                CultureInfo.CurrentUICulture = new CultureInfo(turnContext.Activity.Locale);
                await turnContext.SendActivityAsync(responseManager.GetResponse(EmailSharedResponses.EmailErrorMessage));
                await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Email Skill Error: {exception.Message} | {exception.StackTrace}"));
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