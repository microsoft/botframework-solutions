// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Responses.CheckAvailable;
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Responses.FindContact;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Graph;
using static CalendarSkill.Models.CalendarSkillState;

namespace CalendarSkill.Dialogs
{
    public class FindContactDialog : CalendarSkillDialogBase
    {
        public FindContactDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(FindContactDialog), settings, services, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            // entry, get the name list
            var confirmNameList = new WaterfallStep[]
            {
                ConfirmNameList,
                AfterConfirmNameList,
            };

            // go through the name list, replace the confirmNameList
            // set state.MeetingInfor.ContactInfor.CurrentContactName
            var loopNameList = new WaterfallStep[]
            {
                LoopNameList,
                AfterLoopNameList
            };

            // check on the attendee of state.MeetingInfor.ContactInfor.CurrentContactName.
            // called by loopNameList
            var confirmAttendee = new WaterfallStep[]
            {
                // call updateName to get the person state.MeetingInfor.ContactInfor.ConfirmedContact.
                // state.MeetingInfor.ContactInfor.ConfirmedContact should be set after this step
                ConfirmName,

                // check if the state.MeetingInfor.ContactInfor.ConfirmedContact
                //  - null : failed to parse this name for multiple try.
                //  - one email : check if this one is wanted
                //  - multiple emails : call selectEmail
                ConfirmEmail,

                // if got no on last step, replace/restart this flow.
                AfterConfirmEmail
            };

            // use the user name of state.MeetingInfor.ContactInfor.CurrentContactName or user input to find the persons.
            // and will call select person.
            // after all this done, state.MeetingInfor.ContactInfor.ConfirmedContact should be set.
            var updateName = new WaterfallStep[]
            {
                // check whether should the bot ask for attendee name.
                // if called by confirmAttendee then skip this step.
                // if called by itself when can not find the last input, it will ask back or end this one when multiple try.
                UpdateUserName,

                // check if email. add email direct into attendee and set state.MeetingInfor.ContactInfor.ConfirmedContact null.
                // if not, search for the attendee.
                // if got multiple persons, call selectPerson. use replace
                // if got no person, replace/restart this flow.
                AfterUpdateUserName,
                GetAuthToken,
                AfterGetAuthToken,
                GetUserFromUserName
            };

            // select person, called bt updateName with replace.
            var selectPerson = new WaterfallStep[]
            {
                SelectPerson,
                AfterSelectPerson
            };

            // select email.
            // called by ConfirmEmail
            var selectEmail = new WaterfallStep[]
            {
                SelectEmail,
                AfterSelectEmail
            };

            var addMoreUserPrompt = new WaterfallStep[]
            {
                AddMoreUserPrompt,
                AfterAddMoreUserPrompt
            };

            AddDialog(new WaterfallDialog(Actions.ConfirmNameList, confirmNameList) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.LoopNameList, loopNameList) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ConfirmAttendee, confirmAttendee) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateName, updateName) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.SelectPerson, selectPerson) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.SelectEmail, selectEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.AddMoreUserPrompt, addMoreUserPrompt) { TelemetryClient = telemetryClient });
            InitialDialogId = Actions.ConfirmNameList;
        }

        private async Task<DialogTurnResult> ConfirmNameList(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = sc.Options as FindContactDialogOptions;

                if (options.Scenario.Equals(nameof(CheckAvailableDialog)))
                {
                    options.PromptMoreContact = false;
                }

                // got attendee name list already.
                if (state.MeetingInfor.ContactInfor.ContactsNameList.Any())
                {
                    if (options != null && options.FindContactReason == FindContactDialogOptions.FindContactReasonType.FirstFindContact)
                    {
                        if (state.MeetingInfor.ContactInfor.ContactsNameList.Count > 1)
                        {
                            options.PromptMoreContact = false;
                        }
                    }

                    return await sc.NextAsync();
                }

                // ask for attendee
                if (options.Scenario.Equals(nameof(CheckAvailableDialog)))
                {
                    var prompt = await LGHelper.GenerateMessageAsync(sc.Context, CheckAvailableResponses.AskForCheckAvailableUserName, null) as Activity;
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                }
                else if (options.FindContactReason == FindContactDialogOptions.FindContactReasonType.FirstFindContact)
                {
                    var prompt = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.NoAttendees, null) as Activity;
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                }
                else
                {
                    var prompt = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.AddMoreAttendees, null) as Activity;
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterConfirmNameList(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = sc.Options as FindContactDialogOptions;

                // get name list from sc.result
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                    // if is skip. set the name list to be myself only.
                    if (CreateEventWhiteList.IsSkip(userInput))
                    {
                        state.MeetingInfor.ContactInfor.ContactsNameList = new List<string>
                        {
                            CalendarCommonStrings.MyselfConst
                        };
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
                            state.MeetingInfor.ContactInfor.ContactsNameList = nameList;
                        }
                    }
                }

                if (state.MeetingInfor.ContactInfor.ContactsNameList.Any())
                {
                    if (state.MeetingInfor.ContactInfor.ContactsNameList.Count > 1 && !options.Scenario.Equals(nameof(CheckAvailableDialog)))
                    {
                        var nameString = await GetReadyToSendNameListStringAsync(sc);
                        var activity = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.BeforeSendingMessage, new
                        {
                            NameList = nameString
                        });
                        await sc.Context.SendActivityAsync(activity);
                    }

                    // go to loop to go through all the names
                    state.MeetingInfor.ContactInfor.ConfirmContactsNameIndex = 0;
                    return await sc.ReplaceDialogAsync(Actions.LoopNameList, sc.Options, cancellationToken);
                }

                // todo:
                state.MeetingInfor.ContactInfor.ContactsNameList = new List<string>();
                state.MeetingInfor.ContactInfor.CurrentContactName = string.Empty;
                state.MeetingInfor.ContactInfor.ConfirmContactsNameIndex = 0;
                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> LoopNameList(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = sc.Options as FindContactDialogOptions;

                // check available dialog can only recieve one contact
                if (state.MeetingInfor.ContactInfor.ConfirmContactsNameIndex < state.MeetingInfor.ContactInfor.ContactsNameList.Count && !(options.Scenario.Equals(nameof(CheckAvailableDialog)) && state.MeetingInfor.ContactInfor.ConfirmContactsNameIndex > 0))
                {
                    state.MeetingInfor.ContactInfor.CurrentContactName = state.MeetingInfor.ContactInfor.ContactsNameList[state.MeetingInfor.ContactInfor.ConfirmContactsNameIndex];
                    options.UpdateUserNameReason = FindContactDialogOptions.UpdateUserNameReasonType.Initialize;
                    return await sc.BeginDialogAsync(Actions.ConfirmAttendee, sc.Options, cancellationToken);
                }
                else
                {
                    state.MeetingInfor.ContactInfor.ContactsNameList = new List<string>();
                    state.MeetingInfor.ContactInfor.CurrentContactName = string.Empty;
                    state.MeetingInfor.ContactInfor.ConfirmContactsNameIndex = 0;
                    if (options.PromptMoreContact && state.MeetingInfor.ContactInfor.Contacts.Count < 20)
                    {
                        return await sc.ReplaceDialogAsync(Actions.AddMoreUserPrompt, options);
                    }
                    else
                    {
                        return await sc.EndDialogAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterLoopNameList(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                state.MeetingInfor.ContactInfor.ConfirmContactsNameIndex = state.MeetingInfor.ContactInfor.ConfirmContactsNameIndex + 1;
                state.MeetingInfor.ContactInfor.ConfirmedContact = null;
                return await sc.ReplaceDialogAsync(Actions.LoopNameList, sc.Options, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ConfirmName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                // when called bt LoopNameList, the options reason is initialize.
                // when replaced by itself, the reason will be Confirm No.
                var options = (FindContactDialogOptions)sc.Options;

                // set the ConfirmPerson to null as default.
                state.MeetingInfor.ContactInfor.ConfirmedContact = null;
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

        private async Task<DialogTurnResult> ConfirmEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = sc.Options as FindContactDialogOptions;
                var confirmedPerson = state.MeetingInfor.ContactInfor.ConfirmedContact;
                if (confirmedPerson == null)
                {
                    return await sc.EndDialogAsync();
                }

                var name = confirmedPerson.DisplayName;
                var userString = string.Empty;
                if (!name.Equals(confirmedPerson.Emails.First().Address ?? confirmedPerson.UserPrincipalName))
                {
                    userString = name + ": ";
                }

                userString += confirmedPerson.Emails.First().Address ?? confirmedPerson.UserPrincipalName;

                if (confirmedPerson.Emails.Count() == 1)
                {
                    // Highest probability
                    if (!options.Scenario.Equals(nameof(CheckAvailableDialog)))
                    {
                        var activity = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.PromptOneNameOneAddress, new
                        {
                            User = userString
                        });
                        await sc.Context.SendActivityAsync(activity);
                    }

                    return await sc.NextAsync();
                }
                else
                {
                    return await sc.BeginDialogAsync(Actions.SelectEmail);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterConfirmEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var confirmedPerson = state.MeetingInfor.ContactInfor.ConfirmedContact;
                var name = confirmedPerson.DisplayName;

                var attendee = new EventModel.Attendee
                {
                    DisplayName = name,
                    Address = confirmedPerson.Emails.First().Address,
                    UserPrincipalName = confirmedPerson.UserPrincipalName
                };
                if (state.MeetingInfor.ContactInfor.Contacts.All(r => r.Address != attendee.Address))
                {
                    state.MeetingInfor.ContactInfor.Contacts.Add(attendee);
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> UpdateUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                state.MeetingInfor.ContactInfor.UnconfirmedContact.Clear();
                state.MeetingInfor.ContactInfor.ConfirmedContact = null;
                var options = (FindContactDialogOptions)sc.Options;

                // if it is confirm no, thenask user to give a new attendee
                if (options.UpdateUserNameReason == FindContactDialogOptions.UpdateUserNameReasonType.ConfirmNo)
                {
                    return await sc.PromptAsync(
                        Actions.Prompt,
                        new PromptOptions
                        {
                            Prompt = await LGHelper.GenerateMessageAsync(sc.Context, CreateEventResponses.NoAttendees, null) as Activity,
                        });
                }

                var currentRecipientName = state.MeetingInfor.ContactInfor.CurrentContactName;

                // if not initialize ask user for attendee
                if (options.UpdateUserNameReason != FindContactDialogOptions.UpdateUserNameReasonType.Initialize)
                {
                    if (options.FirstRetry)
                    {
                        return await sc.PromptAsync(
                            Actions.Prompt,
                            new PromptOptions
                            {
                                Prompt = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.UserNotFound, new
                                {
                                    UserName = currentRecipientName
                                }) as Activity
                            });
                    }
                    else
                    {
                        return await sc.PromptAsync(
                            Actions.Prompt,
                            new PromptOptions
                            {
                                Prompt = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.UserNotFoundAgain, new
                                {
                                    Source = state.EventSource == Models.EventSource.Microsoft ? "Outlook" : "Gmail",
                                    UserName = currentRecipientName
                                }) as Activity,
                            });
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

        private async Task<DialogTurnResult> AfterUpdateUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var userInput = sc.Result as string;
                var state = await Accessor.GetAsync(sc.Context);
                var currentRecipientName = state.MeetingInfor.ContactInfor.CurrentContactName;
                var options = (FindContactDialogOptions)sc.Options;

                if (string.IsNullOrEmpty(userInput) && options.UpdateUserNameReason != FindContactDialogOptions.UpdateUserNameReasonType.Initialize)
                {
                    var activity = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.UserNotFoundAgain, new
                    {
                        Source = state.EventSource == EventSource.Microsoft ? "Outlook Calendar" : "Google Calendar",
                        UserName = currentRecipientName
                    });
                    await sc.Context.SendActivityAsync(activity);
                    return await sc.EndDialogAsync();
                }

                state.MeetingInfor.ContactInfor.CurrentContactName = string.IsNullOrEmpty(userInput) ? state.MeetingInfor.ContactInfor.CurrentContactName : userInput;

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> GetUserFromUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = (FindContactDialogOptions)sc.Options;
                var currentRecipientName = state.MeetingInfor.ContactInfor.CurrentContactName;

                // if it's an email, add to attendee and kepp the state.MeetingInfor.ContactInfor.ConfirmedContact null
                if (!string.IsNullOrEmpty(currentRecipientName) && IsEmail(currentRecipientName))
                {
                    var attendee = new EventModel.Attendee
                    {
                        DisplayName = currentRecipientName,
                        Address = currentRecipientName,
                        UserPrincipalName = currentRecipientName,
                    };
                    if (state.MeetingInfor.ContactInfor.Contacts.All(r => r.Address != attendee.Address))
                    {
                        state.MeetingInfor.ContactInfor.Contacts.Add(attendee);
                    }

                    state.MeetingInfor.ContactInfor.CurrentContactName = string.Empty;
                    state.MeetingInfor.ContactInfor.ConfirmedContact = null;
                    return await sc.EndDialogAsync();
                }

                var unionList = new List<CustomizedPerson>();

                if (CreateEventWhiteList.GetMyself(currentRecipientName))
                {
                    var me = await GetMe(sc.Context);
                    unionList.Add(new CustomizedPerson(me));
                }
                else if (!string.IsNullOrEmpty(currentRecipientName) && state.MeetingInfor.ContactInfor.RelatedEntityInfoDict.ContainsKey(currentRecipientName))
                {
                    string pronounType = state.MeetingInfor.ContactInfor.RelatedEntityInfoDict[currentRecipientName].PronounType;
                    string relationship = state.MeetingInfor.ContactInfor.RelatedEntityInfoDict[currentRecipientName].RelationshipName;
                    var personList = new List<PersonModel>();
                    if (pronounType == PronounType.FirstPerson)
                    {
                        if (Regex.IsMatch(relationship, CalendarCommonStrings.Manager, RegexOptions.IgnoreCase))
                        {
                            var person = await GetMyManager(sc);
                            if (person != null)
                            {
                                personList.Add(person);
                            }
                        }
                    }
                    else if (pronounType == PronounType.ThirdPerson && state.MeetingInfor.ContactInfor.Contacts.Count > 0)
                    {
                        int count = state.MeetingInfor.ContactInfor.Contacts.Count;
                        string prename = state.MeetingInfor.ContactInfor.Contacts[count - 1].UserPrincipalName;
                        if (Regex.IsMatch(relationship, CalendarCommonStrings.Manager, RegexOptions.IgnoreCase))
                        {
                            var person = await GetManager(sc, prename);
                            if (person != null)
                            {
                                personList.Add(person);
                            }
                        }
                    }

                    foreach (var person in personList)
                    {
                        unionList.Add(new CustomizedPerson(person));
                    }
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

                state.MeetingInfor.ContactInfor.UnconfirmedContact = unionList;

                if (unionList.Count == 0)
                {
                    if (!(options.UpdateUserNameReason == FindContactDialogOptions.UpdateUserNameReasonType.Initialize))
                    {
                        options.FirstRetry = false;
                    }

                    options.UpdateUserNameReason = FindContactDialogOptions.UpdateUserNameReasonType.NotFound;
                    return await sc.ReplaceDialogAsync(Actions.UpdateName, options);
                }
                else
                if (unionList.Count == 1)
                {
                    state.MeetingInfor.ContactInfor.ConfirmedContact = unionList.First();
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

        private async Task<DialogTurnResult> SelectPerson(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var unionList = state.MeetingInfor.ContactInfor.UnconfirmedContact;
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

        private async Task<DialogTurnResult> AfterSelectPerson(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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
                        state.MeetingInfor.ContactInfor.ShowContactsIndex++;
                    }
                    else if (generalTopIntent == General.Intent.ShowPrevious)
                    {
                        if (state.MeetingInfor.ContactInfor.ShowContactsIndex > 0)
                        {
                            state.MeetingInfor.ContactInfor.ShowContactsIndex--;
                        }
                        else
                        {
                            var activity = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.AlreadyFirstPage, null);
                            await sc.Context.SendActivityAsync(activity);
                        }
                    }
                    else
                    {
                        // result is null when just update the recipient name. show recipients page should be reset.
                        state.MeetingInfor.ContactInfor.ShowContactsIndex = 0;
                    }

                    return await sc.ReplaceDialogAsync(Actions.SelectPerson, options: sc.Options, cancellationToken: cancellationToken);
                }

                var choiceResult = (sc.Result as FoundChoice)?.Value.Trim('*');
                if (choiceResult != null)
                {
                    // Clean up data
                    state.MeetingInfor.ContactInfor.ShowContactsIndex = 0;

                    // Start to confirm the email
                    var confirmedPerson = state.MeetingInfor.ContactInfor.UnconfirmedContact.Where(p => p.DisplayName.ToLower() == choiceResult.ToLower()).First();
                    state.MeetingInfor.ContactInfor.ConfirmedContact = confirmedPerson;
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> SelectEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var confirmedPerson = state.MeetingInfor.ContactInfor.ConfirmedContact;
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

        private async Task<DialogTurnResult> AfterSelectEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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
                        state.MeetingInfor.ContactInfor.ShowContactsIndex++;
                    }
                    else if (generalTopIntent == General.Intent.ShowPrevious)
                    {
                        if (state.MeetingInfor.ContactInfor.ShowContactsIndex > 0)
                        {
                            state.MeetingInfor.ContactInfor.ShowContactsIndex--;
                        }
                        else
                        {
                            var activity = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.AlreadyFirstPage, null);
                            await sc.Context.SendActivityAsync(activity);
                        }
                    }
                    else
                    {
                        // result is null when just update the recipient name. show recipients page should be reset.
                        state.MeetingInfor.ContactInfor.ShowContactsIndex = 0;
                    }

                    return await sc.ReplaceDialogAsync(Actions.SelectEmail, sc.Options, cancellationToken);
                }

                var choiceResult = (sc.Result as FoundChoice)?.Value.Trim('*');
                if (choiceResult != null)
                {
                    state.MeetingInfor.ContactInfor.ConfirmedContact.DisplayName = choiceResult.Split(": ")[0];
                    state.MeetingInfor.ContactInfor.ConfirmedContact.Emails.First().Address = choiceResult.Split(": ")[1];

                    // Clean up data
                    state.MeetingInfor.ContactInfor.ShowContactsIndex = 0;
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AddMoreUserPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var users = state.MeetingInfor.ContactInfor.Contacts.ToSpeechString(CommonStrings.And, li => $"{li.DisplayName ?? li.Address}");
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.AddMoreUserPrompt, new
                    {
                        Users = users
                    }) as Activity,
                    RetryPrompt = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.AddMoreUserPrompt, new
                    {
                        Users = users
                    }) as Activity,
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterAddMoreUserPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var result = (bool)sc.Result;
                if (result)
                {
                    var options = sc.Options as FindContactDialogOptions;
                    options.FindContactReason = FindContactDialogOptions.FindContactReasonType.FindContactAgain;
                    return await sc.ReplaceDialogAsync(Actions.ConfirmNameList, options);
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
            var pageIndex = state.MeetingInfor.ContactInfor.ShowContactsIndex;
            var pageSize = 3;
            var skip = pageSize * pageIndex;
            var emailList = confirmedPerson.Emails.ToList();

            // Go back to the last page when reaching the end.
            if (skip >= emailList.Count && pageIndex > 0)
            {
                state.MeetingInfor.ContactInfor.ShowContactsIndex--;
                pageIndex = state.MeetingInfor.ContactInfor.ShowContactsIndex;
                skip = pageSize * pageIndex;
                var activity = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.AlreadyLastPage, null);
                await sc.Context.SendActivityAsync(activity);
            }

            var options = new PromptOptions
            {
                Choices = new List<Choice>(),
                Prompt = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.ConfirmMultipleContactEmailSinglePage, new
                {
                    UserName = confirmedPerson.DisplayName
                }) as Activity
            };

            if (!isSinglePage)
            {
                options.Prompt = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.ConfirmMultipleContactEmailMultiPage, new
                {
                    UserName = confirmedPerson.DisplayName
                }) as Activity;
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
                        options.Prompt.Speak = SpeechUtility.ListToSpeechReadyString(options, ReadPreference.Chronological, ConfigData.GetInstance().MaxReadSize);
                        options.Prompt.Text += "\r\n" + GetSelectPromptEmailString(options, true);
                        options.RetryPrompt = await LGHelper.GenerateMessageAsync(sc.Context, CalendarSharedResponses.DidntUnderstandMessage, null) as Activity;
                        return options;
                    }

                    options.Choices.Add(choice);
                }
                else
                {
                    skip--;
                }
            }

            options.Prompt.Speak = SpeechUtility.ListToSpeechReadyString(options, ReadPreference.Chronological, ConfigData.GetInstance().MaxReadSize);
            options.Prompt.Text += "\r\n" + GetSelectPromptEmailString(options, true);
            options.RetryPrompt = await LGHelper.GenerateMessageAsync(sc.Context, CalendarSharedResponses.DidntUnderstandMessage, null) as Activity;
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
            var pageIndex = state.MeetingInfor.ContactInfor.ShowContactsIndex;
            var pageSize = 3;
            var skip = pageSize * pageIndex;
            var currentRecipientName = state.MeetingInfor.ContactInfor.CurrentContactName;

            // Go back to the last page when reaching the end.
            if (skip >= unionList.Count && pageIndex > 0)
            {
                state.MeetingInfor.ContactInfor.ShowContactsIndex--;
                pageIndex = state.MeetingInfor.ContactInfor.ShowContactsIndex;
                skip = pageSize * pageIndex;
                var activity = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.AlreadyLastPage, null);
                await sc.Context.SendActivityAsync(activity);
            }

            var options = new PromptOptions
            {
                Choices = new List<Choice>(),
                Prompt = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.ConfirmMultipleContactNameSinglePage, new
                {
                    UserName = currentRecipientName
                }) as Activity
            };

            if (!isSinglePage)
            {
                options.Prompt = await LGHelper.GenerateMessageAsync(sc.Context, FindContactResponses.ConfirmMultipleContactNameMultiPage, new
                {
                    UserName = currentRecipientName
                }) as Activity;
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
                        options.Prompt.Speak = SpeechUtility.ListToSpeechReadyString(options, ReadPreference.Chronological, ConfigData.GetInstance().MaxReadSize);
                        options.Prompt.Text = GetSelectPromptString(options, true);
                        options.RetryPrompt = await LGHelper.GenerateMessageAsync(sc.Context, CalendarSharedResponses.DidntUnderstandMessage, null) as Activity;
                        return options;
                    }

                    options.Choices.Add(choice);
                }
                else
                {
                    skip--;
                }
            }

            options.Prompt.Speak = SpeechUtility.ListToSpeechReadyString(options, ReadPreference.Chronological, ConfigData.GetInstance().MaxReadSize);
            options.Prompt.Text = GetSelectPromptString(options, true);
            options.RetryPrompt = await LGHelper.GenerateMessageAsync(sc.Context, CalendarSharedResponses.DidntUnderstandMessage, null) as Activity;
            return options;
        }

        private class PronounType
        {
            public const string FirstPerson = "FirstPerson";
            public const string ThirdPerson = "ThirdPerson";
        }
    }
}