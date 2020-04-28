// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using VirtualAssistantSample.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Solutions.Middleware;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VirtualAssistantSample.Adapters
{
    public class DefaultAdapter : BotFrameworkHttpAdapter
    {
        private readonly BotSettings _settings;
        private readonly ILogger _logger;
        private readonly IStorage _storage;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly LocaleTemplateManager _templateEngine;
        private readonly IBotTelemetryClient _telemetryClient;

        public DefaultAdapter(
            IConfiguration configuration, 
            BotSettings settings,
            ILogger<BotFrameworkHttpAdapter> logger, 
            IStorage storage, 
            UserState userState, 
            ConversationState conversationState,
            LocaleTemplateManager templateEngine,
            IBotTelemetryClient telemetryClient)
            : base(configuration, logger: logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));

            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            _templateEngine = templateEngine ?? throw new ArgumentNullException(nameof(templateEngine));
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));

            OnTurnError = HandleTurnErrorAsync;

            this.UseStorage(_storage);
            this.UseState(_userState, _conversationState);

            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(_settings.BlobStorage.ConnectionString, _settings.BlobStorage.Container)));
            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(_settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
            Use(new SetSpeakMiddleware());
        }

        private async Task HandleTurnErrorAsync(ITurnContext turnContext, Exception exception)
        {
            // Log any leaked exception from the application.
            _logger.LogError(exception, $"[OnTurnError] unhandled error : {exception.Message}");

            await SendErrorMessageAsync(turnContext, exception);
            // TODO: Enable Skills
            // await EndSkillConversationAsync(turnContext);
            await ClearConversationStateAsync(turnContext);
        }

        private async Task SendErrorMessageAsync(ITurnContext turnContext, Exception exception)
        {
            try
            {
                _telemetryClient.TrackException(exception);

                // Send a message to the user.
                await turnContext.SendActivityAsync(_templateEngine.GenerateActivityForLocale("ErrorMessage"));

                // Send a trace activity, which will be displayed in the Bot Framework Emulator.
                // Note: we return the entire exception in the value property to help the developer;
                // this should not be done in production.
                await turnContext.TraceActivityAsync("OnTurnError Trace", exception.ToString(), "https://www.botframework.com/schemas/error", "TurnError");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught in SendErrorMessageAsync : {ex}");
            }
        }

        // TODO: Enable Skills
        //private async Task EndSkillConversationAsync(ITurnContext turnContext)
        //{
        //    if (_skillClient == null || _skillsConfig == null)
        //    {
        //        return;
        //    }

        //    try
        //    {
        //        // Inform the active skill that the conversation is ended so that it has a chance to clean up.
        //        // Note: the root bot manages the ActiveSkillPropertyName, which has a value while the root bot
        //        // has an active conversation with a skill.
        //        // TODO: Determine where to handle Active Skill name
        //        var activeSkill = await _conversationState.CreateProperty<BotFrameworkSkill>(DispatchDialog.ActiveSkillPropertyName).GetAsync(turnContext, () => null);
        //        if (activeSkill != null)
        //        {
        //            var endOfConversation = Activity.CreateEndOfConversationActivity();
        //            endOfConversation.Code = "RootSkillError";
        //            endOfConversation.ApplyConversationReference(turnContext.Activity.GetConversationReference(), true);

        //            await _conversationState.SaveChangesAsync(turnContext, true);
        //            await _skillClient.PostActivityAsync(_settings.MicrosoftAppId, activeSkill, _skillsConfig.SkillHostEndpoint, (Activity)endOfConversation, CancellationToken.None);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Exception caught on attempting to send EndOfConversation : {ex}");
        //    }
        //}

        private async Task ClearConversationStateAsync(ITurnContext turnContext)
        {
            try
            {
                // Delete the conversationState for the current conversation to prevent the
                // bot from getting stuck in a error-loop caused by being in a bad state.
                // ConversationState should be thought of as similar to "cookie-state" for a Web page.
                await _conversationState.DeleteAsync(turnContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught on attempting to Delete ConversationState : {ex}");
            }
        }
    }
}
