using System.Globalization;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Schema;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Utilities;

namespace PointOfInterestSkill.Adapters
{
    public class POISkillWebSocketBotAdapter : SkillWebSocketBotAdapter
    {
        private ResourceMultiLanguageGenerator _lgMultiLangEngine;

        public POISkillWebSocketBotAdapter(
            BotSettings settings,
            UserState userState,
            ConversationState conversationState,
            IBotTelemetryClient telemetryClient)
        {
            _lgMultiLangEngine = new ResourceMultiLanguageGenerator("POISharedResponses.lg");

            OnTurnError = async (context, exception) =>
            {
                CultureInfo.CurrentUICulture = new CultureInfo(context.Activity.Locale);
                await context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, context, "[PointOfInterestErrorMessage]"));
                await context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"PointOfInterest Skill Error: {exception.Message} | {exception.StackTrace}"));
                telemetryClient.TrackException(exception);
            };

            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
            Use(new SkillMiddleware(userState, conversationState, conversationState.CreateProperty<DialogState>(nameof(PointOfInterestSkill))));
        }
    }
}