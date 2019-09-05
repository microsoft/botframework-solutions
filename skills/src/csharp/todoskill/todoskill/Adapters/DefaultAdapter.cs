// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions.Contextual;
using Microsoft.Bot.Builder.Solutions.Contextual.Actions;
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
            ResponseManager responseManager,
            ConversationState convState,
            UserState userState,
            UserContextManager userContextResolver)
            : base(credentialProvider)
        {
            OnTurnError = async (context, exception) =>
            {
                CultureInfo.CurrentUICulture = new CultureInfo(context.Activity.Locale);
                await context.SendActivityAsync(responseManager.GetResponse(ToDoSharedResponses.ToDoErrorMessage));
                await context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"To Do Skill Error: {exception.Message} | {exception.StackTrace}"));
                telemetryClient.TrackException(exception);
            };

            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());

            var savePreviousQuestion = new SavePreviousInput(
                convState,
                userState,
                userContextResolver,
                nameof(ToDoSkill),
                new List<string> { "ShowToDo", "MarkToDo" });

            var skillContextualMiddleware = new SkillContextualMiddleware();
            skillContextualMiddleware.Register(savePreviousQuestion);
            Use(skillContextualMiddleware);
        }
    }
}
