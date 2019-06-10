using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogModel;
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Responses.FindContact;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Graph;
using static CalendarSkill.Models.CalendarDialogStateBase;

namespace CalendarSkill.Dialogs
{
    public class FindContactDialog : CalendarSkillDialogBase
    {
        public FindContactDialog(
           BotSettings settings,
           BotServices services,
           ResponseManager responseManager,
           ConversationState conversationState,
           IServiceManager serviceManager,
           IBotTelemetryClient telemetryClient)
          : base(nameof(FindContactDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            // entry, get the name list
            var confirmNameList = new WaterfallStep[]
            {
                InitFindContactDialogState,
                ConfirmNameList,
                AfterConfirmNameList,
            };

            // go through the name list, replace the confirmNameList
            // set dialogState.FindContactInfor.CurrentContactName
            var loopNameList = new WaterfallStep[]
            {
                SaveCreateEventDialogState,
                LoopNameList,
                AfterLoopNameList
            };

            // check on the attendee of dialogState.FindContactInfor.CurrentContactName.
            // called by loopNameList
            var confirmAttendee = new WaterfallStep[]
            {
                SaveCreateEventDialogState,

                // call updateName to get the person dialogState.FindContactInfor.ConfirmedContact.
                // dialogState.FindContactInfor.ConfirmedContact should be set after this step
                ConfirmName,

                // check if the dialogState.FindContactInfor.ConfirmedContact
                //  - null : failed to parse this name for multiple try.
                //  - one email : check if this one is wanted
                //  - multiple emails : call selectEmail
                ConfirmEmail,

                // if got no on last step, replace/restart this flow.
                AfterConfirmEmail
            };

            // use the user name of dialogState.FindContactInfor.CurrentContactName or user input to find the persons.
            // and will call select person.
            // after all this done, dialogState.FindContactInfor.ConfirmedContact should be set.
            var updateName = new WaterfallStep[]
            {
                SaveCreateEventDialogState,

                // check whether should the bot ask for attendee name.
                // if called by confirmAttendee then skip this step.
                // if called by itself when can not find the last input, it will ask back or end this one when multiple try.
                UpdateUserName,

                // check if email. add email direct into attendee and set dialogState.FindContactInfor.ConfirmedContact null.
                // if not, search for the attendee.
                // if got multiple persons, call selectPerson. use replace
                // if got no person, replace/restart this flow.
                AfterUpdateUserName,
            };

            // select person, called bt updateName with replace.
            var selectPerson = new WaterfallStep[]
            {
                SaveCreateEventDialogState,
                SelectPerson,
                AfterSelectPerson
            };

            // select email.
            // called by ConfirmEmail
            var selectEmail = new WaterfallStep[]
            {
                SaveCreateEventDialogState,
                SelectEmail,
                AfterSelectEmail
            };

            var addMoreUserPrompt = new WaterfallStep[]
            {
                SaveCreateEventDialogState,
                AddMoreUserPrompt,
                AfterAddMoreUserPrompt
            };

            AddDialog(new CalendarWaterfallDialog(Actions.ConfirmNameList, confirmNameList, CalendarStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new CalendarWaterfallDialog(Actions.LoopNameList, loopNameList, CalendarStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new CalendarWaterfallDialog(Actions.ConfirmAttendee, confirmAttendee, CalendarStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new CalendarWaterfallDialog(Actions.UpdateName, updateName, CalendarStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new CalendarWaterfallDialog(Actions.SelectPerson, selectPerson, CalendarStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new CalendarWaterfallDialog(Actions.SelectEmail, selectEmail, CalendarStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new CalendarWaterfallDialog(Actions.AddMoreUserPrompt, addMoreUserPrompt, CalendarStateAccessor) { TelemetryClient = telemetryClient });
            InitialDialogId = Actions.ConfirmNameList;
        }

        public async Task<DialogTurnResult> ConfirmNameList(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];
                var options = sc.Options as FindContactDialogOptions;

                // got attendee name list already.
                if (dialogState.FindContactInfor.ContactsNameList.Any())
                {
                    if (options != null && options.FindContactReason == FindContactDialogOptions.FindContactReasonType.FirstFindContact)
                    {
                        if (dialogState.FindContactInfor.ContactsNameList.Count > 1)
                        {
                            options.PromptMoreContact = false;
                        }
                    }

                    return await sc.NextAsync();
                }

                // ask for attendee
                if (options.FindContactReason == FindContactDialogOptions.FindContactReasonType.FirstFindContact)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(FindContactResponses.NoAttendees) }, cancellationToken);
                }
                else
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(FindContactResponses.AddMoreAttendees) }, cancellationToken);
                }
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
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];
                var skillOptions = (FindContactDialogOptions)sc.Options;

                // get name list from sc.result
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                    // if is skip. set the name list to be myself only.
                    if (CreateEventWhiteList.IsSkip(userInput))
                    {
                        dialogState.FindContactInfor.ContactsNameList = new List<string>
                        {
                            CalendarCommonStrings.MyselfConst
                        };
                    }
                    else
                    if (userState.EventSource != EventSource.Other)
                    {
                        if (userInput != null)
                        {
                            var nameList = userInput.Split(CreateEventWhiteList.GetContactNameSeparator(), StringSplitOptions.None)
                                .Select(x => x.Trim())
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .ToList();
                            dialogState.FindContactInfor.ContactsNameList = nameList;
                        }
                    }
                }

                if (dialogState.FindContactInfor.ContactsNameList.Any())
                {
                    if (dialogState.FindContactInfor.ContactsNameList.Count > 1)
                    {
                        var nameString = await GetReadyToSendNameListStringAsync(sc);
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindContactResponses.BeforeSendingMessage, new StringDictionary() { { "NameList", nameString } }));
                    }

                    // go to loop to go through all the names
                    dialogState.FindContactInfor.ConfirmContactsNameIndex = 0;

                    skillOptions.DialogState = dialogState;
                    return await sc.ReplaceDialogAsync(Actions.LoopNameList, sc.Options, cancellationToken);
                }

                dialogState.FindContactInfor.ContactsNameList = new List<string>();
                dialogState.FindContactInfor.CurrentContactName = string.Empty;
                dialogState.FindContactInfor.ConfirmContactsNameIndex = 0;

                var returnOptions = sc.Options as FindContactDialogOptions;
                returnOptions.DialogState = dialogState;
                return await sc.EndDialogAsync(returnOptions);
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
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (dialogState.FindContactInfor.ConfirmContactsNameIndex < dialogState.FindContactInfor.ContactsNameList.Count)
                {
                    dialogState.FindContactInfor.CurrentContactName = dialogState.FindContactInfor.ContactsNameList[dialogState.FindContactInfor.ConfirmContactsNameIndex];
                    var options = sc.Options as FindContactDialogOptions;
                    options.UpdateUserNameReason = FindContactDialogOptions.UpdateUserNameReasonType.Initialize;
                    options.DialogState = dialogState;
                    return await sc.BeginDialogAsync(Actions.ConfirmAttendee, sc.Options, cancellationToken);
                }
                else
                {
                    dialogState.FindContactInfor.ContactsNameList = new List<string>();
                    dialogState.FindContactInfor.CurrentContactName = string.Empty;
                    dialogState.FindContactInfor.ConfirmContactsNameIndex = 0;
                    var options = sc.Options as FindContactDialogOptions;
                    if (options.PromptMoreContact && dialogState.FindContactInfor.Contacts.Count < 20)
                    {
                        options.DialogState = dialogState;
                        return await sc.ReplaceDialogAsync(Actions.AddMoreUserPrompt, options);
                    }
                    else
                    {
                        options.DialogState = dialogState;
                        return await sc.EndDialogAsync(options);
                    }
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
                if (sc.Result != null && sc.Result is FindContactDialogOptions)
                {
                    var result = (FindContactDialogOptions)sc.Result;
                    sc.State.Dialog[CalendarStateKey] = result.DialogState;
                }

                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];
                var skillOptions = (FindContactDialogOptions)sc.Options;

                dialogState.FindContactInfor.ConfirmContactsNameIndex = dialogState.FindContactInfor.ConfirmContactsNameIndex + 1;
                dialogState.FindContactInfor.ConfirmedContact = null;
                skillOptions.DialogState = dialogState;
                return await sc.ReplaceDialogAsync(Actions.LoopNameList, skillOptions, cancellationToken);
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
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                // when called bt LoopNameList, the options reason is initialize.
                // when replaced by itself, the reason will be Confirm No.
                var skillOptions = (FindContactDialogOptions)sc.Options;

                // set the ConfirmPerson to null as defaut.
                dialogState.FindContactInfor.ConfirmedContact = null;
                skillOptions.DialogState = dialogState;
                return await sc.BeginDialogAsync(Actions.UpdateName, skillOptions, cancellationToken: cancellationToken);
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
            var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];
            var options = (FindContactDialogOptions)sc.Options;

            var confirmedPerson = dialogState.FindContactInfor.ConfirmedContact;
            if (confirmedPerson == null)
            {
                options.DialogState = dialogState;
                return await sc.EndDialogAsync(options);
            }

            var name = confirmedPerson.DisplayName;
            if (confirmedPerson.Emails.Count() == 1)
            {
                // Highest probability
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = ResponseManager.GetResponse(FindContactResponses.PromptOneNameOneAddress, new StringDictionary() { { "UserName", name }, { "EmailAddress", confirmedPerson.Emails.First().Address ?? confirmedPerson.UserPrincipalName } }), });
            }
            else
            {
                options.DialogState = dialogState;
                return await sc.BeginDialogAsync(Actions.SelectEmail, options, cancellationToken);
            }
        }

        public async Task<DialogTurnResult> AfterConfirmEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];
                var options = (FindContactDialogOptions)sc.Options;

                var confirmedPerson = dialogState.FindContactInfor.ConfirmedContact;
                var name = confirmedPerson.DisplayName;

                // it will be new retry whether the user set this attendee down or choose to retry on this one.
                dialogState.FindContactInfor.FirstRetryInFindContact = true;

                if (!(sc.Result is bool) || (bool)sc.Result)
                {
                    var attendee = new EventModel.Attendee
                    {
                        DisplayName = name,
                        Address = confirmedPerson.Emails.First().Address
                    };
                    if (dialogState.FindContactInfor.Contacts.All(r => r.Address != attendee.Address))
                    {
                        dialogState.FindContactInfor.Contacts.Add(attendee);
                    }

                    options.DialogState = dialogState;
                    return await sc.EndDialogAsync(options);
                }
                else
                {
                    options.UpdateUserNameReason = FindContactDialogOptions.UpdateUserNameReasonType.ConfirmNo;
                    options.DialogState = dialogState;
                    return await sc.ReplaceDialogAsync(Actions.ConfirmAttendee, options);
                }
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
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                dialogState.FindContactInfor.UnconfirmedContact.Clear();
                dialogState.FindContactInfor.ConfirmedContact = null;
                var options = (FindContactDialogOptions)sc.Options;

                // if it is confirm no, thenask user to give a new attendee
                if (options.UpdateUserNameReason == FindContactDialogOptions.UpdateUserNameReasonType.ConfirmNo)
                {
                    return await sc.PromptAsync(
                        Actions.Prompt,
                        new PromptOptions
                        {
                            Prompt = ResponseManager.GetResponse(CreateEventResponses.NoAttendees)
                        });
                }

                var currentRecipientName = dialogState.FindContactInfor.CurrentContactName;

                // if not initialize ask user for attendee
                if (options.UpdateUserNameReason != FindContactDialogOptions.UpdateUserNameReasonType.Initialize)
                {
                    if (dialogState.FindContactInfor.FirstRetryInFindContact)
                    {
                        dialogState.FindContactInfor.FirstRetryInFindContact = false;
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
                            { "source", userState.EventSource == Models.EventSource.Microsoft ? "Outlook" : "Gmail" },
                            { "UserName", currentRecipientName }
                            }));
                        dialogState.FindContactInfor.FirstRetryInFindContact = true;
                        dialogState.FindContactInfor.CurrentContactName = string.Empty;
                        options.DialogState = dialogState;
                        return await sc.EndDialogAsync(options);
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
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];
                var options = (FindContactDialogOptions)sc.Options;

                if (string.IsNullOrEmpty(userInput) && options.UpdateUserNameReason != FindContactDialogOptions.UpdateUserNameReasonType.Initialize)
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindContactResponses.UserNotFoundAgain, new StringDictionary() { { "source", userState.EventSource == EventSource.Microsoft ? "Outlook Calendar" : "Google Calendar" } }));

                    options.DialogState = dialogState;
                    return await sc.EndDialogAsync(options);
                }

                var currentRecipientName = string.IsNullOrEmpty(userInput) ? dialogState.FindContactInfor.CurrentContactName : userInput;
                dialogState.FindContactInfor.CurrentContactName = currentRecipientName;

                // if it's an email, add to attendee and kepp the dialogState.FindContactInfor.ConfirmedContact null
                if (!string.IsNullOrEmpty(currentRecipientName) && IsEmail(currentRecipientName))
                {
                    var attendee = new EventModel.Attendee
                    {
                        DisplayName = currentRecipientName,
                        Address = currentRecipientName
                    };
                    if (dialogState.FindContactInfor.Contacts.All(r => r.Address != attendee.Address))
                    {
                        dialogState.FindContactInfor.Contacts.Add(attendee);
                    }

                    dialogState.FindContactInfor.CurrentContactName = string.Empty;
                    dialogState.FindContactInfor.ConfirmedContact = null;

                    options.DialogState = dialogState;
                    return await sc.EndDialogAsync(options);
                }

                var unionList = new List<CustomizedPerson>();

                if (CreateEventWhiteList.GetMyself(currentRecipientName))
                {
                    var me = await GetMe(sc.Context);
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

                dialogState.FindContactInfor.UnconfirmedContact = unionList;

                if (unionList.Count == 0)
                {
                    options.UpdateUserNameReason = FindContactDialogOptions.UpdateUserNameReasonType.NotFound;
                    options.DialogState = dialogState;
                    return await sc.ReplaceDialogAsync(Actions.UpdateName, options);
                }
                else
                if (unionList.Count == 1)
                {
                    dialogState.FindContactInfor.ConfirmedContact = unionList.First();
                    return await sc.EndDialogAsync();
                }
                else
                {
                    options.DialogState = dialogState;
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
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];
                var unionList = dialogState.FindContactInfor.UnconfirmedContact;
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
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];
                var skillOptions = (FindContactDialogOptions)sc.Options;

                var luisResult = userState.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;
                var generlLuisResult = userState.GeneralLuisResult;
                var generalTopIntent = generlLuisResult?.TopIntent().intent;
                generalTopIntent = MergeShowIntent(generalTopIntent, topIntent, luisResult);

                if (sc.Result == null)
                {
                    if (generalTopIntent == General.Intent.ShowNext)
                    {
                        dialogState.FindContactInfor.ShowContactsIndex++;
                    }
                    else if (generalTopIntent == General.Intent.ShowPrevious)
                    {
                        if (dialogState.FindContactInfor.ShowContactsIndex > 0)
                        {
                            dialogState.FindContactInfor.ShowContactsIndex--;
                        }
                        else
                        {
                            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindContactResponses.AlreadyFirstPage));
                        }
                    }
                    else
                    {
                        // result is null when just update the recipient name. show recipients page should be reset.
                        dialogState.FindContactInfor.ShowContactsIndex = 0;
                    }

                    skillOptions.DialogState = dialogState;
                    return await sc.ReplaceDialogAsync(Actions.SelectPerson, skillOptions, cancellationToken: cancellationToken);
                }

                var choiceResult = (sc.Result as FoundChoice)?.Value.Trim('*');
                if (choiceResult != null)
                {
                    // Clean up data
                    dialogState.FindContactInfor.ShowContactsIndex = 0;

                    // Start to confirm the email
                    var confirmedPerson = dialogState.FindContactInfor.UnconfirmedContact.Where(p => p.DisplayName.ToLower() == choiceResult.ToLower()).First();
                    dialogState.FindContactInfor.ConfirmedContact = confirmedPerson;
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
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                var confirmedPerson = dialogState.FindContactInfor.ConfirmedContact;
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
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];
                var skillOptions = (FindContactDialogOptions)sc.Options;

                var luisResult = userState.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;
                var generlLuisResult = userState.GeneralLuisResult;
                var generalTopIntent = generlLuisResult?.TopIntent().intent;
                generalTopIntent = MergeShowIntent(generalTopIntent, topIntent, luisResult);

                if (sc.Result == null)
                {
                    if (generalTopIntent == General.Intent.ShowNext)
                    {
                        dialogState.FindContactInfor.ShowContactsIndex++;
                    }
                    else if (generalTopIntent == General.Intent.ShowPrevious)
                    {
                        if (dialogState.FindContactInfor.ShowContactsIndex > 0)
                        {
                            dialogState.FindContactInfor.ShowContactsIndex--;
                        }
                        else
                        {
                            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindContactResponses.AlreadyFirstPage));
                        }
                    }
                    else
                    {
                        // result is null when just update the recipient name. show recipients page should be reset.
                        dialogState.FindContactInfor.ShowContactsIndex = 0;
                    }

                    skillOptions.DialogState = dialogState;
                    return await sc.ReplaceDialogAsync(Actions.SelectEmail, skillOptions, cancellationToken);
                }

                var choiceResult = (sc.Result as FoundChoice)?.Value.Trim('*');
                if (choiceResult != null)
                {
                    dialogState.FindContactInfor.ConfirmedContact.DisplayName = choiceResult.Split(": ")[0];
                    dialogState.FindContactInfor.ConfirmedContact.Emails.First().Address = choiceResult.Split(": ")[1];

                    // Clean up data
                    dialogState.FindContactInfor.ShowContactsIndex = 0;
                }

                skillOptions.DialogState = dialogState;
                return await sc.EndDialogAsync(skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AddMoreUserPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = ResponseManager.GetResponse(FindContactResponses.AddMoreUserPrompt, new StringDictionary() { { "Users", dialogState.FindContactInfor.Contacts.ToSpeechString(CommonStrings.And, li => $"{li.DisplayName ?? li.Address}: {li.Address}") } }),
                    RetryPrompt = ResponseManager.GetResponse(FindContactResponses.AddMoreUserPrompt, new StringDictionary() { { "Users", dialogState.FindContactInfor.Contacts.ToSpeechString(CommonStrings.And, li => $"{li.DisplayName ?? li.Address}: {li.Address}") } })
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterAddMoreUserPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (FindContactDialogOptions)sc.Options;
                var state = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];
                var result = (bool)sc.Result;
                skillOptions.DialogState = state;
                if (result)
                {
                    skillOptions.FindContactReason = FindContactDialogOptions.FindContactReasonType.FindContactAgain;
                    return await sc.ReplaceDialogAsync(Actions.ConfirmNameList, skillOptions);
                }
                else
                {
                    return await sc.EndDialogAsync(skillOptions);
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
            var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

            var pageIndex = dialogState.FindContactInfor.ShowContactsIndex;
            var pageSize = 3;
            var skip = pageSize * pageIndex;
            var emailList = confirmedPerson.Emails.ToList();

            // Go back to the last page when reaching the end.
            if (skip >= emailList.Count && pageIndex > 0)
            {
                dialogState.FindContactInfor.ShowContactsIndex--;
                pageIndex = dialogState.FindContactInfor.ShowContactsIndex;
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
                        options.Prompt.Speak = SpeechUtility.ListToSpeechReadyString(options, ReadPreference.Chronological, ConfigData.GetInstance().MaxReadSize);
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

            options.Prompt.Speak = SpeechUtility.ListToSpeechReadyString(options, ReadPreference.Chronological, ConfigData.GetInstance().MaxReadSize);
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
            var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

            var pageIndex = dialogState.FindContactInfor.ShowContactsIndex;
            var pageSize = 3;
            var skip = pageSize * pageIndex;
            var currentRecipientName = dialogState.FindContactInfor.CurrentContactName;

            // Go back to the last page when reaching the end.
            if (skip >= unionList.Count && pageIndex > 0)
            {
                dialogState.FindContactInfor.ShowContactsIndex--;
                pageIndex = dialogState.FindContactInfor.ShowContactsIndex;
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
                        options.Prompt.Speak = SpeechUtility.ListToSpeechReadyString(options, ReadPreference.Chronological, ConfigData.GetInstance().MaxReadSize);
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

            options.Prompt.Speak = SpeechUtility.ListToSpeechReadyString(options, ReadPreference.Chronological, ConfigData.GetInstance().MaxReadSize);
            options.Prompt.Text = GetSelectPromptString(options, true);
            options.RetryPrompt = ResponseManager.GetResponse(CalendarSharedResponses.DidntUnderstandMessage);
            return options;
        }

        private async Task<DialogTurnResult> InitFindContactDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = new CreateEventDialogState();

                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = Services.CognitiveModelSets[locale];

                // Update state with email luis result and entities --- todo: use luis result in adaptive dialog
                var luisResult = await localeConfig.LuisServices["calendar"].RecognizeAsync<CalendarLuis>(sc.Context);
                userState.LuisResult = luisResult;
                localeConfig.LuisServices.TryGetValue("general", out var luisService);
                var generalLuisResult = await luisService.RecognizeAsync<General>(sc.Context);
                userState.GeneralLuisResult = generalLuisResult;

                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (skillOptions != null && skillOptions.SubFlowMode)
                {
                    dialogState = skillOptions?.DialogState != null ? new CreateEventDialogState(skillOptions?.DialogState) : dialogState;
                }

                var newState = await DigestFindContactLuisResult(sc, userState.LuisResult, userState.GeneralLuisResult, dialogState, true);
                sc.State.Dialog.Add(CalendarStateKey, newState);

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> SaveCreateEventDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var dialogState = skillOptions?.DialogState != null ? skillOptions?.DialogState : new CreateEventDialogState();

                if (skillOptions != null && skillOptions.DialogState != null)
                {
                    if (skillOptions.DialogState is CreateEventDialogState)
                    {
                        dialogState = (CreateEventDialogState)skillOptions.DialogState;
                    }
                    else
                    {
                        dialogState = skillOptions.DialogState != null ? new CreateEventDialogState(skillOptions.DialogState) : dialogState;
                    }
                }

                var userState = await CalendarStateAccessor.GetAsync(sc.Context);

                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = Services.CognitiveModelSets[locale];

                // Update state with email luis result and entities --- todo: use luis result in adaptive dialog
                var luisResult = await localeConfig.LuisServices["calendar"].RecognizeAsync<CalendarLuis>(sc.Context);
                userState.LuisResult = luisResult;
                localeConfig.LuisServices.TryGetValue("general", out var luisService);
                var generalLuisResult = await luisService.RecognizeAsync<General>(sc.Context);
                userState.GeneralLuisResult = generalLuisResult;

                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                var newState = await DigestFindContactLuisResult(sc, userState.LuisResult, userState.GeneralLuisResult, dialogState as CreateEventDialogState, false);
                sc.State.Dialog.Add(CalendarStateKey, newState);

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<CreateEventDialogState> DigestFindContactLuisResult(DialogContext dc, CalendarLuis luisResult, General generalLuisResult, CreateEventDialogState state, bool isBeginDialog)
        {
            try
            {
                var userState = await CalendarStateAccessor.GetAsync(dc.Context);

                var intent = luisResult.TopIntent().intent;

                var entity = luisResult.Entities;

                if (!isBeginDialog)
                {
                    return state;
                }

                return state;
            }
            catch
            {
                await ClearAllState(dc.Context);
                await dc.CancelAllDialogsAsync();
                throw;
            }
        }
    }
}