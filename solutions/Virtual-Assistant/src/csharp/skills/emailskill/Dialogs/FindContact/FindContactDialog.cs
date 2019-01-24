using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.FindContact.Resources;
using EmailSkill.Dialogs.Shared;
using EmailSkill.Dialogs.Shared.DialogOptions;
using EmailSkill.ServiceClients;
using EmailSkill.Util;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Data;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Graph;

namespace EmailSkill.Dialogs.FindContact
{
    public class FindContactDialog : EmailSkillDialog
    {

        public FindContactDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(FindContactDialog), services, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient)
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

            var updateRecipientName = new WaterfallStep[]
            {
                UpdateUserName,
                AfterUpdateUserName,
            };

            AddDialog(new WaterfallDialog(Actions.ConfirmName, confirmName) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ConfirmEmail, confirmEmail) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateRecipientName, updateRecipientName) { TelemetryClient = telemetryClient });
            AddDialog(new ChoicePrompt(Actions.Choice, ChoiceValidator) { Style = ListStyle.None });
            InitialDialogId = Actions.ConfirmName;
        }

        public async Task<DialogTurnResult> UpdateUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var currentRecipientName = state.NameList[state.ConfirmRecipientIndex];
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
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                if (string.IsNullOrEmpty(userInput))
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(FindContactResponses.UserNotFoundAgain, null, new StringDictionary() { { "source", state.MailSourceType == Model.MailSource.Microsoft ? "Outlook" : "Gmail" } }));
                    return await sc.EndDialogAsync();
                }

                if (IsEmail(userInput))
                {
                    if (!state.EmailList.Contains(userInput))
                    {
                        state.EmailList.Add(userInput);
                    }
                }
                else
                {
                    state.NameList[state.ConfirmRecipientIndex] = userInput;
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
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var skillOptions = (EmailSkillDialogOptions)sc.Options;

                if (((state.NameList == null) || (state.NameList.Count == 0)) && state.EmailList.Count == 0)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateRecipientName);
                }

                var unionList = new List<Person>();

                if (skillOptions != null || state.EmailList.Count > 0)
                {
                    if (state.NameList.Count > 1)
                    {
                        var nameString = await GetReadyToSendNameListStringAsync(sc);
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(FindContactResponses.BeforeSendingMessage, null, new StringDictionary() { { "NameList", nameString } }));
                    }
                }

                if (state.EmailList.Count > 0)
                {
                    foreach (var email in state.EmailList)
                    {
                        var recipient = new Recipient();
                        var emailAddress = new EmailAddress
                        {
                            Name = email,
                            Address = email,
                        };
                        recipient.EmailAddress = emailAddress;

                        if (state.Recipients.All(r => r.EmailAddress.Address != emailAddress.Address))
                        {
                            state.Recipients.Add(recipient);
                        }
                    }

                    state.EmailList.Clear();

                    if (state.NameList.Count > 0)
                    {
                        return await sc.ReplaceDialogAsync(Actions.ConfirmName);
                    }
                    else
                    {
                        return await sc.EndDialogAsync();
                    }
                }

                if (state.ConfirmRecipientIndex < state.NameList.Count)
                {
                    var currentRecipientName = state.NameList[state.ConfirmRecipientIndex];

                    var originPersonList = await GetPeopleWorkWithAsync(sc.Context, currentRecipientName);
                    var originContactList = await GetContactsAsync(sc.Context, currentRecipientName);
                    originPersonList.AddRange(originContactList);

                    var originUserList = new List<Person>();
                    try
                    {
                        originUserList = await GetUserAsync(sc.Context, currentRecipientName);
                    }
                    catch
                    {
                        // do nothing when get user failed. because can not use token to ensure user use a work account.
                    }

                    (var personList, var userList) = DisplayHelper.FormatRecipientList(originPersonList, originUserList);

                    foreach (var person in personList)
                    {
                        if (unionList.Find(p => p.DisplayName == person.DisplayName) == null)
                        {
                            var personWithSameName = personList.FindAll(p => p.DisplayName == person.DisplayName);
                            if (personWithSameName.Count == 1)
                            {
                                unionList.Add(personWithSameName.FirstOrDefault());
                            }
                            else
                            {
                                var unionPerson = personWithSameName.FirstOrDefault();
                                var emailList = new List<ScoredEmailAddress>();
                                foreach (var sameNamePerson in personWithSameName)
                                {
                                    sameNamePerson.ScoredEmailAddresses.ToList().ForEach(e => emailList.Add(e));
                                }

                                unionPerson.ScoredEmailAddresses = emailList;
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
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;
                var generlLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generlLuisResult?.TopIntent().intent;

                if (state.NameList != null && state.NameList.Count > 0)
                {
                    if (sc.Result == null)
                    {
                        if (generalTopIntent == General.Intent.Next)
                        {
                            state.ShowRecipientIndex++;
                            state.ReadRecipientIndex = 0;
                        }
                        else if (generalTopIntent == General.Intent.Previous)
                        {
                            if (state.ShowRecipientIndex > 0)
                            {
                                state.ShowRecipientIndex--;
                                state.ReadRecipientIndex = 0;
                            }
                        }
                        else if (IsReadMoreIntent(generalTopIntent, sc.Context.Activity.Text))
                        {
                            if (state.RecipientChoiceList.Count <= ConfigData.GetInstance().MaxReadSize)
                            {
                                state.ShowRecipientIndex++;
                                state.ReadRecipientIndex = 0;
                            }
                            else
                            {
                                state.ReadRecipientIndex++;
                            }
                        }
                        else
                        {
                            // result is null when just update the recipient name. show recipients page should be reset.
                            state.ShowRecipientIndex = 0;
                            state.ReadRecipientIndex = 0;
                        }

                        return await sc.ReplaceDialogAsync(Actions.ConfirmName);
                    }

                    var choiceResult = (sc.Result as FoundChoice)?.Value.Trim('*');
                    if (choiceResult != null)
                    {
                        // Clean up data
                        state.ShowRecipientIndex = 0;
                        state.ReadRecipientIndex = 0;
                        state.RecipientChoiceList.Clear();

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
            var state = await EmailStateAccessor.GetAsync(sc.Context);
            var confirmedPerson = sc.Options as Person;
            var name = confirmedPerson.DisplayName;
            if (confirmedPerson.ScoredEmailAddresses.Count() == 1)
            {
                // Highest probability
                var recipient = new Recipient();
                var emailAddress = new EmailAddress
                {
                    Name = name,
                    Address = confirmedPerson.ScoredEmailAddresses.First().Address
                };
                recipient.EmailAddress = emailAddress;
                if (state.Recipients.All(r => r.EmailAddress.Address != emailAddress.Address))
                {
                    state.Recipients.Add(recipient);
                }

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(FindContactResponses.PromptOneNameOneAddress, null, new StringDictionary() { { "UserName", name }, { "EmailAddress", confirmedPerson.ScoredEmailAddresses.First().Address } }), });
            }
            else
            {
                var emailString = string.Empty;
                var emailList = confirmedPerson.ScoredEmailAddresses.ToList();

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
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                if (sc.Result is bool)
                {
                    if ((bool)sc.Result)
                    {
                        state.ConfirmRecipientIndex++;
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

                if (state.NameList != null && state.NameList.Count > 0)
                {
                    if (sc.Result == null)
                    {
                        if (generalTopIntent == General.Intent.Next)
                        {
                            state.ShowRecipientIndex++;
                            state.ReadRecipientIndex = 0;
                        }
                        else if (generalTopIntent == General.Intent.Previous)
                        {
                            if (state.ShowRecipientIndex > 0)
                            {
                                state.ShowRecipientIndex--;
                                state.ReadRecipientIndex = 0;
                            }
                        }
                        else if (IsReadMoreIntent(generalTopIntent, sc.Context.Activity.Text))
                        {
                            if (state.RecipientChoiceList.Count <= ConfigData.GetInstance().MaxReadSize)
                            {
                                state.ShowRecipientIndex++;
                                state.ReadRecipientIndex = 0;
                            }
                            else
                            {
                                state.ReadRecipientIndex++;
                            }
                        }
                        else
                        {
                            // result is null when just update the recipient name. show recipients page should be reset.
                            state.ShowRecipientIndex = 0;
                            state.ReadRecipientIndex = 0;
                        }

                        var confirmedPerson = state.ConfirmedPerson;
                        return await sc.ReplaceDialogAsync(Actions.ConfirmEmail, confirmedPerson);
                    }

                    var choiceResult = (sc.Result as FoundChoice)?.Value.Trim('*');
                    if (choiceResult != null)
                    {
                        // Find an recipient
                        var recipient = new Recipient();
                        var emailAddress = new EmailAddress
                        {
                            Name = choiceResult.Split(": ")[0],
                            Address = choiceResult.Split(": ")[1],
                        };
                        recipient.EmailAddress = emailAddress;
                        if (state.Recipients.All(r => r.EmailAddress.Address != emailAddress.Address))
                        {
                            state.Recipients.Add(recipient);
                        }

                        state.ConfirmRecipientIndex++;

                        // Clean up data
                        state.ShowRecipientIndex = 0;
                        state.ReadRecipientIndex = 0;
                        state.ConfirmedPerson = new Person();
                        state.RecipientChoiceList.Clear();
                    }
                }

                if (state.ConfirmRecipientIndex < state.NameList.Count)
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
            var state = await EmailStateAccessor.GetAsync(pc.Context);
            var luisResult = state.LuisResult;
            var topIntent = luisResult?.TopIntent().intent;
            var generlLuisResult = state.GeneralLuisResult;
            var generalTopIntent = generlLuisResult?.TopIntent().intent;

            if ((generalTopIntent == General.Intent.Next)
                || (generalTopIntent == General.Intent.Previous)
                || IsReadMoreIntent(generalTopIntent, pc.Context.Activity.Text))
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