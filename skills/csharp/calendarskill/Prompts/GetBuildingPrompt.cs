// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DateTime;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Prompts
{
    /// <summary>
    /// Prompt meeting start time or title to get a list of meetings.
    /// </summary>
    public class GetBuildingPrompt : Prompt<IList<RoomModel>>
    {
        internal const string AttemptCountKey = "AttemptCount";

        private BotServices Services { get; set; }

        private ISearchService SearchService { get; set; }

        private static int maxReprompt = -1;

        public GetBuildingPrompt(
            string dialogId,
            BotServices services,
            ISearchService searchService,
            PromptValidator<IList<RoomModel>> validator = null,
            string defaultLocale = null)
               : base(dialogId, validator)
        {
            DefaultLocale = defaultLocale;
            SearchService = searchService;
            Services = services;
        }

        public string DefaultLocale { get; set; }

        protected override async Task OnPromptAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, bool isRetry, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (isRetry && options.RetryPrompt != null)
            {
                await turnContext.SendActivityAsync(options.RetryPrompt, cancellationToken).ConfigureAwait(false);
            }
            else if (options.Prompt != null)
            {
                await turnContext.SendActivityAsync(options.Prompt, cancellationToken).ConfigureAwait(false);
            }

            maxReprompt = ((CalendarPromptOptions)options).MaxReprompt;
        }

        protected override async Task<PromptRecognizerResult<IList<RoomModel>>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var result = new PromptRecognizerResult<IList<RoomModel>>();

            var luisResult = turnContext.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
            if (luisResult == null)
            {
                // Get cognitive models for the current locale.
                var localizedServices = Services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("Calendar", out var skillLuisService);
                if (skillLuisService != null)
                {
                    luisResult = await skillLuisService.RecognizeAsync<CalendarLuis>(turnContext, default);
                    turnContext.TurnState[StateProperties.CalendarLuisResultKey] = luisResult;
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }
            }

            if (luisResult.TopIntent().intent == CalendarLuis.Intent.RejectCalendar && luisResult.TopIntent().score > 0.8)
            {
                result.Succeeded = true;
            }
            else
            {
                var message = turnContext.Activity.AsMessageActivity();
                List<RoomModel> meetingRooms = await SearchService.GetMeetingRoomAsync(message.Text);
                if (meetingRooms.Count > 0)
                {
                    result.Succeeded = true;
                    result.Value = meetingRooms;
                }
            }

            if (maxReprompt > 0 && Convert.ToInt32(state[AttemptCountKey]) >= maxReprompt)
            {
                result.Succeeded = true;
            }

            return await Task.FromResult(result);
        }
    }
}
