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
using CalendarSkill.Dialogs.Shared.Resources.Strings;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using CalendarSkill.Util;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
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

            var confirmAttendee = new WaterfallStep[]
            {
                ConfirmName,
                ConfirmEmail,
                AfterConfirmEmail
            };

            var loopNameList = new WaterfallStep[]
            {
                LoopNameList,
                AfterLoopNameList
            };

            var updateName = new WaterfallStep[]
            {
                UpdateUserName,
                AfterUpdateUserName,
            };

            var confirmNameList = new WaterfallStep[]
            {
                ConfirmNameList,
                AfterConfirmNameList,
            };

            var selectPerson = new WaterfallStep[]
            {
                SelectPerson,
                AfterSelectPerson
            };

            var selectEmail = new WaterfallStep[]
            {
                SelectEmail,
                AfterSelectEmail
            };

            AddDialog(new WaterfallDialog(Actions.ConfirmAttendee, confirmAttendee) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.LoopNameList, loopNameList) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateName, updateName) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.SelectPerson, selectPerson) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.SelectEmail, selectEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ConfirmNameList, confirmNameList) { TelemetryClient = telemetryClient });
            InitialDialogId = Actions.ConfirmNameList;
        }

        public async Task<DialogTurnResult> ConfirmNameList(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.AttendeesNameList.Any())
                {
                    return await sc.NextAsync();
                }

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(FindContactResponses.NoAttendees) }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterConfirmNameList(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                    if (CreateEventWhiteList.IsSkip(userInput))
                    {
                        state.AttendeesNameList = new List<string>();
                        state.AttendeesNameList.Add(CalendarCommonStrings.MyselfConst);
                    }
                    else
                    if (state.EventSource != EventSource.Other)
                    {
                        if (userInput != null)
                        {
                            var nameList = userInput.Split(CreateEventWhiteList.GetContactNameSeparator(), StringSplitOptions.None)
                                .Select(x => x.Trim())
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .ToList();
                            state.AttendeesNameList = nameList;
                        }
                    }
                }

                if (state.AttendeesNameList.Any())
                {
                    if (state.AttendeesNameList.Count > 1)
                    {
                        var nameString = await GetReadyToSendNameListStringAsync(sc);
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindContactResponses.BeforeSendingMessage, new StringDictionary() { { "NameList", nameString } }));
                    }

                    state.ConfirmAttendeesNameIndex = 0;
                    return await sc.ReplaceDialogAsync(Actions.LoopNameList, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.Initialize), cancellationToken);
                }

                state.AttendeesNameList = new List<string>();
                state.CurrentAttendeeName = string.Empty;
                state.ConfirmAttendeesNameIndex = 0;
                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> LoopNameList(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.ConfirmAttendeesNameIndex < state.AttendeesNameList.Count)
                {
                    state.CurrentAttendeeName = state.AttendeesNameList[state.ConfirmAttendeesNameIndex];
                    return await sc.BeginDialogAsync(Actions.ConfirmAttendee, sc.Options, cancellationToken);
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

        public async Task<DialogTurnResult> AfterLoopNameList(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                state.ConfirmAttendeesNameIndex = state.ConfirmAttendeesNameIndex + 1;
                state.ConfirmedPerson = null;
                return await sc.ReplaceDialogAsync(Actions.LoopNameList, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.Initialize), cancellationToken);
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
                var options = (UpdateUserNameDialogOptions)sc.Options;
                return await sc.BeginDialogAsync(Actions.UpdateName, options: options, cancellationToken: cancellationToken);
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

        public async Task<DialogTurnResult> ConfirmEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);
            var confirmedPerson = state.ConfirmedPerson;
            if (confirmedPerson == null)
            {
                return await sc.EndDialogAsync();
            }

            var name = confirmedPerson.DisplayName;
            if (confirmedPerson.Emails.Count() == 1)
            {
                // Highest probability
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = ResponseManager.GetResponse(FindContactResponses.PromptOneNameOneAddress, new StringDictionary() { { "UserName", name }, { "EmailAddress", confirmedPerson.Emails.First().Address ?? confirmedPerson.UserPrincipalName } }), });
            }
            else
            {
                return await sc.BeginDialogAsync(Actions.SelectEmail);
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
                        return await sc.EndDialogAsync();
                    }
                    else
                    {
                        state.FirstRetryInFindContact = true;
                        return await sc.ReplaceDialogAsync(Actions.ConfirmAttendee, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.ConfirmNo));
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

        public async Task<DialogTurnResult> UpdateUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                state.UnconfirmedPerson.Clear();
                state.ConfirmedPerson = null;
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

                var currentRecipientName = state.CurrentAttendeeName;

                if (options.Reason != UpdateUserNameDialogOptions.UpdateReason.Initialize)
                {
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
                                    { "UserName", currentRecipientName }
                                    })
                            });
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(
                            FindContactResponses.UserNotFoundAgain,
                            new StringDictionary()
                            {
                            { "source", state.EventSource == Models.EventSource.Microsoft ? "Outlook" : "Gmail" },
                            { "UserName", currentRecipientName }
                            }));
                        state.FirstRetryInFindContact = true;
                        state.CurrentAttendeeName = string.Empty;
                        return await sc.EndDialogAsync();
                    }
                }

                return await sc.NextAsync();
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
                var options = (UpdateUserNameDialogOptions)sc.Options;

                if (string.IsNullOrEmpty(userInput) && options.Reason != UpdateUserNameDialogOptions.UpdateReason.Initialize)
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindContactResponses.UserNotFoundAgain, new StringDictionary() { { "source", state.EventSource == EventSource.Microsoft ? "Outlook Calendar" : "Google Calendar" } }));
                    return await sc.EndDialogAsync();
                }

                string currentRecipientName = string.IsNullOrEmpty(userInput) ? state.CurrentAttendeeName : userInput;

                if (!string.IsNullOrEmpty(currentRecipientName) && IsEmail(currentRecipientName))
                {
                    var attendee = new EventModel.Attendee
                    {
                        DisplayName = currentRecipientName,
                        Address = currentRecipientName
                    };
                    if (state.Attendees.All(r => r.Address != attendee.Address))
                    {
                        state.Attendees.Add(attendee);
                    }

                    state.CurrentAttendeeName = string.Empty;
                    return await sc.EndDialogAsync();
                }

                List<CustomizedPerson> unionList = new List<CustomizedPerson>();

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
                    if (personList.Count == 1 && personList.First().Emails.Any() && personList.First().Emails.First() != null)
                    {
                        unionList.Add(new CustomizedPerson(personList.First()));
                    }
                    else
                    {
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
                                        sameNamePerson.Emails.ToList().ForEach(e =>
                                        {
                                            if (!string.IsNullOrEmpty(e))
                                            {
                                                curEmailList.Add(new ScoredEmailAddress { Address = e });
                                            }
                                        });
                                    }

                                    unionPerson.Emails = curEmailList;
                                    unionList.Add(unionPerson);
                                }
                            }
                        }
                    }
                }

                unionList.RemoveAll(person => !person.Emails.Exists(email => email.Address != null));
                unionList.RemoveAll(person => !person.Emails.Any());

                state.UnconfirmedPerson = unionList;

                if (unionList.Count == 0)
                {
                    return await sc.ReplaceDialogAsync(Actions.UpdateName, new UpdateUserNameDialogOptions(UpdateUserNameDialogOptions.UpdateReason.NotFound));
                }
                else
                if (unionList.Count == 1)
                {
                    state.ConfirmedPerson = unionList.First();
                    return await sc.EndDialogAsync();
                }
                else
                {
                    return await sc.ReplaceDialogAsync(Actions.SelectPerson, sc.Options, cancellationToken);
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

        public async Task<DialogTurnResult> SelectPerson(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var unionList = state.UnconfirmedPerson;
                if (unionList.Count <= ConfigData.GetInstance().MaxDisplaySize)
                {
                    return await sc.PromptAsync(Actions.Choice, await GenerateOptionsForName(sc, unionList, sc.Context, true));
                }
                else
                {
                    return await sc.PromptAsync(Actions.Choice, await GenerateOptionsForName(sc, unionList, sc.Context, false));
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterSelectPerson(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;
                var generlLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generlLuisResult?.TopIntent().intent;
                generalTopIntent = MergeShowIntent(generalTopIntent, topIntent, luisResult);

                if (sc.Result == null)
                {
                    if (generalTopIntent == General.Intent.ShowNext)
                    {
                        state.ShowAttendeesIndex++;
                    }
                    else if (generalTopIntent == General.Intent.ShowPrevious)
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

                    return await sc.ReplaceDialogAsync(Actions.SelectPerson, options: sc.Options, cancellationToken: cancellationToken);
                }

                var choiceResult = (sc.Result as FoundChoice)?.Value.Trim('*');
                if (choiceResult != null)
                {
                    // Clean up data
                    state.ShowAttendeesIndex = 0;

                    // Start to confirm the email
                    var confirmedPerson = state.UnconfirmedPerson.Where(p => p.DisplayName.ToLower() == choiceResult.ToLower()).First();
                    state.ConfirmedPerson = confirmedPerson;
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> SelectEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var confirmedPerson = state.ConfirmedPerson;
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
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterSelectEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;
                var generlLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generlLuisResult?.TopIntent().intent;
                generalTopIntent = MergeShowIntent(generalTopIntent, topIntent, luisResult);

                if (sc.Result == null)
                {
                    if (generalTopIntent == General.Intent.ShowNext)
                    {
                        state.ShowAttendeesIndex++;
                    }
                    else if (generalTopIntent == General.Intent.ShowPrevious)
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

                    return await sc.ReplaceDialogAsync(Actions.SelectEmail, sc.Options, cancellationToken);
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

                    state.FirstRetryInFindContact = true;

                    // Clean up data
                    state.ShowAttendeesIndex = 0;
                    state.ConfirmedPerson = new CustomizedPerson();
                }

                return await sc.EndDialogAsync();
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
            var currentRecipientName = state.CurrentAttendeeName;

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