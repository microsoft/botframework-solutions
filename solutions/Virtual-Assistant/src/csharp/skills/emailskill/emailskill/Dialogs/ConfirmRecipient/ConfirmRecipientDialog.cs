using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.ConfirmRecipient.Resources;
using EmailSkill.Dialogs.Shared;
using EmailSkill.Dialogs.Shared.DialogOptions;
using EmailSkill.ServiceClients;
using EmailSkill.Dialogs.Shared.Resources;
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

namespace EmailSkill.Dialogs.ConfirmRecipient
{
    public class ConfirmRecipientDialog : EmailSkillDialog
    {
        public ConfirmRecipientDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ConfirmRecipientDialog), services, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var confirmRecipient = new WaterfallStep[]
            {
                ConfirmRecipient,
                AfterConfirmRecipient,
            };

            var updateRecipientName = new WaterfallStep[]
            {
                UpdateUserName,
                AfterUpdateUserName,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.ConfirmRecipient, confirmRecipient) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateRecipientName, updateRecipientName) { TelemetryClient = telemetryClient });
            AddDialog(new ChoicePrompt(Actions.Choice, ChoiceValidator) { Style = ListStyle.None });
            InitialDialogId = Actions.ConfirmRecipient;
        }

        public async Task<DialogTurnResult> UpdateUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);
                var currentRecipientName = state.NameList[state.ConfirmRecipientIndex];

                // todo: should make a reason enum
                if (((UpdateUserDialogOptions)sc.Options).Reason == UpdateUserDialogOptions.UpdateReason.TooMany)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(ConfirmRecipientResponses.PromptTooManyPeople, null, new StringDictionary() { { "UserName", currentRecipientName } }), });
                }

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(ConfirmRecipientResponses.PromptPersonNotFound, null, new StringDictionary() { { "UserName", currentRecipientName } }), });
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
                if (string.IsNullOrEmpty(userInput))
                {
                    return await sc.EndDialogAsync();
                }

                var state = await EmailStateAccessor.GetAsync(sc.Context);

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

        public async Task<DialogTurnResult> ConfirmRecipient(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                if ((state.NameList == null) || (state.NameList.Count == 0))
                {
                    return await sc.NextAsync();
                }

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

                // if cannot find related user's name and cannot take user input as email address, send not found
                if ((personList.Count < 1) && (userList.Count < 1) && (state.EmailList.Count < 1))
                {
                    return await sc.BeginDialogAsync(Actions.UpdateRecipientName, new UpdateUserDialogOptions(UpdateUserDialogOptions.UpdateReason.NotFound));
                }

                if (personList.Count == 1)
                {
                    var user = personList.FirstOrDefault();
                    if (user != null)
                    {
                        var result =
                            new FoundChoice()
                            {
                                Value =
                                    $"{user.DisplayName}: {user.ScoredEmailAddresses.FirstOrDefault()?.Address ?? user.UserPrincipalName}",
                            };

                        return await sc.NextAsync(result);
                    }
                }

                if (state.EmailList.Count == 1)
                {
                    var email = state.EmailList.FirstOrDefault();
                    if (email != null)
                    {
                        var result =
                            new FoundChoice()
                            {
                                Value =
                                    $"{email}: {email}",
                            };

                        return await sc.NextAsync(result);
                    }
                }

                if (sc.Options is UpdateUserDialogOptions updateUserDialogOptions)
                {
                    state.ShowRecipientIndex = 0;
                    state.ReadRecipientIndex = 0;
                    return await sc.BeginDialogAsync(Actions.UpdateRecipientName, updateUserDialogOptions);
                }

                // TODO: should be simplify
                var selectOption = await GenerateOptions(personList, userList, sc.Context);

                var startIndex = ConfigData.GetInstance().MaxReadSize * state.ReadRecipientIndex;
                var choices = new List<Choice>();
                for (int i = startIndex; i < selectOption.Choices.Count; i++)
                {
                    choices.Add(selectOption.Choices[i]);
                }

                selectOption.Choices = choices;
                state.RecipientChoiceList = choices;

                // If no more recipient to show, start update name flow and reset the recipient paging index.
                if (selectOption.Choices.Count == 0)
                {
                    state.ShowRecipientIndex = 0;
                    state.ReadRecipientIndex = 0;
                    state.RecipientChoiceList.Clear();
                    return await sc.BeginDialogAsync(Actions.UpdateRecipientName, new UpdateUserDialogOptions(UpdateUserDialogOptions.UpdateReason.NotFound));
                }

                selectOption.Prompt.Speak = SpeakHelper.ToSpeechSelectionDetailString(selectOption, ConfigData.GetInstance().MaxReadSize);

                // Update prompt string to include the choices because the list style is none;
                // TODO: should be removed if use adaptive card show choices.
                var choiceString = GetSelectPromptString(selectOption, true);
                selectOption.Prompt.Text += "\r\n" + choiceString;
                selectOption.RetryPrompt = sc.Context.Activity.CreateReply(EmailSharedResponses.NoChoiceOptions_Retry);

                return await sc.PromptAsync(Actions.Choice, selectOption);
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

        public async Task<DialogTurnResult> AfterConfirmRecipient(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

                        return await sc.ReplaceDialogAsync(Actions.ConfirmRecipient);
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
                        state.RecipientChoiceList.Clear();
                    }

                    if (state.ConfirmRecipientIndex < state.NameList.Count)
                    {
                        return await sc.ReplaceDialogAsync(Actions.ConfirmRecipient);
                    }
                }

                // save emails
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

                return await sc.EndDialogAsync(true);
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