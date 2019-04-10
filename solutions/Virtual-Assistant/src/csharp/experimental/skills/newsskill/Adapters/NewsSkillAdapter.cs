using System.Globalization;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.Solutions.Shared.Telemetry;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using NewsSkill.Responses.Main;
using NewsSkill.Services;

namespace NewsSkill.Adapters
{
    public class NewsSkillAdapter : SkillAdapter
    {
        public NewsSkillAdapter(
            BotSettings settings,
            ICredentialProvider credentialProvider,
            UserState userState,
            ConversationState conversationState,
            IBotTelemetryClient telemetryClient)
            : base(credentialProvider)
        {
            OnTurnError = async (context, exception) =>
            {
                CultureInfo.CurrentUICulture = new CultureInfo(context.Activity.Locale);
                await context.SendActivityAsync(MainStrings.ERROR);
                await context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"News Skill Error: {exception.Message} | {exception.StackTrace}"));
                telemetryClient.TrackExceptionEx(exception, context.Activity);
            };

            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
            Use(new AutoSaveStateMiddleware(userState, conversationState));
        }
    }
}
