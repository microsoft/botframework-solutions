// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveCalendarSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Dialogs.Declarative.Types;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace AdaptiveCalendarSkill.Bots
{
    public class DefaultAdapter : BotFrameworkHttpAdapter
    {
        public DefaultAdapter(
            IConfiguration configuration,
            ICredentialProvider credentialProvider,
            BotSettings settings,
            IStorage storage,
            UserState userState,
            ConversationState conversationState,
            IBotTelemetryClient telemetryClient,
            ResourceExplorer resourceExplorer)
            : base(credentialProvider)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                await turnContext.SendActivityAsync($"{exception.Message}");
                await turnContext.SendActivityAsync($"{exception.StackTrace}");
                await turnContext.SendActivityAsync("Sorry, something went wrong (skill)!");
            };

            TypeFactory.Configuration = configuration;
            this.UseStorage(storage);
            this.UseState(userState, conversationState);
            this.UseLanguageGeneration(resourceExplorer);

            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
        }
    }
}