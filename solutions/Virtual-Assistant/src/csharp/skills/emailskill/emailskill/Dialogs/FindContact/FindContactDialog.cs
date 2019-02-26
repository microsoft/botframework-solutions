using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.FindContact.Resources;
using EmailSkill.Dialogs.Shared;
using EmailSkill.Dialogs.Shared.DialogOptions;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.ServiceClients;
using EmailSkill.Util;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Graph;

namespace EmailSkill.Dialogs.FindContact
{
    public class FindContactDialog : EmailSkillDialog
    {
        public FindContactDialog(
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(FindContactDialog), services, responseManager, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient)
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
                var options = (UpdateUserDialogOptions)sc.Options;
                if (options.Reason == UpdateUserDialogOptions.UpdateReason.ConfirmNo)
                {
                    return await sc.PromptAsync(
                        Actions.Prompt,
                        new PromptOptions
                        {
                            Prompt = ResponseManager.GetResponse(EmailSharedResponses.NoRecipients)
                        });
                }

                var currentRecipientName = state.NameList[state.ConfirmRecipientIndex];
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
                                    { "UserName", state.NameList[state.ConfirmRecipientIndex] }
                                })
                        });
                }
                else
                {
                    if (state.ConfirmRecipientIndex < state.NameList.Count())
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(
                            FindContactResponses.UserNotFoundAgain,
                            new StringDictionary()
                            {
                                { "source", state.MailSourceType == Model.MailSource.Microsoft ? "Outlook" : "Gmail" },
                                { "UserName", state.NameList[state.ConfirmRecipientIndex] }
                            }));
                        state.ConfirmRecipientIndex++;
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
                                { "source", state.MailSourceType == Model.MailSource.Microsoft ? "Outlook" : "Gmail" },
                                { "UserName", state.NameList[state.ConfirmRecipientIndex] }
                          }));
                        state.ConfirmRecipientIndex = 0;
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
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                if (string.IsNullOrEmpty(userInput))
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindContactResponses.UserNotFoundAgain, new StringDictionary() { { "source", state.MailSourceType == Model.MailSource.Microsoft ? "Outlook" : "Gmail" } }));
                    return await sc.EndDialogAsync();
                }

                if (IsEmail(userInput))
                {
                    if (!state.EmailList.Contains(userInput))
                    {
                        state.EmailList.Add(userInput);
                    }

                    state.NameList.RemoveAt(state.ConfirmRecipientIndex);
                }
                else
                {
                    state.UnconfirmedPerson.Clear();
                    state.NameList[state.ConfirmRecipientIndex] = userInput;
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
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                if (((state.NameList == null) || (state.NameList.Count == 0)) && state.EmailList.Count == 0)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateRecipientName, new UpdateUserDialogOptions(UpdateUserDialogOptions.UpdateReason.NotFound));
                }

                var unionList = new List<Person>();

                if (state.FirstEnterFindContact || state.EmailList.Count > 0)
                {
                    state.FirstEnterFindContact = false;
                    if (state.NameList.Count > 1)
                    {
                        var nameString = await GetReadyToSendNameListStringAsync(sc);
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindContactResponses.BeforeSendingMessage, new StringDictionary() { { "NameList", nameString } }));
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
                        return await sc.ReplaceDialogAsync(Actions.ConfirmName, sc.Options);
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

                    // people you work with has the distinct email address has the highest priority
                    if (personList.Count == 1 && personList.First().ScoredEmailAddresses.Count() == 1)
                    {
                        state.ConfirmedPerson = personList.First();
                        return await sc.ReplaceDialogAsync(Actions.ConfirmEmail, personList.First());
                    }

                    personList.AddRange(userList);

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
                else
                {
                    return await sc.EndDialogAsync();
                }

                state.UnconfirmedPerson = unionList;

                if (unionList.Count == 0)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateRecipientName, new UpdateUserDialogOptions(UpdateUserDialogOptions.UpdateReason.NotFound));
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
                            else
                            {
                                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindContactResponses.AlreadyFirstPage));
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

                        return await sc.ReplaceDialogAsync(Actions.ConfirmName, sc.Options);
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
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = ResponseManager.GetResponse(FindContactResponses.PromptOneNameOneAddress, new StringDictionary() { { "UserName", name }, { "EmailAddress", confirmedPerson.ScoredEmailAddresses.First().Address } }), });
            }
            else
            {
                var emailString = string.Empty;
                var emailList = confirmedPerson.ScoredEmailAddresses.ToList();

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
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var confirmedPerson = state.ConfirmedPerson;
                var name = confirmedPerson.DisplayName;
                if (sc.Result is bool)
                {
                    if ((bool)sc.Result)
                    {
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

                        state.FirstRetryInFindContact = true;
                        state.ConfirmRecipientIndex++;
                        if (state.ConfirmRecipientIndex < state.NameList.Count)
                        {
                            return await sc.ReplaceDialogAsync(Actions.ConfirmName, sc.Options);
                        }
                        else
                        {
                            return await sc.EndDialogAsync();
                        }
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Actions.UpdateRecipientName, new UpdateUserDialogOptions(UpdateUserDialogOptions.UpdateReason.ConfirmNo));
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
                            else
                            {
                                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(FindContactResponses.AlreadyFirstPage));
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
                        state.FirstRetryInFindContact = true;

                        // Clean up data
                        state.ShowRecipientIndex = 0;
                        state.ReadRecipientIndex = 0;
                        state.ConfirmedPerson = new Person();
                        state.RecipientChoiceList.Clear();
                    }
                }

                if (state.ConfirmRecipientIndex < state.NameList.Count)
                {
                    return await sc.ReplaceDialogAsync(Actions.ConfirmName, sc.Options);
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

        private async Task<string> GetReadyToSendNameListStringAsync(WaterfallStepContext sc)
        {
            var state = await EmailStateAccessor.GetAsync(sc?.Context);
            var unionList = state.NameList.Concat(state.EmailList).ToList();
            if (unionList.Count == 1)
            {
                return unionList.First();
            }

            var nameString = string.Join(", ", unionList.ToArray().SkipLast(1)) + string.Format(CommonStrings.SeparatorFormat, CommonStrings.And) + unionList.Last();
            return nameString;
        }

        private async Task<PromptOptions> GenerateOptionsForEmail(WaterfallStepContext sc, Person confirmedPerson, ITurnContext context, bool isSinglePage = true)
        {
            var state = await EmailStateAccessor.GetAsync(context);
            var pageIndex = state.ShowRecipientIndex;
            var pageSize = ConfigData.GetInstance().MaxDisplaySize;
            var skip = pageSize * pageIndex;
            var emailList = confirmedPerson.ScoredEmailAddresses.ToList();

            // Go back to the last page when reaching the end.
            if (skip >= emailList.Count && pageIndex > 0)
            {
                state.ShowRecipientIndex--;
                state.ReadRecipientIndex = 0;
                pageIndex = state.ShowRecipientIndex;
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
                        options.Prompt.Speak = SpeakHelper.ToSpeechSelectionDetailString(options, ConfigData.GetInstance().MaxReadSize);
                        options.Prompt.Text += "\r\n" + GetSelectPromptEmailString(options, true);
                        options.RetryPrompt = ResponseManager.GetResponse(EmailSharedResponses.NoChoiceOptions_Retry);
                        return options;
                    }

                    options.Choices.Add(choice);
                }
                else
                {
                    skip--;
                }
            }

            options.Prompt.Speak = SpeakHelper.ToSpeechSelectionDetailString(options, ConfigData.GetInstance().MaxReadSize);
            options.Prompt.Text += "\r\n" + GetSelectPromptEmailString(options, true);
            options.RetryPrompt = ResponseManager.GetResponse(EmailSharedResponses.NoChoiceOptions_Retry);
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

        private async Task<PromptOptions> GenerateOptionsForName(WaterfallStepContext sc, List<Person> unionList, ITurnContext context, bool isSinglePage = true)
        {
            var state = await EmailStateAccessor.GetAsync(context);
            var pageIndex = state.ShowRecipientIndex;
            var pageSize = ConfigData.GetInstance().MaxDisplaySize;
            var skip = pageSize * pageIndex;
            var currentRecipientName = state.NameList[state.ConfirmRecipientIndex];

            // Go back to the last page when reaching the end.
            if (skip >= unionList.Count && pageIndex > 0)
            {
                state.ShowRecipientIndex--;
                state.ReadRecipientIndex = 0;
                pageIndex = state.ShowRecipientIndex;
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
                        options.Prompt.Speak = SpeakHelper.ToSpeechSelectionDetailString(options, ConfigData.GetInstance().MaxReadSize);
                        options.Prompt.Text += "\r\n" + GetSelectPromptString(options, true);
                        options.RetryPrompt = ResponseManager.GetResponse(EmailSharedResponses.NoChoiceOptions_Retry);
                        return options;
                    }

                    options.Choices.Add(choice);
                }
                else
                {
                    skip--;
                }
            }

            options.Prompt.Speak = SpeakHelper.ToSpeechSelectionDetailString(options, ConfigData.GetInstance().MaxReadSize);
            options.Prompt.Text += "\r\n" + GetSelectPromptString(options, true);
            options.RetryPrompt = ResponseManager.GetResponse(EmailSharedResponses.NoChoiceOptions_Retry);
            return options;
        }
    }
}