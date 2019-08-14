// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveAssistant.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Dialogs.Declarative.Types;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;

namespace AdaptiveAssistant.Adapters
{
    public class DefaultAdapter : BotFrameworkHttpAdapter
    {
        public DefaultAdapter(
            IConfiguration configuration,
            BotSettings settings,
            IStorage storage,
            UserState userState,
            ConversationState conversationState,
            ResourceExplorer resourceExplorer,
            ICredentialProvider credentialProvider)
            : base(credentialProvider)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                await turnContext.SendActivityAsync($"{exception.Message}");
                await turnContext.SendActivityAsync($"{exception.StackTrace}");
                await turnContext.SendActivityAsync("Sorry, something went wrong!");
            };

            TypeFactory.Configuration = configuration;
            this.UseStorage(storage);
            this.UseState(userState, conversationState);
            this.UseLanguageGeneration(resourceExplorer);
            this.UseDebugger(configuration.GetValue("debugport", 4712), events: new Events<AdaptiveEvents>());

            // Use(new TranscriptLoggerMiddleware(new MemoryTranscriptStore()));           
            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
        }
    }
}