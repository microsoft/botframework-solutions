// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using AdaptiveAssistant.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Extensions.Configuration;
using Microsoft.Bot.Builder.Dialogs.Declarative.Types;

namespace AdaptiveAssistant.Bots
{
    public class DefaultAdapter : BotFrameworkHttpAdapter
    {
        public DefaultAdapter(
            BotSettings settings,
            IConfiguration config,
            ICredentialProvider credentialProvider,
            IBotTelemetryClient telemetryClient,
            ConversationState conversationState,
            IStorage storage,
            UserState userState,
            ResourceExplorer resourceExplorer)
            : base(credentialProvider)
        {
            var templateEngine = TemplateEngine.FromFiles("./Responses/MainResponses.lg");

            OnTurnError = async (turnContext, exception) =>
            {
                await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"{exception.Message}"));
                await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"{exception.StackTrace}"));
                await turnContext.SendActivityAsync(templateEngine.EvaluateTemplate("errorMessage", null));
                telemetryClient.TrackException(exception);
            };

            TypeFactory.Configuration = config;
            this.UseStorage(storage);
            this.UseState(userState, conversationState);
            this.Use(new RegisterClassMiddleware<ResourceExplorer>(resourceExplorer));
            this.UseLanguageGeneration(resourceExplorer, "MainResponses.lg");
            this.UseDebugger(4712, events: new Events<AdaptiveEvents>());
        }
    }
}