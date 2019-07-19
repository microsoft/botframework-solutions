using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Models.DialogModel;
using EmailSkill.Responses.FindContact;
using EmailSkill.Services;
using EmailSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;
using Microsoft.Graph;
using Microsoft.Recognizers.Text;

namespace EmailSkill.Dialogs
{
    public class FindContactDialog : ComponentDialog
    {
        public static readonly int MaxAcceptContactsNum = 20;
        protected const string EmailStateKey = "EmailState";

        private ResourceMultiLanguageGenerator _lgMultiLangEngine;

        public FindContactDialog(
             BotSettings settings,
             BotServices services,
             ResponseManager responseManager,
             ConversationState conversationState,
             IServiceManager serviceManager,
             IBotTelemetryClient telemetryClient)
             : base(nameof(FindContactDialog))
        {
            TelemetryClient = telemetryClient;
            Services = services;
            ResponseManager = responseManager;
            Accessor = conversationState.CreateProperty<EmailSkillState>(nameof(EmailSkillState));
            DialogStateAccessor = conversationState.CreateProperty<DialogState>(nameof(DialogState));
            ServiceManager = serviceManager;
            TelemetryClient = telemetryClient;

            _lgMultiLangEngine = new ResourceMultiLanguageGenerator("FindContact.lg");

            // entry, get the name list
            var confirmNameList = new WaterfallStep[]
            {
                InitDialogState,
                ConfirmNameList,
                AfterConfirmNameList,
            };

            // go through the name list, replace the confirmNameList
            // set state.CurrentAttendeeName
            var loopNameList = new WaterfallStep[]
            {
                SaveDialogState,
                LoopNameList,
                AfterLoopNameList
            };

            // called by loopNameList
            var confirmContacts = new WaterfallStep[]
            {
                SaveDialogState,

                // call updateName to get the person state.ConfirmedPerson.
                // state.ConfirmedPerson should be set after this step
                ConfirmName,

                // check if the state.ConfirmedPerson
                //  - null : failed to parse this name for multiple try.
                //  - one email : check if this one is wanted
                //  - multiple emails : call selectEmail
                ConfirmEmail,

                // if got no on last step, replace/restart this flow.
                AfterConfirmEmail
            };

            // use the user name of state.CurrentAttendeeName or user input to find the persons.
            // and will call select person.
            // after all this done, state.ConfirmedPerson should be set.
            var updateName = new WaterfallStep[]
            {
                SaveDialogState,

                // check whether should the bot ask for attendee name.
                // if called by confirmAttendee then skip this step.
                // if called by itself when can not find the last input, it will ask back or end this one when multiple try.
                UpdateUserName,

                // check if email. add email direct into attendee and set state.ConfirmedPerson null.
                // if not, search for the attendee.
                // if got multiple persons, call selectPerson. use replace
                // if got no person, replace/restart this flow.
                AfterUpdateUserName,
            };

            // select person, called bt updateName with replace.
            var selectPerson = new WaterfallStep[]
            {
                SaveDialogState,
                SelectPerson,
                AfterSelectPerson
            };

            // select email.
            // called by ConfirmEmail
            var selectEmail = new WaterfallStep[]
            {
                SaveDialogState,
                SelectEmail,
                AfterSelectEmail
            };

            var addMoreContactsPrompt = new WaterfallStep[]
            {
                SaveDialogState,
                AddMoreUserPrompt,
                AfterAddMoreUserPrompt
            };

            AddDialog(new TextPrompt(FindContactAction.Prompt));
            AddDialog(new ConfirmPrompt(FindContactAction.TakeFurtherAction, null, Culture.English) { Style = ListStyle.SuggestedAction });
            AddDialog(new EmailWaterfallDialog(FindContactAction.ConfirmNameList, confirmNameList, Accessor) { TelemetryClient = telemetryClient });
            AddDialog(new EmailWaterfallDialog(FindContactAction.LoopNameList, loopNameList, Accessor) { TelemetryClient = telemetryClient });
            AddDialog(new EmailWaterfallDialog(FindContactAction.ConfirmAttendee, confirmContacts, Accessor) { TelemetryClient = telemetryClient });
            AddDialog(new EmailWaterfallDialog(FindContactAction.UpdateName, updateName, Accessor) { TelemetryClient = telemetryClient });
            AddDialog(new EmailWaterfallDialog(FindContactAction.SelectPerson, selectPerson, Accessor) { TelemetryClient = telemetryClient });
            AddDialog(new EmailWaterfallDialog(FindContactAction.SelectEmail, selectEmail, Accessor) { TelemetryClient = telemetryClient });
            AddDialog(new ChoicePrompt(FindContactAction.Choice, ChoiceValidator, Culture.English) { Style = ListStyle.None, });
            AddDialog(new EmailWaterfallDialog(FindContactAction.AddMoreContactsPrompt, addMoreContactsPrompt, Accessor) { TelemetryClient = telemetryClient });
            InitialDialogId = FindContactAction.ConfirmNameList;
        }

        protected BotSettings Settings { get; set; }

        protected BotServices Services { get; set; }

        protected IStatePropertyAccessor<EmailSkillState> Accessor { get; set; }

        protected IStatePropertyAccessor<DialogState> DialogStateAccessor { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected ResponseManager ResponseManager { get; set; }

        protected virtual async Task<DialogTurnResult> InitDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (FindContactDialogOptions)sc.Options;
                var userState = await Accessor.GetAsync(sc.Context);
                var dialogState = new SendEmailDialogState();

                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = Services.CognitiveModelSets[locale];

                // Update state with email luis result and entities --- todo: use luis result in adaptive dialog
                var luisResult = await localeConfig.LuisServices["email"].RecognizeAsync<emailLuis>(sc.Context);
                userState.LuisResult = luisResult;
                localeConfig.LuisServices.TryGetValue("general", out var luisService);
                var generalLuisResult = await luisService.RecognizeAsync<General>(sc.Context);
                userState.GeneralLuisResult = generalLuisResult;

                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (skillOptions != null)
                {
                    dialogState = skillOptions?.DialogState != null ? new SendEmailDialogState(skillOptions?.DialogState) : dialogState;
                }

                var newState = DigestLuisResult(sc, userState.LuisResult, userState.GeneralLuisResult, dialogState, true);
                sc.State.Dialog.Add(EmailStateKey, newState);

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> SaveDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var skillOptions = (FindContactDialogOptions)sc.Options;
            var dialogState = skillOptions?.DialogState != null ? (SendEmailDialogState)skillOptions?.DialogState : new SendEmailDialogState();

            var state = await Accessor.GetAsync(sc.Context);

            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = Services.CognitiveModelSets[locale];

            // Update state with email luis result and entities --- todo: use luis result in adaptive dialog
            var emailLuisResult = await localeConfig.LuisServices["email"].RecognizeAsync<emailLuis>(sc.Context);
            state.LuisResult = emailLuisResult;
            localeConfig.LuisServices.TryGetValue("general", out var luisService);
            var luisResult = await luisService.RecognizeAsync<General>(sc.Context);
            state.GeneralLuisResult = luisResult;

            var newState = DigestLuisResult(sc, state.LuisResult, state.GeneralLuisResult, dialogState, true);
            sc.State.Dialog.Add(EmailStateKey, newState);

            return await sc.NextAsync();
        }

        public async Task<DialogTurnResult> ConfirmNameList(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                var options = sc.Options as FindContactDialogOptions;

                // got attendee name list already.
                if (state.FindContactInfor.ContactsNameList.Any())
                {
                    if (options != null && options.FindContactReason == FindContactDialogOptions.FindContactReasonType.FirstFindContact)
                    {
                        if (state.FindContactInfor.ContactsNameList.Count > 1)
                        {
                            options.PromptMoreContact = false;
                        }
                    }

                    return await sc.NextAsync();
                }

                // ask for attendee
                if (options.FindContactReason == FindContactDialogOptions.FindContactReasonType.FirstFindContact)
                {
                    var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[NoRecipients]", null);
                    return await sc.PromptAsync(FindContactAction.Prompt, new PromptOptions { Prompt = activity as Activity }, cancellationToken);
                }
                else
                {
                    var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[AddMoreContacts]", null);
                    return await sc.PromptAsync(FindContactAction.Prompt, new PromptOptions { Prompt = activity as Activity }, cancellationToken);
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                var userState = await Accessor.GetAsync(sc.Context);

                // get name list from sc.result
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                    if (userState.MailSourceType != MailSource.Other)
                    {
                        if (userInput != null)
                        {
                            var nameList = userInput.Split(EmailCommonPhrase.GetContactNameSeparator(), StringSplitOptions.None)
                                .Select(x => x.Trim())
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .ToList();
                            state.FindContactInfor.ContactsNameList = nameList;
                        }
                    }
                }

                if (state.FindContactInfor.ContactsNameList.Any())
                {
                    if (state.FindContactInfor.ContactsNameList.Count > 1)
                    {
                        var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[BeforeSendingMessage]", new { nameList = await GetNameListStringAsync(sc) });
                        await sc.Context.SendActivityAsync(activity);
                    }

                    // go to loop to go through all the names
                    state.FindContactInfor.ConfirmContactsNameIndex = 0;
                    var options = sc.Options as FindContactDialogOptions;
                    options.DialogState = state;

                    return await sc.ReplaceDialogAsync(FindContactAction.LoopNameList, options, cancellationToken);
                }

                state.FindContactInfor.ContactsNameList = new List<string>();
                state.FindContactInfor.CurrentContactName = string.Empty;
                state.FindContactInfor.ConfirmContactsNameIndex = 0;

                var returnOptions = sc.Options as FindContactDialogOptions;
                returnOptions.DialogState = state;
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                if (state.FindContactInfor.ConfirmContactsNameIndex < state.FindContactInfor.ContactsNameList.Count)
                {
                    state.FindContactInfor.CurrentContactName = state.FindContactInfor.ContactsNameList[state.FindContactInfor.ConfirmContactsNameIndex];

                    var options = sc.Options as FindContactDialogOptions;
                    options.UpdateUserNameReason = FindContactDialogOptions.UpdateUserNameReasonType.Initialize;
                    return await sc.BeginDialogAsync(FindContactAction.ConfirmAttendee, sc.Options, cancellationToken);
                }
                else
                {
                    state.FindContactInfor.ContactsNameList = new List<string>();
                    state.FindContactInfor.CurrentContactName = string.Empty;
                    state.FindContactInfor.ConfirmContactsNameIndex = 0;
                    var options = sc.Options as FindContactDialogOptions;
                    options.DialogState = state;
                    if (options.PromptMoreContact && state.FindContactInfor.Contacts.Count < MaxAcceptContactsNum)
                    {
                        return await sc.ReplaceDialogAsync(FindContactAction.AddMoreContactsPrompt, options);
                    }
                    else
                    {
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                if (sc.Result is FindContactDialogOptions)
                {
                    var option = (FindContactDialogOptions)sc.Result;
                    state = (SendEmailDialogState)option.DialogState;
                }

                state.FindContactInfor.ConfirmContactsNameIndex = state.FindContactInfor.ConfirmContactsNameIndex + 1;
                state.FindContactInfor.ConfirmedContact = null;

                var options = sc.Options as FindContactDialogOptions;
                options.DialogState = state;
                return await sc.ReplaceDialogAsync(FindContactAction.LoopNameList, options, cancellationToken);
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];

                // set the ConfirmPerson to null as defaut.
                state.FindContactInfor.ConfirmedContact = null;

                // when called bt LoopNameList, the options reason is initialize.
                // when replaced by itself, the reason will be Confirm No.
                var options = (FindContactDialogOptions)sc.Options;
                options.DialogState = state;
                return await sc.BeginDialogAsync(FindContactAction.UpdateName, options: options, cancellationToken: cancellationToken);
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
            var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
            var confirmedPerson = state.FindContactInfor.ConfirmedContact;
            var options = sc.Options as FindContactDialogOptions;
            if (confirmedPerson == null)
            {
                options.DialogState = state;
                return await sc.EndDialogAsync(options);
            }

            var name = confirmedPerson.DisplayName;
            if (confirmedPerson.Emails.Count() == 1)
            {
                // Highest probability
                var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[PromptOneNameOneAddress]", new { userName = name, emailAddress = confirmedPerson.Emails.First() ?? confirmedPerson.UserPrincipalName });
                return await sc.PromptAsync(FindContactAction.TakeFurtherAction, new PromptOptions { Prompt = activity as Activity });
            }
            else
            {
                options.DialogState = state;
                return await sc.BeginDialogAsync(FindContactAction.SelectEmail, options: options, cancellationToken: cancellationToken);
            }
        }

        public async Task<DialogTurnResult> AfterConfirmEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                if (sc.Result is FindContactDialogOptions)
                {
                    var option = (FindContactDialogOptions)sc.Result;
                    state = (SendEmailDialogState)option.DialogState;
                }
                var confirmedPerson = state.FindContactInfor.ConfirmedContact;
                var name = confirmedPerson.DisplayName;
                var options = sc.Options as FindContactDialogOptions;

                // it will be new retry whether the user set this attendee down or choose to retry on this one.
                state.FindContactInfor.FirstRetryInFindContact = true;

                if (!(sc.Result is bool) || (bool)sc.Result)
                {
                    var attendee = new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Name = name,
                            Address = confirmedPerson.Emails.First()
                        }
                    };
                    if (state.FindContactInfor.Contacts.All(r => r.EmailAddress.Address != attendee.EmailAddress.Address))
                    {
                        state.FindContactInfor.Contacts.Add(attendee);
                    }

                    options.DialogState = state;
                    return await sc.EndDialogAsync(options);
                }
                else
                {
                    options.UpdateUserNameReason = FindContactDialogOptions.UpdateUserNameReasonType.ConfirmNo;
                    options.DialogState = state;
                    return await sc.ReplaceDialogAsync(FindContactAction.ConfirmAttendee, options);
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                if (sc.Result is FindContactDialogOptions)
                {
                    var option = (FindContactDialogOptions)sc.Result;
                    state = (SendEmailDialogState)option.DialogState;
                }
                var userState = await Accessor.GetAsync(sc.Context);
                state.FindContactInfor.UnconfirmedContact.Clear();
                state.FindContactInfor.ConfirmedContact = null;
                var options = (FindContactDialogOptions)sc.Options;

                // if it is confirm no, then ask user to give a new attendee
                if (options.UpdateUserNameReason == FindContactDialogOptions.UpdateUserNameReasonType.ConfirmNo)
                {
                    var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[NoRecipients]", null);
                    return await sc.PromptAsync(FindContactAction.Prompt, new PromptOptions { Prompt = activity as Activity });
                }

                var currentRecipientName = state.FindContactInfor.CurrentContactName;

                // if not initialize ask user for attendee
                if (options.UpdateUserNameReason != FindContactDialogOptions.UpdateUserNameReasonType.Initialize)
                {
                    if (state.FindContactInfor.FirstRetryInFindContact)
                    {
                        state.FindContactInfor.FirstRetryInFindContact = false;

                        var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[UserNotFound]", new { userName = currentRecipientName });
                        return await sc.PromptAsync(FindContactAction.Prompt, new PromptOptions { Prompt = activity as Activity });
                    }
                    else
                    {
                        var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[UserNotFoundAgain]", new { userName = currentRecipientName, source = userState.MailSourceType == MailSource.Microsoft ? "Outlook" : "Gmail" });
                        await sc.Context.SendActivityAsync(activity);

                        state.FindContactInfor.FirstRetryInFindContact = true;
                        state.FindContactInfor.CurrentContactName = string.Empty;

                        options.DialogState = state;
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
                var userState = await Accessor.GetAsync(sc.Context);
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                if (sc.Result is FindContactDialogOptions)
                {
                    var option = (FindContactDialogOptions)sc.Result;
                    state = (SendEmailDialogState)option.DialogState;
                }

                var options = (FindContactDialogOptions)sc.Options;

                if (string.IsNullOrEmpty(userInput) && options.UpdateUserNameReason != FindContactDialogOptions.UpdateUserNameReasonType.Initialize)
                {
                    var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[UserNotFoundAgain]", new { userName = userInput, source = userState.MailSourceType == MailSource.Microsoft ? "Outlook" : "Gmail" });
                    await sc.Context.SendActivityAsync(activity);
                    options.DialogState = state;
                    return await sc.EndDialogAsync(options);
                }

                var currentRecipientName = string.IsNullOrEmpty(userInput) ? state.FindContactInfor.CurrentContactName : userInput;
                state.FindContactInfor.CurrentContactName = currentRecipientName;

                // if it's an email, add to attendee and keep the state.ConfirmedPerson null
                if (!string.IsNullOrEmpty(currentRecipientName) && Utilities.Util.IsEmail(currentRecipientName))
                {
                    var attendee = new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Name = currentRecipientName,
                            Address = currentRecipientName
                        }
                    };
                    if (state.FindContactInfor.Contacts.All(r => r.EmailAddress.Address != attendee.EmailAddress.Address))
                    {
                        state.FindContactInfor.Contacts.Add(attendee);
                    }

                    state.FindContactInfor.CurrentContactName = string.Empty;
                    state.FindContactInfor.ConfirmedContact = null;
                    options.DialogState = state;
                    return await sc.EndDialogAsync(options);
                }

                var unionList = new List<PersonModel>();

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
                    unionList.Add(personList.First());
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
                                unionList.Add(personWithSameName.First());
                            }
                            else
                            {
                                var unionPerson = personWithSameName.FirstOrDefault();
                                var curEmailList = new List<string>();
                                foreach (var sameNamePerson in personWithSameName)
                                {
                                    sameNamePerson.Emails.ToList().ForEach(e =>
                                    {
                                        if (!string.IsNullOrEmpty(e))
                                        {
                                            curEmailList.Add(e);
                                        }
                                    });
                                }

                                unionPerson.Emails = curEmailList;
                                unionList.Add(unionPerson);
                            }
                        }
                    }
                }

                unionList.RemoveAll(person => !person.Emails.Exists(email => email != null));
                unionList.RemoveAll(person => !person.Emails.Any());

                state.FindContactInfor.UnconfirmedContact = unionList;

                if (unionList.Count == 0)
                {
                    options.UpdateUserNameReason = FindContactDialogOptions.UpdateUserNameReasonType.NotFound;
                    options.DialogState = state;
                    return await sc.ReplaceDialogAsync(FindContactAction.UpdateName, options);
                }
                else
                if (unionList.Count == 1)
                {
                    state.FindContactInfor.ConfirmedContact = unionList.First();
                    options.DialogState = state;
                    return await sc.EndDialogAsync(options);
                }
                else
                {
                    options.DialogState = state;
                    return await sc.ReplaceDialogAsync(FindContactAction.SelectPerson, options, cancellationToken);
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                var unionList = state.FindContactInfor.UnconfirmedContact;
                if (unionList.Count <= ConfigData.GetInstance().MaxDisplaySize)
                {
                    return await sc.PromptAsync(FindContactAction.Choice, await GenerateOptionsForName(sc, unionList, sc.Context, true));
                }
                else
                {
                    return await sc.PromptAsync(FindContactAction.Choice, await GenerateOptionsForName(sc, unionList, sc.Context, false));
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
                var userState = await Accessor.GetAsync(sc.Context);
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                var luisResult = userState.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;
                var generlLuisResult = userState.GeneralLuisResult;
                var generalTopIntent = generlLuisResult?.TopIntent().intent;
                var options = (FindContactDialogOptions)sc.Options;

                if (sc.Result == null)
                {
                    if (generalTopIntent == General.Intent.ShowNext)
                    {
                        state.FindContactInfor.ShowContactsIndex++;
                    }
                    else if (generalTopIntent == General.Intent.ShowPrevious)
                    {
                        if (state.FindContactInfor.ShowContactsIndex > 0)
                        {
                            state.FindContactInfor.ShowContactsIndex--;
                        }
                        else
                        {
                            var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[AlreadyFirstPage]", null);
                            await sc.Context.SendActivityAsync(activity);
                        }
                    }
                    else
                    {
                        // result is null when just update the recipient name. show recipients page should be reset.
                        state.FindContactInfor.ShowContactsIndex = 0;
                    }

                    options.DialogState = state;
                    return await sc.ReplaceDialogAsync(FindContactAction.SelectPerson, options: options, cancellationToken: cancellationToken);
                }

                var choiceResult = (sc.Result as FoundChoice)?.Value.Trim('*');
                if (choiceResult != null)
                {
                    // Clean up data
                    state.FindContactInfor.ShowContactsIndex = 0;

                    // Start to confirm the email
                    var confirmedPerson = state.FindContactInfor.UnconfirmedContact.Where(p => p.DisplayName.ToLower() == choiceResult.ToLower()).First();
                    state.FindContactInfor.ConfirmedContact = confirmedPerson;
                }

                options.DialogState = state;
                return await sc.EndDialogAsync(options);
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                var confirmedPerson = state.FindContactInfor.ConfirmedContact;
                var emailString = string.Empty;
                var emailList = confirmedPerson.Emails.ToList();

                if (emailList.Count <= ConfigData.GetInstance().MaxDisplaySize)
                {
                    return await sc.PromptAsync(FindContactAction.Choice, await GenerateOptionsForEmail(sc, confirmedPerson, sc.Context, true));
                }
                else
                {
                    return await sc.PromptAsync(FindContactAction.Choice, await GenerateOptionsForEmail(sc, confirmedPerson, sc.Context, false));
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                var userState = await Accessor.GetAsync(sc.Context);
                var luisResult = userState.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;
                var generlLuisResult = userState.GeneralLuisResult;
                var generalTopIntent = generlLuisResult?.TopIntent().intent;
                var options = (FindContactDialogOptions)sc.Options;

                if (sc.Result == null)
                {
                    if (generalTopIntent == General.Intent.ShowNext)
                    {
                        state.FindContactInfor.ShowContactsIndex++;
                    }
                    else if (generalTopIntent == General.Intent.ShowPrevious)
                    {
                        if (state.FindContactInfor.ShowContactsIndex > 0)
                        {
                            state.FindContactInfor.ShowContactsIndex--;
                        }
                        else
                        {
                            var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[AlreadyFirstPage]", null);
                            await sc.Context.SendActivityAsync(activity);
                        }
                    }
                    else
                    {
                        // result is null when just update the recipient name. show recipients page should be reset.
                        state.FindContactInfor.ShowContactsIndex = 0;
                    }

                    options.DialogState = state;
                    return await sc.ReplaceDialogAsync(FindContactAction.SelectEmail, options, cancellationToken);
                }

                var choiceResult = (sc.Result as FoundChoice)?.Value.Trim('*');
                if (choiceResult != null)
                {
                    state.FindContactInfor.ConfirmedContact.DisplayName = choiceResult.Split(": ")[0];
                    state.FindContactInfor.ConfirmedContact.Emails.Add(choiceResult.Split(": ")[1]);

                    // Clean up data
                    state.FindContactInfor.ShowContactsIndex = 0;
                }

                options.DialogState = state;
                return await sc.EndDialogAsync(options);
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
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                var nameString = state.FindContactInfor.Contacts.ToSpeechString(CommonStrings.And, li => $"{li.EmailAddress.Name ?? li.EmailAddress.Name}: {li.EmailAddress.Address}");

                var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[AddMoreContactsPrompt]", new { nameList = nameString });
                return await sc.PromptAsync(FindContactAction.TakeFurtherAction, new PromptOptions { Prompt = activity as Activity }, cancellationToken);
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
                var options = (FindContactDialogOptions)sc.Options;
                var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                var result = (bool)sc.Result;
                if (result)
                {
                    options.FindContactReason = FindContactDialogOptions.FindContactReasonType.FindContactAgain;
                    options.DialogState = state;
                    return await sc.ReplaceDialogAsync(FindContactAction.ConfirmNameList, options);
                }
                else
                {
                    options.DialogState = state;
                    return await sc.EndDialogAsync(options);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected (List<PersonModel> formattedPersonList, List<PersonModel> formattedUserList) FormatRecipientList(List<PersonModel> personList, List<PersonModel> userList)
        {
            // Remove dup items
            var formattedPersonList = new List<PersonModel>();
            var formattedUserList = new List<PersonModel>();

            foreach (var person in personList)
            {
                var mailAddress = person.Emails[0] ?? person.UserPrincipalName;

                var isDup = false;
                foreach (var formattedPerson in formattedPersonList)
                {
                    var formattedMailAddress = formattedPerson.Emails[0] ?? formattedPerson.UserPrincipalName;

                    if (mailAddress.Equals(formattedMailAddress))
                    {
                        isDup = true;
                        break;
                    }
                }

                if (!isDup)
                {
                    formattedPersonList.Add(person);
                }
            }

            foreach (var user in userList)
            {
                var mailAddress = user.Emails[0] ?? user.UserPrincipalName;

                var isDup = false;
                foreach (var formattedPerson in formattedPersonList)
                {
                    var formattedMailAddress = formattedPerson.Emails[0] ?? formattedPerson.UserPrincipalName;

                    if (mailAddress.Equals(formattedMailAddress))
                    {
                        isDup = true;
                        break;
                    }
                }

                if (!isDup)
                {
                    foreach (var formattedUser in formattedUserList)
                    {
                        var formattedMailAddress = formattedUser.Emails[0] ?? formattedUser.UserPrincipalName;

                        if (mailAddress.Equals(formattedMailAddress))
                        {
                            isDup = true;
                            break;
                        }
                    }
                }

                if (!isDup)
                {
                    formattedUserList.Add(user);
                }
            }

            return (formattedPersonList, formattedUserList);
        }

        protected async Task<List<PersonModel>> GetContactsAsync(WaterfallStepContext sc, string name)
        {
            var result = new List<PersonModel>();
            var userState = await Accessor.GetAsync(sc.Context);
            var token = userState.Token;
            var service = ServiceManager.InitUserService(token, userState.GetUserTimeZone(), userState.MailSourceType);

            // Get users.
            result = await service.GetContactsAsync(name);
            return result;
        }

        protected async Task<List<PersonModel>> GetPeopleWorkWithAsync(WaterfallStepContext sc, string name)
        {
            var result = new List<PersonModel>();
            var userState = await Accessor.GetAsync(sc.Context);
            var token = userState.Token;
            var service = ServiceManager.InitUserService(token, userState.GetUserTimeZone(), userState.MailSourceType);

            // Get users.
            result = await service.GetPeopleAsync(name);

            return result;
        }

        protected async Task<List<PersonModel>> GetUserAsync(WaterfallStepContext sc, string name)
        {
            var result = new List<PersonModel>();
            var userState = await Accessor.GetAsync(sc.Context);
            var token = userState.Token;
            var service = ServiceManager.InitUserService(token, userState.GetUserTimeZone(), userState.MailSourceType);

            // Get users.
            result = await service.GetUserAsync(name);

            return result;
        }

        protected string GetSelectPromptString(PromptOptions selectOption, bool containNumbers)
        {
            var result = string.Empty;
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

        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ErrorMessage]", null);
            await sc.Context.SendActivityAsync(activity);

            // clear state
            //var state = await Accessor.GetAsync(sc.Context);
            //state.Clear();
        }

        protected async Task<bool> ChoiceValidator(PromptValidatorContext<FoundChoice> pc, CancellationToken cancellationToken)
        {
            var userState = await Accessor.GetAsync(pc.Context);
            var generalLuisResult = userState.GeneralLuisResult;
            var generalTopIntent = generalLuisResult?.TopIntent().intent;
            var emailLuisResult = userState.LuisResult;
            var emailTopIntent = emailLuisResult?.TopIntent().intent;

            // TODO: The signature for validators has changed to return bool -- Need new way to handle this logic
            // If user want to show more recipient end current choice dialog and return the intent to next step.
            if (generalTopIntent == Luis.General.Intent.ShowNext || generalTopIntent == Luis.General.Intent.ShowPrevious || emailTopIntent == emailLuis.Intent.ShowNext || emailTopIntent == emailLuis.Intent.ShowPrevious)
            {
                // pc.End(topIntent);
                return true;
            }
            else
            {
                if (!pc.Recognized.Succeeded || pc.Recognized == null)
                {
                    // do nothing when not recognized.
                }
                else
                {
                    // pc.End(pc.Recognized.Value);
                    return true;
                }
            }

            return false;
        }

        private async Task<PromptOptions> GenerateOptionsForEmail(WaterfallStepContext sc, PersonModel confirmedPerson, ITurnContext context, bool isSinglePage = true)
        {
            var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
            var pageIndex = state.FindContactInfor.ShowContactsIndex;
            var pageSize = 3;
            var skip = pageSize * pageIndex;
            var emailList = confirmedPerson.Emails.ToList();

            // Go back to the last page when reaching the end.
            if (skip >= emailList.Count && pageIndex > 0)
            {
                state.FindContactInfor.ShowContactsIndex--;
                pageIndex = state.FindContactInfor.ShowContactsIndex;
                skip = pageSize * pageIndex;

                var replyActivity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[AlreadyLastPage]", null);
                await sc.Context.SendActivityAsync(replyActivity);
            }

            var reply = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ConfirmMultiplContactEmailSinglePage]", new { userName = confirmedPerson.DisplayName });
            var options = new PromptOptions
            {
                Choices = new List<Choice>(),
                Prompt = reply as Activity
            };

            if (!isSinglePage)
            {
                var multiPageReply = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ConfirmMultiplContactEmailMultiPage]", new { userName = confirmedPerson.DisplayName });
                options.Prompt = multiPageReply as Activity;
            }

            var didntUnderstandReply = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[DidntUnderstandMessage]", null);
            for (var i = 0; i < emailList.Count; i++)
            {
                var user = confirmedPerson;
                var mailAddress = emailList[i] ?? user.UserPrincipalName;

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
                        options.RetryPrompt = didntUnderstandReply as Activity;
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
            options.RetryPrompt = didntUnderstandReply as Activity;
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

        private async Task<PromptOptions> GenerateOptionsForName(WaterfallStepContext sc, List<PersonModel> unionList, ITurnContext context, bool isSinglePage = true)
        {
            var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
            var pageIndex = state.FindContactInfor.ShowContactsIndex;
            var pageSize = 3;
            var skip = pageSize * pageIndex;
            var currentRecipientName = state.FindContactInfor.CurrentContactName;

            // Go back to the last page when reaching the end.
            if (skip >= unionList.Count && pageIndex > 0)
            {
                state.FindContactInfor.ShowContactsIndex--;
                pageIndex = state.FindContactInfor.ShowContactsIndex;
                skip = pageSize * pageIndex;

                var replyActivity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[AlreadyLastPage]", null);
                await sc.Context.SendActivityAsync(replyActivity);
            }

            var reply = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ConfirmMultiplContactEmailSinglePage]", new { userName = currentRecipientName });
            var options = new PromptOptions
            {
                Choices = new List<Choice>(),
                Prompt = reply as Activity
            };

            if (!isSinglePage)
            {
                var multiPageReply = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ConfirmMultiplContactEmailMultiPage]", new { userName = currentRecipientName });
                options.Prompt = multiPageReply as Activity;
            }

            var didntUnderstandReply = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[DidntUnderstandMessage]", null);
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
                        options.RetryPrompt = didntUnderstandReply as Activity;
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
            options.RetryPrompt = didntUnderstandReply as Activity;
            return options;
        }

        private async Task<string> GetNameListStringAsync(WaterfallStepContext sc)
        {
            var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
            var unionList = state.FindContactInfor.ContactsNameList.ToList();
            if (unionList.Count == 1)
            {
                return unionList.First();
            }

            var nameString = string.Join(", ", unionList.ToArray().SkipLast(1)) + string.Format(CommonStrings.SeparatorFormat, CommonStrings.And) + unionList.Last();
            return nameString;
        }

        protected EmailStateBase DigestLuisResult(DialogContext dc, emailLuis luisResult, General generalLuisResult, SendEmailDialogState state, bool isBeginDialog)
        {
            try
            {
                var intent = luisResult.TopIntent().intent;
                var entity = luisResult.Entities;
                var generalEntity = generalLuisResult.Entities;

                if (entity != null)
                {
                    if (entity.ordinal != null)
                    {
                        try
                        {
                            var emailList = state.MessageList;
                            var value = entity.ordinal[0];
                            if (Math.Abs(value - (int)value) < double.Epsilon)
                            {
                                state.UserSelectIndex = (int)value - 1;
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    if (generalEntity != null && generalEntity.number != null && (entity.ordinal == null || entity.ordinal.Length == 0))
                    {
                        try
                        {
                            var emailList = state.MessageList;
                            var value = generalEntity.number[0];
                            if (Math.Abs(value - (int)value) < double.Epsilon)
                            {
                                state.UserSelectIndex = (int)value - 1;
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    if (!isBeginDialog)
                    {
                        return state;
                    }

                    switch (intent)
                    {
                        case emailLuis.Intent.SendEmail:
                        case emailLuis.Intent.Forward:
                        case emailLuis.Intent.Reply:
                            {
                                if (entity.EmailSubject != null)
                                {
                                    state.Subject = entity.EmailSubject[0];
                                }

                                if (entity.Message != null)
                                {
                                    state.Content = entity.Message[0];
                                }

                                if (entity.ContactName != null)
                                {
                                    foreach (var name in entity.ContactName)
                                    {
                                        if (!state.FindContactInfor.ContactsNameList.Contains(name))
                                        {
                                            state.FindContactInfor.ContactsNameList.Add(name);
                                        }
                                    }
                                }

                                if (entity.email != null)
                                {
                                    // As luis result for email address often contains extra spaces for word breaking
                                    // (e.g. send email to test@test.com, email address entity will be test @ test . com)
                                    // So use original user input as email address.
                                    var rawEntity = luisResult.Entities._instance.email;
                                    foreach (var emailAddress in rawEntity)
                                    {
                                        var email = luisResult.Text.Substring(emailAddress.StartIndex, emailAddress.EndIndex - emailAddress.StartIndex);
                                        if (Utilities.Util.IsEmail(email) && !state.FindContactInfor.ContactsNameList.Contains(email))
                                        {
                                            state.FindContactInfor.ContactsNameList.Add(email);
                                        }
                                    }
                                }

                                if (entity.SenderName != null)
                                {
                                    state.SenderName = entity.SenderName[0];

                                    // Clear focus email if there is any.
                                    state.Message.Clear();
                                }

                                break;
                            }

                        default:
                            break;
                    }
                }

                return state;
            }
            catch
            {
                return state;
            }
        }
    }
}