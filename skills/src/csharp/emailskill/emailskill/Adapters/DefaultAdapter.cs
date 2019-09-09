using System.Globalization;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions.Contextual;
using Microsoft.Bot.Builder.Solutions.Contextual.Actions;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace EmailSkill.Adapters
{
    public class DefaultAdapter : BotFrameworkHttpAdapter
    {
        public DefaultAdapter(
            BotSettings settings,
            ICredentialProvider credentialProvider,
            IBotTelemetryClient telemetryClient,
            ResponseManager responseManager,
            ConversationState convState,
            UserState userState,
            UserContextManager userContextManager)
            : base(credentialProvider)
        {
            OnTurnError = async (context, exception) =>
            {
                CultureInfo.CurrentUICulture = new CultureInfo(context.Activity.Locale);
                await context.SendActivityAsync(responseManager.GetResponse(EmailSharedResponses.EmailErrorMessage));
                await context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Email Skill Error: {exception.Message} | {exception.StackTrace}"));
                telemetryClient.TrackException(exception);
            };

            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());

            var skillContextualMiddleware = new SkillContextualMiddleware();
            skillContextualMiddleware.Register(new GetSentimentAction());

            var savePreviousInputAction = new SavePreviousInputAction(
              convState,
              userState,
              userContextManager,
              nameof(EmailSkill));
            skillContextualMiddleware.Register(savePreviousInputAction);

            Use(skillContextualMiddleware);
        }
    }
}