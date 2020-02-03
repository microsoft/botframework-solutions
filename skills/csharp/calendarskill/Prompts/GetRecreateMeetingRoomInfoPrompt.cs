using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Utilities;
using CalendarSkill.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using static CalendarSkill.Models.CreateEventStateModel;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Prompts
{
    public class GetRecreateMeetingRoomInfoPrompt : Prompt<RecreateMeetingRoomState?>
    {
        internal const string AttemptCountKey = "AttemptCount";

        private BotServices Services { get; set; }

        private static int maxReprompt = -1;

        public GetRecreateMeetingRoomInfoPrompt(string dialogId, BotServices services, PromptValidator<RecreateMeetingRoomState?> validator = null, string defaultLocale = null)
                : base(dialogId, validator)
        {
            DefaultLocale = defaultLocale;
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

        protected override async Task<PromptRecognizerResult<RecreateMeetingRoomState?>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var result = new PromptRecognizerResult<RecreateMeetingRoomState?>();
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
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

                var recreateState = GetStateFromMessage(turnContext);
                if (recreateState != null)
                {
                    result.Succeeded = true;
                    result.Value = recreateState;
                }
            }

            if (maxReprompt > 0 && Convert.ToInt32(state[AttemptCountKey]) >= maxReprompt)
            {
                result.Succeeded = true;
            }

            return await Task.FromResult(result);
        }

        private RecreateMeetingRoomState? GetStateFromMessage(ITurnContext turnContext)
        {
            RecreateMeetingRoomState? result = null;

            var luisResult = turnContext.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
            var intent = luisResult.TopIntent().intent;
            switch (intent)
            {
                case CalendarLuis.Intent.CancelCalendar:
                    {
                        result = RecreateMeetingRoomState.Cancel;
                        return result;
                    }

                case CalendarLuis.Intent.FindMeetingRoom:
                    {
                        result = RecreateMeetingRoomState.ChangeMeetingRoom;
                        return result;
                    }

                case CalendarLuis.Intent.CheckAvailability:
                    {
                        if (luisResult.Entities.MeetingRoom != null)
                        {
                            result = RecreateMeetingRoomState.ChangeMeetingRoom;
                            return result;
                        }

                        break;
                    }

                case CalendarLuis.Intent.ChangeCalendarEntry:
                    {
                        if (luisResult.Entities.ToDate != null || luisResult.Entities.ToTime != null || CalendarCommonUtil.ContainTimeSlot(luisResult))
                        {
                            result = RecreateMeetingRoomState.ChangeTime;
                            return result;
                        }

                        if (luisResult.Entities.MeetingRoom != null || luisResult.Entities.MeetingRoomPatternAny != null || CalendarCommonUtil.ContainMeetingRoomSlot(luisResult))
                        {
                            result = RecreateMeetingRoomState.ChangeMeetingRoom;
                            return result;
                        }

                        break;
                    }

                default:
                    {
                        break;
                    }
            }

            return result;
        }

        protected string GetSlotAttributeFromEntity(CalendarLuis._Entities entity)
        {
            return entity.SlotAttribute[0];
        }

    }
}