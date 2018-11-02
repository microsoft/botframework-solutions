using CalendarSkill.Common;
using CalendarSkill.Dialogs.CreateEvent.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Graph;
using Microsoft.Recognizers.Text.Choice;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
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
            AddDialog(new WaterfallDialog(Actions.CreateEvent, createEvent));
            AddDialog(new WaterfallDialog(Actions.UpdateAddress, updateAddress));
            AddDialog(new WaterfallDialog(Actions.ConfirmAttendee, confirmAttendee));
            AddDialog(new WaterfallDialog(Actions.UpdateName, updateName));
            AddDialog(new WaterfallDialog(Actions.UpdateStartDateForCreate, updateStartDate));
            AddDialog(new WaterfallDialog(Actions.UpdateStartTimeForCreate, updateStartTime));
            AddDialog(new WaterfallDialog(Actions.UpdateDurationForCreate, updateDuration));

            // Set starting dialog for component
            InitialDialogId = Actions.CreateEvent;
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

                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoTitle, _responseBuilder, new StringDictionary() { { "UserName", userNameString } })
                    });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
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
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoContent),
                    });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
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

                var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource);
                if (state.Attendees.Count == 0)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateAddress);
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
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
                    return await sc.BeginDialogAsync(Actions.UpdateStartDateForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> CollectStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.UpdateStartTimeForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> CollectDuration(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);

                if (state.EndDateTime == null)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateDurationForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound));
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> CollectLocation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);

                if (state.Location == null)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoLocation) });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> ConfirmBeforeCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (state.Location == null && sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var luisResult = state.LuisResult;

                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                    var topIntent = luisResult?.TopIntent().intent.ToString();

                    var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);

                    // Enable the user to skip providing the location if they say something matching the Cancel intent, say something matching the ConfirmNo recognizer or something matching the NoLocation intent

                    if (topIntent == Luis.General.Intent.Cancel.ToString() || (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false) || topIntent == Luis.Calendar.Intent.NoLocation.ToString())
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
                    StartTime = state.StartDateTime.Value,
                    EndTime = state.EndDateTime.Value,
                    TimeZone = TimeZoneInfo.Utc,
                    Location = state.Location,
                };

                var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(CreateEventResponses.ConfirmCreate, newEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", newEvent.ToAdaptiveCardData(state.GetUserTimeZone()));

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = sc.Context.Activity.CreateReply(CreateEventResponses.ConfirmCreateFailed, _responseBuilder),
                });
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
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
                        TimeZone = TimeZoneInfo.Utc,
                        Location = state.Location,
                    };
                    var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource);
                    if (await calendarService.CreateEvent(newEvent) != null)
                    {
                        var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(CreateEventResponses.EventCreated, newEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", newEvent.ToAdaptiveCardData(state.GetUserTimeZone()));
                        await sc.Context.SendActivityAsync(replyMessage);
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CreateEventResponses.EventCreationFailed));
                    }

                    state.Clear();
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarSharedResponses.ActionEnded));
                    state.Clear();
                }

                return await sc.EndDialogAsync(true);
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        // update address waterfall steps
        public async Task<DialogTurnResult> UpdateAddress(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (state.AttendeesNameList.Count() > 0)
                {
                    return await sc.NextAsync();
                }

                if (state.EventSource == EventSource.Microsoft)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoAttendeesMS),
                    });
                }
                else
                {
                    if (sc.Result != null)
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions
                        {
                            Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.WrongAddress),
                        });
                    }
                    else
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions
                        {
                            Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoAttendees),
                        });
                    }
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> AfterUpdateAddress(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (state.AttendeesNameList.Count() > 0)
                {
                    return await sc.BeginDialogAsync(Actions.ConfirmAttendee);
                }

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

                            return await sc.BeginDialogAsync(Actions.ConfirmAttendee);
                        }
                        else
                        {
                            return await sc.BeginDialogAsync(Actions.UpdateAddress, new UpdateAddressDialogOptions(UpdateAddressDialogOptions.UpdateReason.NotAnAddress));
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
                await HandleDialogExceptions(sc);
                throw;
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
                    return await sc.BeginDialogAsync(Actions.UpdateName, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.TooMany));
                }

                if (personList.Count < 1 && userList.Count < 1)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateName, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.NotFound));
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
                    return await sc.BeginDialogAsync(Actions.UpdateName, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.NotFound));
                }

                // Update prompt string to include the choices because the list style is none;
                // TODO: should be removed if use adaptive card show choices.
                var choiceString = GetSelectPromptString(selectOption, true);
                selectOption.Prompt.Text = choiceString;
                return await sc.PromptAsync(Actions.Choice, selectOption);
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
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
                    return await sc.BeginDialogAsync(Actions.ConfirmAttendee);
                }
                else if (sc.Result.ToString() == Luis.General.Intent.Next.ToString())
                {
                    state.ShowAttendeesIndex++;
                    return await sc.BeginDialogAsync(Actions.ConfirmAttendee);
                }
                else if (sc.Result.ToString() == Luis.General.Intent.Previous.ToString())
                {
                    if (state.ShowAttendeesIndex > 0)
                    {
                        state.ShowAttendeesIndex--;
                    }

                    return await sc.BeginDialogAsync(Actions.ConfirmAttendee);
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
                        return await sc.BeginDialogAsync(Actions.ConfirmAttendee);
                    }
                    else
                    {
                        return await sc.EndDialogAsync(true);
                    }
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
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
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.PromptTooManyPeople, _responseBuilder, new StringDictionary() { { "UserName", currentRecipientName } }),
                    });
                }
                else
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.PromptPersonNotFound, _responseBuilder),
                    });
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
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

                return await sc.EndDialogAsync();
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        // update start date waterfall steps
        public async Task<DialogTurnResult> UpdateStartDateForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (((UpdateDateTimeDialogOptions)sc.Options).Reason == UpdateDateTimeDialogOptions.UpdateReason.NotFound)
                {
                    return await sc.PromptAsync(Actions.DateTimePrompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoStartDate),
                    });
                }
                else
                {
                    return await sc.PromptAsync(Actions.DateTimePrompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarSharedResponses.DidntUnderstandMessage),
                    });
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
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
                    if (dateTimeResolutions.First().Value != null)
                    {
                        var dateTime = DateTime.Parse(dateTimeResolutions.First().Value);
                        var dateTimeConvertType = dateTimeResolutions.First().Timex;

                        if (dateTime != null)
                        {
                            bool isRelativeTime = IsRelativeTime(sc.Context.Activity.Text, dateTimeResolutions.First().Value, dateTimeResolutions.First().Timex);
                            if (ContainsTime(dateTimeConvertType))
                            {
                                state.StartTime = TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone());
                            }

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
                }

                return await sc.BeginDialogAsync(Actions.UpdateStartDateForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime));
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        // update start time waterfall steps
        public async Task<DialogTurnResult> UpdateStartTimeForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (state.StartTime == null)
                {
                    if (((UpdateDateTimeDialogOptions)sc.Options).Reason == UpdateDateTimeDialogOptions.UpdateReason.NotFound)
                    {
                        return await sc.PromptAsync(Actions.DateTimePrompt, new PromptOptions
                        {
                            Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoStartTime),
                        });
                    }
                    else
                    {
                        return await sc.PromptAsync(Actions.DateTimePrompt, new PromptOptions
                        {
                            Prompt = sc.Context.Activity.CreateReply(CalendarSharedResponses.DidntUnderstandMessage),
                        });
                    }
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> AfterUpdateStartTimeForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (sc.Result != null && state.StartTime == null)
                {
                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    if (dateTimeResolutions.First().Value != null)
                    {
                        var dateTime = DateTime.Parse(dateTimeResolutions.First().Value);
                        var dateTimeConvertType = dateTimeResolutions.First().Timex;

                        if (dateTime != null)
                        {
                            bool isRelativeTime = IsRelativeTime(sc.Context.Activity.Text, dateTimeResolutions.First().Value, dateTimeResolutions.First().Timex);
                            state.StartTime = isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTime;
                        }
                    }
                }

                if (state.StartTime != null)
                {
                    state.StartDateTime = new DateTime(
                        state.StartDate.Value.Year,
                        state.StartDate.Value.Month,
                        state.StartDate.Value.Day,
                        state.StartTime.Value.Hour,
                        state.StartTime.Value.Minute,
                        state.StartTime.Value.Second);
                    state.StartDateTime = TimeZoneInfo.ConvertTimeToUtc(state.StartDateTime.Value, state.GetUserTimeZone());
                    return await sc.EndDialogAsync();
                }

                return await sc.BeginDialogAsync(Actions.UpdateStartTimeForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime));
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        // update duration waterfall steps
        public async Task<DialogTurnResult> UpdateDurationForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (state.Duration > 0 || state.EndTime != null || state.EndDate != null)
                {
                    return await sc.NextAsync();
                }
                else if (((UpdateDateTimeDialogOptions)sc.Options).Reason == UpdateDateTimeDialogOptions.UpdateReason.NotFound)
                {
                    return await sc.PromptAsync(Actions.DateTimePrompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CreateEventResponses.NoDuration),
                    });
                }
                else
                {
                    return await sc.PromptAsync(Actions.DateTimePrompt, new PromptOptions
                    {
                        Prompt = sc.Context.Activity.CreateReply(CalendarSharedResponses.DidntUnderstandMessage),
                    });
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> AfterUpdateDurationForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (state.EndDate != null || state.EndTime != null)
                {
                    DateTime? startDate = state.StartDate == null ? TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone()) : state.StartDate;
                    DateTime? endDate = state.EndDate;
                    DateTime? endTime = state.EndTime;
                    state.EndDateTime = endDate == null ?
                        new DateTime(startDate.Value.Year,
                            startDate.Value.Month,
                            startDate.Value.Day,
                            endTime.Value.Hour,
                            endTime.Value.Minute,
                            endTime.Value.Second) :
                        new DateTime(endDate.Value.Year,
                            endDate.Value.Month,
                            endDate.Value.Day, 23, 59, 59);
                    state.EndDateTime = TimeZoneInfo.ConvertTimeToUtc(state.EndDateTime.Value, state.GetUserTimeZone());
                    TimeSpan ts = state.StartDateTime.Value.Subtract(state.EndDateTime.Value).Duration();
                    state.Duration = (int)ts.TotalSeconds;
                }

                if (state.Duration <= 0 && sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);

                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    if (dateTimeResolutions.First().Value != null)
                    {
                        int.TryParse(dateTimeResolutions.First().Value, out int duration);
                        state.Duration = duration;
                    }
                }

                if (state.Duration > 0)
                {
                    state.EndDateTime = state.StartDateTime.Value.AddSeconds(state.Duration);
                    return await sc.EndDialogAsync();
                }
                else
                {
                    // TODO: Handle improper duration
                }

                return await sc.BeginDialogAsync(Actions.UpdateDurationForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime));
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<List<Person>> GetUserAsync(DialogContext dc, string name)
        {
            var result = new List<Person>();
            var state = await _accessor.GetAsync(dc.Context);
            try
            {
                var token = state.APIToken;
                var service = _serviceManager.InitUserService(token, state.GetUserTimeZone());

                // Get users.
                result = await service.GetPeopleAsync(name);
            }
            catch (ServiceException)
            {
                await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(CreateEventResponses.FindUserErrorMessage, _responseBuilder, new StringDictionary() { { "UserName", name } }));
                state.Clear();
                await dc.EndDialogAsync(true);
            }

            return result;
        }

        public async Task<List<Person>> GetPeopleWorkWithAsync(DialogContext dc, string name)
        {
            var result = new List<Person>();
            try
            {
                var state = await _accessor.GetAsync(dc.Context);
                var token = state.APIToken;
                var service = _serviceManager.InitUserService(token, state.GetUserTimeZone());

                // Get users.
                result = await service.GetPeopleAsync(name);
            }
            catch (ServiceException)
            {
                await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(CreateEventResponses.FindUserErrorMessage, _responseBuilder, new StringDictionary() { { "UserName", name } }));

                var state = await _accessor.GetAsync(dc.Context);
                state.Clear();
                await _accessor.SetAsync(dc.Context, state);
                await dc.EndDialogAsync(true); // todo: should be sc.EndAll();
            }

            return result;
        }

        public async Task<PromptOptions> GenerateOptions(List<Person> personList, List<Person> userList, DialogContext dc)
        {
            var state = await _accessor.GetAsync(dc.Context);
            var pageIndex = state.ShowAttendeesIndex;
            var pageSize = 5;
            var skip = pageSize * pageIndex;
            var options = new PromptOptions
            {
                Choices = new List<Choice>(),
                Prompt = dc.Context.Activity.CreateReply(CreateEventResponses.ConfirmRecipient),
            };
            for (var i = 0; i < personList.Count; i++)
            {
                var user = personList[i];
                var mailAddress = user.ScoredEmailAddresses.FirstOrDefault()?.Address ?? user.UserPrincipalName;

                var choice = new Choice()
                {
                    Value = $"**{user.DisplayName}: {mailAddress}**",
                    Synonyms = new List<string> { (i + 1).ToString(), user.DisplayName, user.DisplayName.ToLower(), mailAddress },
                };

                var userName = user.UserPrincipalName?.Split("@").FirstOrDefault() ?? user.UserPrincipalName;
                if (!string.IsNullOrEmpty(userName))
                {
                    {
                        choice.Synonyms.Add(userName);
                        choice.Synonyms.Add(userName.ToLower());
                    }

                    if (skip <= 0)
                    {
                        if (options.Choices.Count >= pageSize)
                        {
                            return options;
                        }

                        options.Choices.Add(choice);
                    }
                    else
                    {
                        skip--;
                    }
                }
            }

            if (options.Choices.Count == 0)
            {
                pageSize = 10;
            }

            for (var i = 0; i < userList.Count; i++)
            {
                var user = userList[i];
                var mailAddress = user.ScoredEmailAddresses.FirstOrDefault()?.Address ?? user.UserPrincipalName;
                var choice = new Choice()
                {
                    Value = $"{user.DisplayName}: {mailAddress}",
                    Synonyms = new List<string> { (i + 1).ToString(), user.DisplayName, user.DisplayName.ToLower(), mailAddress },
                };

                var userName = user.UserPrincipalName?.Split("@").FirstOrDefault() ?? user.UserPrincipalName;
                if (!string.IsNullOrEmpty(userName))
                {
                    choice.Synonyms.Add(userName);
                    choice.Synonyms.Add(userName.ToLower());
                }

                if (skip <= 0)
                {
                    if (options.Choices.Count >= pageSize)
                    {
                        return options;
                    }

                    options.Choices.Add(choice);
                }
                else if (skip >= 10)
                {
                    return options;
                }
                else
                {
                    skip--;
                }
            }

            return options;
        }

        public string GetSelectPromptString(PromptOptions selectOption, bool containNumbers)
        {
            var result = string.Empty;
            result += selectOption.Prompt.Text + "\r\n";
            for (var i = 0; i < selectOption.Choices.Count; i++)
            {
                var choice = selectOption.Choices[i];
                result += "  ";
                if (containNumbers)
                {
                    result += i + 1 + "-";
                }

                result += choice.Value + "\r\n";
            }

            return result;
        }

        public bool IsEmail(string emailString)
        {
            return Regex.IsMatch(emailString, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
        }
    }
}
