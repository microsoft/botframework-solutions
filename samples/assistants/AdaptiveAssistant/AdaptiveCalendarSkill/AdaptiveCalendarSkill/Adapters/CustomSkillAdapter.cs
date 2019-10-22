// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveCalendarSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Dialogs.Declarative.Types;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Extensions.Configuration;

namespace AdaptiveCalendarSkill.Adapters
{
    public class CustomSkillAdapter : SkillWebSocketBotAdapter
    {
        public CustomSkillAdapter(
            IConfiguration configuration,
            BotSettings settings,
            IStorage storage,
            UserState userState,
            ConversationState conversationState,
            ResourceExplorer resourceExplorer,
            IBotTelemetryClient telemetryClient)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                await turnContext.SendActivityAsync($"{exception.Message}");
                await turnContext.SendActivityAsync($"{exception.StackTrace}");
                await turnContext.SendActivityAsync("Sorry, something went wrong (skill adapter)!");
            };

            TypeFactory.Configuration = configuration;
            this.UseStorage(storage);
            this.UseState(userState, conversationState);
            this.UseLanguageGeneration(resourceExplorer);
            // this.UseDebugger(configuration.GetValue("debugport", 4712), events: new Events<AdaptiveEvents>());

            // Uncomment the following line for local development without Azure Storage
            // Use(new TranscriptLoggerMiddleware(new MemoryTranscriptStore()));
            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
            Use(new SkillMiddleware(userState, conversationState, conversationState.CreateProperty<DialogState>(nameof(AdaptiveCalendarSkill))));
        }
    }
}
