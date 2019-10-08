// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Dialogs.Declarative.Types;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CalendarSkillAdaptive
{
    public class DefaultAdapter : BotFrameworkHttpAdapter
    {
        public DefaultAdapter(
            IConfiguration configuration,
            IStorage storage,
            UserState userState, 
            ConversationState convoState,
            ResourceExplorer explorer,
            ILogger<BotFrameworkHttpAdapter> logger)
            : base(configuration, logger)
        {
            TypeFactory.Configuration = configuration;
            this.UseStorage(storage);
            this.UseState(userState, convoState);
            this.Use(new RegisterClassMiddleware<ResourceExplorer>(explorer));
            this.UseAdaptiveDialogs();
            this.UseLanguageGeneration(explorer);
            this.Use(new TranscriptLoggerMiddleware(new FileTranscriptLogger()));
        }
    }
}
