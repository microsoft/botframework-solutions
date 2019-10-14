// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.LanguageGeneration.Generators;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Services;

namespace ToDoSkill.Adapters
{
    public class DefaultAdapter : BotFrameworkHttpAdapter
    {
        public DefaultAdapter(
            BotSettings settings,
            ICredentialProvider credentialProvider,
            IBotTelemetryClient telemetryClient,
            ResourceExplorer resourceExplorer)
            : base(credentialProvider)
        {
            OnTurnError = async (context, exception) =>
            {
                CultureInfo.CurrentUICulture = new CultureInfo(context.Activity.Locale);
                var activity = await new ResourceMultiLanguageGenerator("ResponsesAndTexts.lg").Generate(context, $"[{ToDoSharedResponses.ToDoErrorMessage}]", null);
                await context.SendActivityAsync(activity);
                await context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"To Do Skill Error: {exception.Message} | {exception.StackTrace}"));
                telemetryClient.TrackException(exception);
            };

            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());

            this.UseResourceExplorer(resourceExplorer);
            this.UseLanguageGeneration(resourceExplorer, "ResponsesAndTexts.lg");
        }
    }
}
