using System.Globalization;
using AutomotiveSkill.Responses.Shared;
using AutomotiveSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Telemetry;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace AutomotiveSkill.Adapters
{
    public class AutomotiveSkillAdapter : SkillAdapter
    {
        public AutomotiveSkillAdapter(
            BotSettings settings,
            ICredentialProvider credentialProvider,
            UserState userState,
            ConversationState conversationState,
            ResponseManager responseManager,
            IBotTelemetryClient telemetryClient)
            : base(credentialProvider)
        {
            OnTurnError = async (context, exception) =>
            {
                CultureInfo.CurrentUICulture = new CultureInfo(context.Activity.Locale);
                await context.SendActivityAsync(responseManager.GetResponse(AutomotiveSkillSharedResponses.ErrorMessage));
                await context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Automotive Skill Error: {exception.Message} | {exception.StackTrace}"));
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