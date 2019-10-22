// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveAssistant.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Dialogs.Declarative.Types;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.StreamingExtensions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;

namespace AdaptiveAssistant.Adapters
{
    public class DefaultWebSocketAdapter : WebSocketEnabledHttpAdapter
    {
        public DefaultWebSocketAdapter(
            IConfiguration configuration,
            BotSettings settings,
            IStorage storage,
            UserState userState,
            ConversationState conversationState,
            ResourceExplorer resourceExplorer,
            ICredentialProvider credentialProvider)
            : base(configuration, credentialProvider)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                await turnContext.SendActivityAsync($"{exception.Message}");
                await turnContext.SendActivityAsync($"{exception.StackTrace}");
                await turnContext.SendActivityAsync("Sorry, something went wrong (websocket)!");
            };

            TypeFactory.Configuration = configuration;
            this.UseStorage(storage);
            this.UseState(userState, conversationState);
            this.UseLanguageGeneration(resourceExplorer);
            this.UseDebugger(configuration.GetValue("debugport", 4712), events: new Events<AdaptiveEvents>());

            // Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
            Use(new SetSpeakMiddleware(settings.DefaultLocale ?? "en-us"));
        }
    }
}   