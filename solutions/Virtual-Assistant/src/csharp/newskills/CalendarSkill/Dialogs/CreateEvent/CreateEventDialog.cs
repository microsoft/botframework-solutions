using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarSkill
{
    public class CreateEventDialog : CalendarSkillDialog
    {
        public CreateEventDialog(
            SkillConfiguration services,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager)
            : base(nameof(CreateEventDialog), services, accessor, serviceManager)
        {
            var createEvent = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                CollectAttendees,
                CollectTitle,
                CollectContent,
                CollectStartDate,
                CollectStartTime,
                CollectDuration,
                CollectLocation,
                ConfirmBeforeCreate,
                CreateEvent,
            };

            var updateAddress = new WaterfallStep[]
            {
                UpdateAddress,
                AfterUpdateAddress,
            };

            var confirmAttendee = new WaterfallStep[]
            {
                ConfirmAttendee,
                AfterConfirmAttendee,
            };

            var updateName = new WaterfallStep[]
            {
                UpdateUserName,
                AfterUpdateUserName,
            };

            var updateStartDate = new WaterfallStep[]
            {
                UpdateStartDateForCreate,
                AfterUpdateStartDateForCreate,
            };

            var updateStartTime = new WaterfallStep[]
            {
                UpdateStartTimeForCreate,
                AfterUpdateStartTimeForCreate,
            };

            var updateDuration = new WaterfallStep[]
            {
                UpdateDurationForCreate,
                AfterUpdateDurationForCreate,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.CreateEvent, createEvent));
            AddDialog(new WaterfallDialog(Action.UpdateAddress, updateAddress));
            AddDialog(new WaterfallDialog(Action.ConfirmAttendee, confirmAttendee));
            AddDialog(new WaterfallDialog(Action.UpdateName, updateName));
            AddDialog(new WaterfallDialog(Action.UpdateStartDateForCreate, updateStartDate));
            AddDialog(new WaterfallDialog(Action.UpdateStartTimeForCreate, updateStartTime));
            AddDialog(new WaterfallDialog(Action.UpdateDurationForCreate, updateDuration));

            // Set starting dialog for component
            InitialDialogId = Action.CreateEvent;
        }

        // Create Event waterfall steps
        public async Task<DialogTurnResult> CollectTitle(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);

                if (string.IsNullOrEmpty(state.Title))
                {
                    var userNameString = string.Empty;
                    foreach (var attendee in state.Attendees)
                    {
                        if (userNameString != string.Empty)
                        {
                            userNameString += ", ";
                        }

                        userNameString += attendee.DisplayName ?? attendee.Address;
                    }

                    return await sc.PromptAsync(Action.Prompt, new PromptOptions {
                            Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoTitle, _responseBuilder, new StringDictionary() { { "UserName", userNameString } })
                        });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        public async Task<DialogTurnResult> CollectContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    if (string.IsNullOrEmpty(state.Title))
                    {
                        sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                        state.Title = content != null ? content.ToString() : sc.Context.Activity.Text;
                    }
                }

                if (string.IsNullOrEmpty(state.Content))
                {
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoContent),
                    });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        public async Task<DialogTurnResult> CollectAttendees(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.APIToken))
                {
                    return await sc.EndDialogAsync(true);
                }

                var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource, state.GetUserTimeZone());
                if (state.Attendees.Count == 0)
                {
                    return await sc.BeginDialogAsync(Action.UpdateAddress);
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        public async Task<DialogTurnResult> CollectStartDate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    if (string.IsNullOrEmpty(state.Content))
                    {
                        sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                        state.Content = content != null ? content.ToString() : sc.Context.Activity.Text;
                    }
                }

                if (state.StartDate == null)
                {
                    return await sc.BeginDialogAsync(Action.UpdateStartDateForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        public async Task<DialogTurnResult> CollectStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (state.StartTime == null)
                {
                    return await sc.BeginDialogAsync(Action.UpdateStartTimeForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        public async Task<DialogTurnResult> CollectDuration(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);

                if (state.EndDateTime == null)
                {
                    return await sc.BeginDialogAsync(Action.UpdateDurationForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        public async Task<DialogTurnResult> CollectLocation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);

                if (state.Location == null)
                {
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoLocation) });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        public async Task<DialogTurnResult> ConfirmBeforeCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var luisResult = await _services.LuisServices["calendar"].RecognizeAsync<Calendar>(sc.Context, cancellationToken);
                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                    var topIntent = luisResult?.TopIntent().intent;
                    if (topIntent == Calendar.Intent.Reject || topIntent == Calendar.Intent.ConfirmNo || topIntent == Luis.Calendar.Intent.NoLocation)
                    {
                        state.Location = string.Empty;
                    }
                    else
                    {
                        state.Location = userInput;
                    }
                }

                var source = state.EventSource;
                var newEvent = new EventModel(source)
                {
                    Title = state.Title,
                    Content = state.Content,
                    Attendees = state.Attendees,
                    StartTime = (DateTime)state.StartDateTime,
                    EndTime = (DateTime)state.EndDateTime,
                    TimeZone = state.GetUserTimeZone(),
                    Location = state.Location,
                };

                var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(CalendarBotResponses.ConfirmCreate, newEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", newEvent.ToAdaptiveCardData());

                return await sc.PromptAsync(Action.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = sc.Context.Activity.CreateReply(CalendarBotResponses.ConfirmCreateFailed, _responseBuilder),
                });
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        public async Task<DialogTurnResult> CreateEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var source = state.EventSource;
                    var newEvent = new EventModel(source)
                    {
                        Title = state.Title,
                        Content = state.Content,
                        Attendees = state.Attendees,
                        StartTime = (DateTime)state.StartDateTime,
                        EndTime = (DateTime)state.EndDateTime,
                        TimeZone = state.GetUserTimeZone(),
                        Location = state.Location,
                    };
                    var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource, state.GetUserTimeZone());
                    if (await calendarService.CreateEvent(newEvent) != null)
                    {
                        var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(CalendarBotResponses.EventCreated, newEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", newEvent.ToAdaptiveCardData());
                        await sc.Context.SendActivityAsync(replyMessage);
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.EventCreationFailed));
                    }

                    state.Clear();
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.ActionEnded));
                    state.Clear();
                }

                return await sc.EndDialogAsync(true);
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        // update address waterfall steps
        public async Task<DialogTurnResult> UpdateAddress(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (state.EventSource == EventSource.Microsoft)
                {
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoAttendeesMS),
                    });
                }
                else
                {
                    if (sc.Result != null)
                    {
                        return await sc.PromptAsync(Action.Prompt, new PromptOptions
                        {
                            Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.WrongAddress),
                        });
                    }
                    else
                    {
                        return await sc.PromptAsync(Action.Prompt, new PromptOptions
                        {
                            Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoAttendees),
                        });
                    }
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        public async Task<DialogTurnResult> AfterUpdateAddress(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                    // TODO: can we do this somewhere else
                    if (IsEmail(userInput))
                    {
                        state.Attendees.Add(new EventModel.Attendee { Address = userInput });
                        return await sc.EndDialogAsync(true);
                    }
                    else
                    {
                        if (state.EventSource == EventSource.Microsoft)
                        {
                            if (userInput != null)
                            {
                                var nameList = userInput.Split(new string[] { ",", "and", ";" }, StringSplitOptions.None)
                                    .Select(x => x.Trim())
                                    .Where(x => !string.IsNullOrWhiteSpace(x))
                                    .ToList();
                                state.AttendeesNameList = nameList;
                            }

                            return await sc.BeginDialogAsync(Action.ConfirmAttendee);
                        }
                        else
                        {
                            return await sc.BeginDialogAsync(Action.UpdateAddress, new UpdateAddressDialogOptions(UpdateAddressDialogOptions.UpdateReason.NotAnAddress));
                        }
                    }
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        // confirm attendee waterfall steps
        public async Task<DialogTurnResult> ConfirmAttendee(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                var currentRecipientName = state.AttendeesNameList[state.ConfirmAttendeesNameIndex];
                var personList = await GetPeopleWorkWithAsync(sc, currentRecipientName);
                var userList = await GetUserAsync(sc, currentRecipientName);

                // todo: should set updatename reason in sc.Result
                if (personList.Count > 10)
                {
                    return await sc.BeginDialogAsync(Action.UpdateName, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.TooMany));
                }

                if (personList.Count < 1 && userList.Count < 1)
                {
                    return await sc.BeginDialogAsync(Action.UpdateName, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.NotFound));
                }

                if (personList.Count == 1)
                {
                    var user = personList.FirstOrDefault();
                    if (user != null)
                    {
                        var result =
                            new FoundChoice()
                            {
                                Value = $"{user.DisplayName}: {user.ScoredEmailAddresses.FirstOrDefault()?.Address ?? user.UserPrincipalName}",
                            };

                        return await sc.NextAsync(result);
                    }
                }

                // TODO: should be simplify
                var selectOption = await GenerateOptions(personList, userList, sc);

                // If no more recipient to show, start update name flow and reset the recipient paging index.
                if (selectOption.Choices.Count == 0)
                {
                    state.ShowAttendeesIndex = 0;
                    return await sc.BeginDialogAsync(Action.UpdateName, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.TooMany));
                }

                // Update prompt string to include the choices because the list style is none;
                // TODO: should be removed if use adaptive card show choices.
                var choiceString = GetSelectPromptString(selectOption, true);
                selectOption.Prompt.Text = choiceString;
                return await sc.PromptAsync(Action.Choice, selectOption);
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        public async Task<DialogTurnResult> AfterConfirmAttendee(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);

                // result is null when just update the recipient name. show recipients page should be reset.
                if (sc.Result == null)
                {
                    state.ShowAttendeesIndex = 0;
                    return await sc.BeginDialogAsync(Action.ConfirmAttendee);
                }
                else if (sc.Result.ToString() == Luis.Calendar.Intent.ShowNext.ToString())
                {
                    state.ShowAttendeesIndex++;
                    return await sc.BeginDialogAsync(Action.ConfirmAttendee);
                }
                else if (sc.Result.ToString() == Luis.Calendar.Intent.ShowPrevious.ToString())
                {
                    if (state.ShowAttendeesIndex > 0)
                    {
                        state.ShowAttendeesIndex--;
                    }

                    return await sc.BeginDialogAsync(Action.ConfirmAttendee);
                }
                else
                {
                    var user = (sc.Result as FoundChoice)?.Value.Trim('*');
                    if (user != null)
                    {
                        var attendee = new EventModel.Attendee
                        {
                            DisplayName = user.Split(": ")[0],
                            Address = user.Split(": ")[1],
                        };
                        if (state.Attendees.All(r => r.Address != attendee.Address))
                        {
                            state.Attendees.Add(attendee);
                        }
                    }

                    state.ConfirmAttendeesNameIndex++;
                    if (state.ConfirmAttendeesNameIndex < state.AttendeesNameList.Count)
                    {
                        return await sc.BeginDialogAsync(Action.ConfirmAttendee);
                    }
                    else
                    {
                        return await sc.EndDialogAsync(true);
                    }
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        // update name waterfall steps
        public async Task<DialogTurnResult> UpdateUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                var currentRecipientName = state.AttendeesNameList[state.ConfirmAttendeesNameIndex];

                if (((UpdateUserNameDialogOptions)sc.Options).Reason == UpdateUserNameDialogOptions.UpdateReason.TooMany)
                {
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.PromptTooManyPeople, _responseBuilder, new StringDictionary() { { "UserName", currentRecipientName } }),
                    });
                }
                else
                {
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.PromptPersonNotFound, _responseBuilder),
                    });
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        public async Task<DialogTurnResult> AfterUpdateUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                if (!string.IsNullOrEmpty(userInput))
                {
                    var state = await _accessor.GetAsync(sc.Context);
                    state.AttendeesNameList[state.ConfirmAttendeesNameIndex] = userInput;
                }

                return await sc.EndDialogAsync(true);
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        // update start date waterfall steps
        public async Task<DialogTurnResult> UpdateStartDateForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (((UpdateDateTimeDialogOptions)sc.Options).Reason == UpdateDateTimeDialogOptions.UpdateReason.NotFound)
                {
                    return await sc.PromptAsync(Action.DateTimePrompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoStartDate),
                    });
                }
                else
                {
                    return await sc.PromptAsync(Action.DateTimePrompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.DidntUnderstandMessage),
                    });
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        public async Task<DialogTurnResult> AfterUpdateStartDateForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    var dateTime = DateTime.Parse(dateTimeResolutions.First().Value);
                    var dateTimeConvertType = dateTimeResolutions.First().Timex;

                    if (dateTime != null)
                    {
                        bool isRelativeTime = IsRelativeTime(sc.Context.Activity.Text, dateTimeResolutions.First().Value, dateTimeResolutions.First().Timex);

                        // Workaround as DateTimePrompt only return as local time
                        if (isRelativeTime)
                        {
                            dateTime = new DateTime(
                            dateTime.Year,
                            dateTime.Month,
                            dateTime.Day,
                            DateTime.Now.Hour,
                            DateTime.Now.Minute,
                            DateTime.Now.Second);
                        }

                        state.StartDate = isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTime;
                        return await sc.EndDialogAsync();
                    }
                }

                return await sc.BeginDialogAsync(Action.UpdateStartDateForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime));
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        // update start time waterfall steps
        public async Task<DialogTurnResult> UpdateStartTimeForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (((UpdateDateTimeDialogOptions)sc.Options).Reason == UpdateDateTimeDialogOptions.UpdateReason.NotFound)
                {
                    return await sc.PromptAsync(Action.DateTimePrompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoStartTime),
                    });
                }
                else
                {
                    return await sc.PromptAsync(Action.DateTimePrompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.DidntUnderstandMessage),
                    });
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        public async Task<DialogTurnResult> AfterUpdateStartTimeForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    var dateTime = DateTime.Parse(dateTimeResolutions.First().Value);
                    var dateTimeConvertType = dateTimeResolutions.First().Timex;

                    if (dateTime != null)
                    {
                        bool isRelativeTime = IsRelativeTime(sc.Context.Activity.Text, dateTimeResolutions.First().Value, dateTimeResolutions.First().Timex);
                        state.StartTime = isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTime;
                        state.StartDateTime = new DateTime(
                            state.StartDate.Value.Year,
                            state.StartDate.Value.Month,
                            state.StartDate.Value.Day,
                            state.StartTime.Value.Hour,
                            state.StartTime.Value.Minute,
                            state.StartTime.Value.Second);
                        return await sc.EndDialogAsync();
                    }
                }

                return await sc.BeginDialogAsync(Action.UpdateStartTimeForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime));
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        // update duration waterfall steps
        public async Task<DialogTurnResult> UpdateDurationForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (((UpdateDateTimeDialogOptions)sc.Options).Reason == UpdateDateTimeDialogOptions.UpdateReason.NotFound)
                {
                    return await sc.PromptAsync(Action.DateTimePrompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.NoDuration),
                    });
                }
                else
                {
                    return await sc.PromptAsync(Action.DateTimePrompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarBotResponses.DidntUnderstandMessage),
                    });
                }
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        public async Task<DialogTurnResult> AfterUpdateDurationForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);

                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    int.TryParse(dateTimeResolutions.First().Value, out int duration);

                    if (duration > 0)
                    {
                        state.EndDateTime = state.StartDateTime.Value.AddSeconds(duration);
                        return await sc.EndDialogAsync();
                    }
                    else
                    {
                        // TODO: Handle improper duration
                    }
                }

                return await sc.BeginDialogAsync(Action.UpdateDurationForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime));
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }
    }
}
