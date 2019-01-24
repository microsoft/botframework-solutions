using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.FindContact.Resources;
using CalendarSkill.Dialogs.Shared;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Data;
using Microsoft.Bot.Solutions.Extensions;
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
           IStatePropertyAccessor<CalendarSkillState> accessor,
           IServiceManager serviceManager,
           IBotTelemetryClient telemetryClient)
           : base(nameof(FindContactDialog), services, accessor, serviceManager, telemetryClient)
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
                var currentRecipientName = state.AttendeesNameList[state.ConfirmAttendeesNameIndex];
                if (state.FirstRetryInFindContact)
                {
                    state.FirstRetryInFindContact = false;
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(FindContactResponses.UserNotFound) });
                }
                else
                {
                    return await sc.CancelAllDialogsAsync();
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
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(FindContactResponses.UserNotFoundAgain, null, new StringDictionary() { { "source", state.EventSource == EventSource.Microsoft ? "Outlook Calendar" : "Google Calendar" } }));
                    return await sc.EndDialogAsync();
                }

                if (IsEmail(userInput))
                {
                    if (!state.AttendeesNameList.Contains(userInput))
                    {
                        state.AttendeesNameList.Add(userInput);
                    }
                }
                else
                {
                    state.AttendeesNameList[state.ConfirmAttendeesNameIndex] = userInput;
                }

                // should not return with value, next step use the return value for confirmation.
                return await sc.EndDialogAsync();
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
                var skillOptions = sc.Options;

                if ((state.AttendeesNameList == null) || (state.AttendeesNameList.Count == 0))
                {
                    return await sc.BeginDialogAsync(Actions.UpdateName);
                }

                var unionList = new List<CustomizedPerson>();
                var emailList = new List<string>();
                if (skillOptions != null)
                {
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
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(FindContactResponses.BeforeSendingMessage, null, new StringDictionary() { { "NameList", nameString } }));
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
                        return await sc.ReplaceDialogAsync(Actions.ConfirmName);
                    }
                    else
                    {
                        return await sc.EndDialogAsync();
                    }
                }

                if (state.ConfirmAttendeesNameIndex < state.AttendeesNameList.Count)
                {
                    var currentRecipientName = state.AttendeesNameList[state.ConfirmAttendeesNameIndex];

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

                state.UnconfirmedPerson = unionList;
                if (unionList.Count == 0)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateRecipientName);
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
                        return await sc.PromptAsync(Actions.Choice, await GenerateOptionsForName(unionList, sc.Context, true));
                    }
                    else
                    {
                        return await sc.PromptAsync(Actions.Choice, await GenerateOptionsForName(unionList, sc.Context, false));
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
                        }
                        else
                        {
                            // result is null when just update the recipient name. show recipients page should be reset.
                            state.ShowAttendeesIndex = 0;
                        }

                        return await sc.ReplaceDialogAsync(Actions.ConfirmName);
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
                var attendee = new EventModel.Attendee
                {
                    DisplayName = name,
                    Address = confirmedPerson.Emails.First().Address
                };
                if (state.Attendees.All(r => r.Address != attendee.Address))
                {
                    state.Attendees.Add(attendee);
                }

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(FindContactResponses.PromptOneNameOneAddress, null, new StringDictionary() { { "UserName", name }, { "EmailAddress", confirmedPerson.Emails.First().Address } }), });
            }
            else
            {
                var emailString = string.Empty;
                var emailList = confirmedPerson.Emails.ToList();

                if (emailList.Count <= ConfigData.GetInstance().MaxDisplaySize)
                {
                    return await sc.PromptAsync(Actions.Choice, await GenerateOptionsForEmail(confirmedPerson, sc.Context, true));
                }
                else
                {
                    return await sc.PromptAsync(Actions.Choice, await GenerateOptionsForEmail(confirmedPerson, sc.Context, false));
                }
            }

        }

        public async Task<DialogTurnResult> AfterConfirmEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (sc.Result is bool)
                {
                    if ((bool)sc.Result)
                    {
                        state.ConfirmAttendeesNameIndex++;
                        return await sc.EndDialogAsync();
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Actions.UpdateRecipientName);
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
                        }
                        else
                        {
                            // result is null when just update the recipient name. show recipients page should be reset.
                            state.ShowAttendeesIndex = 0;
                        }

                        var confirmedPerson = state.ConfirmedPerson;
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

                        // Clean up data
                        state.ShowAttendeesIndex = 0;
                        state.ConfirmedPerson = new CustomizedPerson();
                    }
                }

                if (state.ConfirmAttendeesNameIndex < state.AttendeesNameList.Count)
                {
                    return await sc.ReplaceDialogAsync(Actions.ConfirmName);
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

        protected async Task<bool> ChoiceValidator(PromptValidatorContext<FoundChoice> pc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(pc.Context);
            var luisResult = state.LuisResult;
            var topIntent = luisResult?.TopIntent().intent;
            var generlLuisResult = state.GeneralLuisResult;
            var generalTopIntent = generlLuisResult?.TopIntent().intent;

            if ((generalTopIntent == General.Intent.Next)
                || (generalTopIntent == General.Intent.Previous))
            {
                return true;
            }
            else
            {
                if (!pc.Recognized.Succeeded || pc.Recognized == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}