using System.Globalization;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Utilities;

namespace PointOfInterestSkill.Adapters
{
    public class DefaultAdapter : BotFrameworkHttpAdapter
    {
        private ResourceMultiLanguageGenerator _lgMultiLangEngine;

        public DefaultAdapter(
            BotSettings settings,
            ICredentialProvider credentialProvider,
            IBotTelemetryClient telemetryClient)
            : base(credentialProvider)
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
            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
        }
    }
}