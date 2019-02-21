using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.CreateEvent.Resources;
using CalendarSkill.Dialogs.FindContact.Resources;
using CalendarSkill.Dialogs.Shared;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using CalendarSkill.Util;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Data;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Graph;
using static CalendarSkill.CalendarSkillState;

namespace CalendarSkill.Dialogs.FindContact
{
    public class FindContactDialog : CalendarSkillDialog
    {
        public FindContactDialog(
           SkillConfigurationBase services,
           ResponseManager responseManager,
           IStatePropertyAccessor<CalendarSkillState> accessor,
           IServiceManager serviceManager,
           IBotTelemetryClient telemetryClient)
           : base(nameof(FindContactDialog), services, responseManager, accessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var confirmName = new WaterfallStep[]
            {
                ConfirmName,
                AfterConfirmName
            };

            var confirmEmail = new WaterfallStep[]
            {
                ConfirmEmail,
                AfterConfirmEmail
            };

            var updateName = new WaterfallStep[]
            {
                UpdateUserName,
                AfterUpdateUserName,
            };

            AddDialog(new WaterfallDialog(Actions.ConfirmName, confirmName) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ConfirmEmail, confirmEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateName, updateName) { TelemetryClient = telemetryClient });
            InitialDialogId = Actions.ConfirmName;
        }

        public async Task<DialogTurnResult> UpdateUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = (UpdateUserNameDialogOptions)sc.Options;
                if (options.Reason == UpdateUserNameDialogOptions.UpdateReason.ConfirmNo)
                {
                    return await sc.PromptAsync(
                        Actions.Prompt,
                        new PromptOptions
                        {
                            Prompt = ResponseManager.GetResponse(CreateEventResponses.NoAttendees)
                        });
                }

                var currentRecipientName = state.AttendeesNameList[state.ConfirmAttendeesNameIndex];
                if (state.FirstRetryInFindContact)
                {
                    state.FirstRetryInFindContact = false;
                    return await sc.PromptAsync(
                        Actions.Prompt,
                        new PromptOptions
                        {
                            Prompt = ResponseManager.GetResponse(
                                FindContactResponses.UserNotFound,
                                new StringDictionary()
                                {
                                    { "UserName", state.AttendeesNameList[state.ConfirmAttendeesNameIndex] }
                                })
                        });
                }
                else
                {
                    if (state.ConfirmAttendeesNameIndex < state.AttendeesNameList.Count())
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(
                            FindContactResponses.UserNotFoundAgain,
                            new StringDictionary()
                            {
                                { "source", state.EventSource == Models.EventSource.Microsoft ? "Outlook" : "Gmail" },
                                { "UserName", state.AttendeesNameList[state.ConfirmAttendeesNameIndex] }
                            }));
                        state.ConfirmAttendeesNameIndex++;
                        state.FirstRetryInFindContact = true;
                        await sc.CancelAllDialogsAsync();
                        return await sc.BeginDialogAsync(Actions.ConfirmName, options: sc.Options);
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(
                          FindContactResponses.UserNotFoundAgain,
                          new StringDictionary()
                          {
                                { "source", state.EventSource == Models.EventSource.Microsoft ? "Outlook" : "Gmail" },
                                { "UserName", state.AttendeesNameList[state.ConfirmAttendeesNameIndex] }
                          }));
                        state.ConfirmAttendeesNameIndex = 0;
                        state.FirstRetryInFindContact = true;
                        return await sc.CancelAllDialogsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterUpdateUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var userInput = sc.Result as string;
                var state = await Accessor.GetAsync(sc.Context);

                if (string.IsNullOrEmpty(userInput))
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindContactResponses.UserNotFoundAgain, new StringDictionary() { { "source", state.EventSource == EventSource.Microsoft ? "Outlook Calendar" : "Google Calendar" } }));
                    return await sc.EndDialogAsync();
                }

                if (IsEmail(userInput))
                {
                    if (!state.AttendeesNameList.Contains(userInput))
                    {
                        state.AttendeesNameList.Add(userInput);
                    }

                    state.AttendeesNameList.RemoveAt(state.ConfirmAttendeesNameIndex);
                }
                else
                {
                    state.UnconfirmedPerson.Clear();
                    state.AttendeesNameList[state.ConfirmAttendeesNameIndex] = userInput;
                }

                await sc.CancelAllDialogsAsync();

                // should not return with value, next step use the return value for confirmation.
                return await sc.BeginDialogAsync(Actions.ConfirmName, options: sc.Options);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> ConfirmName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                if ((state.AttendeesNameList == null) || (state.AttendeesNameList.Count == 0))
                {
                    return await sc.BeginDialogAsync(Actions.UpdateName, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.NotFound));
                }

                var unionList = new List<CustomizedPerson>();
                var emailList = new List<string>();

                if (state.FirstEnterFindContact)
                {
                    state.FirstEnterFindContact = false;
                    foreach (var name in state.AttendeesNameList)
                    {
                        if (IsEmail(name))
                        {
                            emailList.Add(name);
                        }
                    }

                    if (state.AttendeesNameList.Count > 1)
                    {
                        var nameString = await GetReadyToSendNameListStringAsync(sc);
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindContactResponses.BeforeSendingMessage, new StringDictionary() { { "NameList", nameString } }));
                    }
                }

                if (emailList.Count > 0)
                {
                    foreach (var email in emailList)
                    {
                        var attendee = new EventModel.Attendee
                        {
                            DisplayName = email,
                            Address = email
                        };
                        if (state.Attendees.All(r => r.Address != attendee.Address))
                        {
                            state.Attendees.Add(attendee);
                        }
                    }

                    state.AttendeesNameList.RemoveAll(n => IsEmail(n));

                    if (state.AttendeesNameList.Count > 0)
                    {
                        return await sc.ReplaceDialogAsync(Actions.ConfirmName, options: sc.Options, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        return await sc.EndDialogAsync();
                    }
                }

                if (state.ConfirmAttendeesNameIndex < state.AttendeesNameList.Count)
                {
                    var currentRecipientName = state.AttendeesNameList[state.ConfirmAttendeesNameIndex];

                    if (CreateEventWhiteList.GetMyself(currentRecipientName))
                    {
                        var me = await GetMe(sc);
                        unionList.Add(new CustomizedPerson(me));
                    }
                    else
                    {
                        var originPersonList = await GetPeopleWorkWithAsync(sc, currentRecipientName);
                        var originContactList = await GetContactsAsync(sc, currentRecipientName);
                        originPersonList.AddRange(originContactList);

                        var originUserList = new List<PersonModel>();
                        try
                        {
                            originUserList = await GetUserAsync(sc, currentRecipientName);
                        }
                        catch
                        {
                            // do nothing when get user failed. because can not use token to ensure user use a work account.
                        }

                        (var personList, var userList) = FormatRecipientList(originPersonList, originUserList);

                        // people you work with has the distinct email address has the highest priority
                        if (personList.Count == 1 && personList.First().Emails.Count == 1)
                        {
                            state.ConfirmedPerson = new CustomizedPerson(personList.First());
                            var highestPriorityPerson = new CustomizedPerson(personList.First());
                            return await sc.ReplaceDialogAsync(Actions.ConfirmEmail, highestPriorityPerson);
                        }

                        personList.AddRange(userList);

                        foreach (var person in personList)
                        {
                            if (unionList.Find(p => p.DisplayName == person.DisplayName) == null)
                            {
                                var personWithSameName = personList.FindAll(p => p.DisplayName == person.DisplayName);
                                if (personWithSameName.Count == 1)
                                {
                                    unionList.Add(new CustomizedPerson(personWithSameName.First()));
                                }
                                else
                                {
                                    var unionPerson = new CustomizedPerson(personWithSameName.FirstOrDefault());
                                    var curEmailList = new List<ScoredEmailAddress>();
                                    foreach (var sameNamePerson in personWithSameName)
                                    {
                                        sameNamePerson.Emails.ToList().ForEach(e => curEmailList.Add(new ScoredEmailAddress { Address = e }));
                                    }

                                    unionPerson.Emails = curEmailList;
                                    unionList.Add(unionPerson);
                                }
                            }
                        }
                    }
                }
                else
                {
                    return await sc.EndDialogAsync();
                }

                state.UnconfirmedPerson = unionList;

                if (unionList.Count == 0)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateName, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.NotFound));
                }
                else if (unionList.Count == 1)
                {
                    state.ConfirmedPerson = unionList.First();
                    return await sc.ReplaceDialogAsync(Actions.ConfirmEmail, unionList.First());
                }
                else
                {
                    var nameString = string.Empty;
                    if (unionList.Count <= ConfigData.GetInstance().MaxDisplaySize)
                    {
                        return await sc.PromptAsync(Actions.Choice, await GenerateOptionsForName(sc, unionList, sc.Context, true));
                    }
                    else
                    {
                        return await sc.PromptAsync(Actions.Choice, await GenerateOptionsForName(sc, unionList, sc.Context, false));
                    }
                }
            }
            catch (SkillException skillEx)
            {
                await HandleDialogExceptions(sc, skillEx);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterConfirmName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;
                var generlLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generlLuisResult?.TopIntent().intent;

                if (state.AttendeesNameList != null && state.AttendeesNameList.Count > 0)
                {
                    if (sc.Result == null)
                    {
                        if (generalTopIntent == General.Intent.Next)
                        {
                            state.ShowAttendeesIndex++;
                        }
                        else if (generalTopIntent == General.Intent.Previous)
                        {
                            if (state.ShowAttendeesIndex > 0)
                            {
                                state.ShowAttendeesIndex--;
                            }
                            else
                            {
                                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindContactResponses.AlreadyFirstPage));
                            }
                        }
                        else
                        {
                            // result is null when just update the recipient name. show recipients page should be reset.
                            state.ShowAttendeesIndex = 0;
                        }

                        return await sc.ReplaceDialogAsync(Actions.ConfirmName, options: sc.Options, cancellationToken: cancellationToken);
                    }

                    var choiceResult = (sc.Result as FoundChoice)?.Value.Trim('*');
                    if (choiceResult != null)
                    {
                        // Clean up data
                        state.ShowAttendeesIndex = 0;

                        // Start to confirm the email
                        var confirmedPerson = state.UnconfirmedPerson.Where(p => p.DisplayName.ToLower() == choiceResult.ToLower()).First();
                        state.ConfirmedPerson = confirmedPerson;
                        return await sc.ReplaceDialogAsync(Actions.ConfirmEmail, confirmedPerson);
                    }
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> ConfirmEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);
            var confirmedPerson = sc.Options as CustomizedPerson;
            var name = confirmedPerson.DisplayName;
            if (confirmedPerson.Emails.Count() == 1)
            {
                // Highest probability
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = ResponseManager.GetResponse(FindContactResponses.PromptOneNameOneAddress, new StringDictionary() { { "UserName", name }, { "EmailAddress", confirmedPerson.Emails.First().Address ?? confirmedPerson.UserPrincipalName } }), });
            }
            else
            {
                var emailString = string.Empty;
                var emailList = confirmedPerson.Emails.ToList();

                if (emailList.Count <= ConfigData.GetInstance().MaxDisplaySize)
                {
                    return await sc.PromptAsync(Actions.Choice, await GenerateOptionsForEmail(sc, confirmedPerson, sc.Context, true));
                }
                else
                {
                    return await sc.PromptAsync(Actions.Choice, await GenerateOptionsForEmail(sc, confirmedPerson, sc.Context, false));
                }
            }
        }

        public async Task<DialogTurnResult> AfterConfirmEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var confirmedPerson = state.ConfirmedPerson;
                var name = confirmedPerson.DisplayName;
                if (sc.Result is bool)
                {
                    if ((bool)sc.Result)
                    {
                        var attendee = new EventModel.Attendee
                        {
                            DisplayName = name,
                            Address = confirmedPerson.Emails.First().Address
                        };
                        if (state.Attendees.All(r => r.Address != attendee.Address))
                        {
                            state.Attendees.Add(attendee);
                        }

                        state.FirstRetryInFindContact = true;
                        state.ConfirmAttendeesNameIndex++;
                        if (state.ConfirmAttendeesNameIndex < state.AttendeesNameList.Count)
                        {
                            return await sc.ReplaceDialogAsync(Actions.ConfirmName, options: sc.Options, cancellationToken: cancellationToken);
                        }
                        else
                        {
                            return await sc.EndDialogAsync();
                        }
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Actions.UpdateName, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.ConfirmNo));
                    }
                }

                var luisResult = state.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;
                var generlLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generlLuisResult?.TopIntent().intent;

                if (state.AttendeesNameList != null && state.AttendeesNameList.Count > 0)
                {
                    if (sc.Result == null)
                    {
                        if (generalTopIntent == General.Intent.Next)
                        {
                            state.ShowAttendeesIndex++;
                        }
                        else if (generalTopIntent == General.Intent.Previous)
                        {
                            if (state.ShowAttendeesIndex > 0)
                            {
                                state.ShowAttendeesIndex--;
                            }
                            else
                            {
                                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindContactResponses.AlreadyFirstPage));
                            }
                        }
                        else
                        {
                            // result is null when just update the recipient name. show recipients page should be reset.
                            state.ShowAttendeesIndex = 0;
                        }

                        return await sc.ReplaceDialogAsync(Actions.ConfirmEmail, confirmedPerson);
                    }

                    var choiceResult = (sc.Result as FoundChoice)?.Value.Trim('*');
                    if (choiceResult != null)
                    {
                        // Find an recipient
                        var attendee = new EventModel.Attendee
                        {
                            DisplayName = choiceResult.Split(": ")[0],
                            Address = choiceResult.Split(": ")[1],
                        };
                        if (state.Attendees.All(r => r.Address != attendee.Address))
                        {
                            state.Attendees.Add(attendee);
                        }

                        state.ConfirmAttendeesNameIndex++;
                        state.FirstRetryInFindContact = true;

                        // Clean up data
                        state.ShowAttendeesIndex = 0;
                        state.ConfirmedPerson = new CustomizedPerson();
                    }
                }

                if (state.ConfirmAttendeesNameIndex < state.AttendeesNameList.Count)
                {
                    return await sc.ReplaceDialogAsync(Actions.ConfirmName, options: sc.Options, cancellationToken: cancellationToken);
                }
                else
                {
                    return await sc.EndDialogAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<PromptOptions> GenerateOptionsForEmail(WaterfallStepContext sc, CustomizedPerson confirmedPerson, ITurnContext context, bool isSinglePage = true)
        {
            var state = await Accessor.GetAsync(context);
            var pageIndex = state.ShowAttendeesIndex;
            var pageSize = 3;
            var skip = pageSize * pageIndex;
            var emailList = confirmedPerson.Emails.ToList();

            // Go back to the last page when reaching the end.
            if (skip >= emailList.Count && pageIndex > 0)
            {
                state.ShowAttendeesIndex--;
                pageIndex = state.ShowAttendeesIndex;
                skip = pageSize * pageIndex;
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindContactResponses.AlreadyLastPage));
            }

            var options = new PromptOptions
            {
                Choices = new List<Choice>(),
                Prompt = ResponseManager.GetResponse(FindContactResponses.ConfirmMultiplContactEmailSinglePage, new StringDictionary() { { "UserName", confirmedPerson.DisplayName } })
            };

            if (!isSinglePage)
            {
                options.Prompt = ResponseManager.GetResponse(FindContactResponses.ConfirmMultiplContactEmailMultiPage, new StringDictionary() { { "UserName", confirmedPerson.DisplayName } });
            }

            for (var i = 0; i < emailList.Count; i++)
            {
                var user = confirmedPerson;
                var mailAddress = emailList[i].Address ?? user.UserPrincipalName;

                var choice = new Choice()
                {
                    Value = $"{user.DisplayName}: {mailAddress}",
                    Synonyms = new List<string> { (options.Choices.Count + 1).ToString(), user.DisplayName, user.DisplayName.ToLower(), mailAddress },
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
                        options.Prompt.Speak = SpeakHelper.ToSpeechSelectionDetailString(options, Common.ConfigData.GetInstance().MaxDisplaySize);
                        options.Prompt.Text += "\r\n" + GetSelectPromptEmailString(options, true);
                        options.RetryPrompt = ResponseManager.GetResponse(CalendarSharedResponses.DidntUnderstandMessage);
                        return options;
                    }

                    options.Choices.Add(choice);
                }
                else
                {
                    skip--;
                }
            }

            options.Prompt.Speak = SpeakHelper.ToSpeechSelectionDetailString(options, Common.ConfigData.GetInstance().MaxDisplaySize);
            options.Prompt.Text += "\r\n" + GetSelectPromptEmailString(options, true);
            options.RetryPrompt = ResponseManager.GetResponse(CalendarSharedResponses.DidntUnderstandMessage);
            return options;
        }

        private string GetSelectPromptEmailString(PromptOptions selectOption, bool containNumbers)
        {
            var result = string.Empty;
            for (var i = 0; i < selectOption.Choices.Count; i++)
            {
                var choice = selectOption.Choices[i];
                result += "  ";
                if (containNumbers)
                {
                    result += i + 1 + ": ";
                }

                result += choice.Value.Split(":").LastOrDefault() + "\r\n";
            }

            return result;
        }

        private async Task<PromptOptions> GenerateOptionsForName(WaterfallStepContext sc, List<CustomizedPerson> unionList, ITurnContext context, bool isSinglePage = true)
        {
            var state = await Accessor.GetAsync(context);
            var pageIndex = state.ShowAttendeesIndex;
            var pageSize = 3;
            var skip = pageSize * pageIndex;
            var currentRecipientName = state.AttendeesNameList[state.ConfirmAttendeesNameIndex];

            // Go back to the last page when reaching the end.
            if (skip >= unionList.Count && pageIndex > 0)
            {
                state.ShowAttendeesIndex--;
                pageIndex = state.ShowAttendeesIndex;
                skip = pageSize * pageIndex;
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindContactResponses.AlreadyLastPage));
            }

            var options = new PromptOptions
            {
                Choices = new List<Choice>(),
                Prompt = ResponseManager.GetResponse(FindContactResponses.ConfirmMultipleContactNameSinglePage, new StringDictionary() { { "UserName", currentRecipientName } })
            };

            if (!isSinglePage)
            {
                options.Prompt = ResponseManager.GetResponse(FindContactResponses.ConfirmMultipleContactNameMultiPage, new StringDictionary() { { "UserName", currentRecipientName } });
            }

            for (var i = 0; i < unionList.Count; i++)
            {
                var user = unionList[i];

                var choice = new Choice()
                {
                    Value = $"**{user.DisplayName}**",
                    Synonyms = new List<string> { (options.Choices.Count + 1).ToString(), user.DisplayName, user.DisplayName.ToLower() },
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
                        options.Prompt.Speak = SpeakHelper.ToSpeechSelectionDetailString(options, Common.ConfigData.GetInstance().MaxDisplaySize);
                        options.Prompt.Text = GetSelectPromptString(options, true);
                        options.RetryPrompt = ResponseManager.GetResponse(CalendarSharedResponses.DidntUnderstandMessage);
                        return options;
                    }

                    options.Choices.Add(choice);
                }
                else
                {
                    skip--;
                }
            }

            options.Prompt.Speak = SpeakHelper.ToSpeechSelectionDetailString(options, Common.ConfigData.GetInstance().MaxDisplaySize);
            options.Prompt.Text = GetSelectPromptString(options, true);
            options.RetryPrompt = ResponseManager.GetResponse(CalendarSharedResponses.DidntUnderstandMessage);
            return options;
        }
    }
}