using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.ConfirmRecipient.Resources;
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

namespace EmailSkill
{
    public class ConfirmRecipientDialog : EmailSkillDialog
    {
        public ConfirmRecipientDialog(
            ISkillConfiguration services,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IServiceManager serviceManager)
            : base(nameof(ConfirmRecipientDialog), services, emailStateAccessor, dialogStateAccessor, serviceManager)
        {
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
            AddDialog(new WaterfallDialog(Actions.ConfirmRecipient, confirmRecipient));
            AddDialog(new WaterfallDialog(Actions.UpdateRecipientName, updateRecipientName));
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

                var originPersonList = await GetPeopleWorkWithAsync(sc, currentRecipientName);
                var originContactList = await GetContactsAsync(sc, currentRecipientName);
                originPersonList.AddRange(originContactList);

                // msa account can not get user from your org. and token type is not jwt.
                // TODO: find a way to check the account is msa or aad.
                var handler = new JwtSecurityTokenHandler();
                var originUserList = new List<Person>();
                try
                {
                    originUserList = await GetUserAsync(sc, currentRecipientName);
                }
                catch
                {
                    // do nothing when get user failed. because can not use token to ensure user use a work account.
                }

                (var personList, var userList) = DisplayHelper.FormatRecipientList(originPersonList, originUserList);

                // todo: should set updatename reason in stepContext.Result
                if (personList.Count > 10)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateRecipientName, new UpdateUserDialogOptions(UpdateUserDialogOptions.UpdateReason.TooMany));
                }

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
                var selectOption = await GenerateOptions(personList, userList, sc);

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
                selectOption.Prompt.Text = choiceString;

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

                if (state.NameList != null && state.NameList.Count > 0)
                {
                    // result is null when just update the recipient name. show recipients page should be reset.
                    if (sc.Result == null)
                    {
                        state.ShowRecipientIndex = 0;
                        state.ReadRecipientIndex = 0;
                        return await sc.BeginDialogAsync(Actions.ConfirmRecipient);
                    }

                    var choiceResult = (sc.Result as FoundChoice)?.Value.Trim('*');
                    if (choiceResult != null)
                    {
                        if (choiceResult == General.Intent.Next.ToString())
                        {
                            state.ShowRecipientIndex++;
                            state.ReadRecipientIndex = 0;
                            return await sc.BeginDialogAsync(Actions.ConfirmRecipient);
                        }

                        if (choiceResult == General.Intent.ReadMore.ToString())
                        {
                            if (state.RecipientChoiceList.Count <= ConfigData.GetInstance().MaxReadSize)
                            {
                                // Set readmore as false when return to next page
                                state.ShowRecipientIndex++;
                                state.ReadRecipientIndex = 0;
                            }
                            else
                            {
                                state.ReadRecipientIndex++;
                            }

                            return await sc.BeginDialogAsync(Actions.ConfirmRecipient);
                        }

                        if (choiceResult == UpdateUserDialogOptions.UpdateReason.TooMany.ToString())
                        {
                            state.ShowRecipientIndex++;
                            state.ReadRecipientIndex = 0;
                            return await sc.BeginDialogAsync(Actions.ConfirmRecipient, new UpdateUserDialogOptions(UpdateUserDialogOptions.UpdateReason.TooMany));
                        }

                        if (choiceResult == General.Intent.Previous.ToString())
                        {
                            if (state.ShowRecipientIndex > 0)
                            {
                                state.ShowRecipientIndex--;
                                state.ReadRecipientIndex = 0;
                            }

                            return await sc.BeginDialogAsync(Actions.ConfirmRecipient);
                        }

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
                        return await sc.BeginDialogAsync(Actions.ConfirmRecipient);
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

        protected override Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            if (result.ToString().Equals(CommonUtil.DialogTurnResultCancelAllDialogs, StringComparison.InvariantCultureIgnoreCase))
            {
                return outerDc.CancelAllDialogsAsync();
            }
            else
            {
                return base.EndComponentAsync(outerDc, result, cancellationToken);
            }
        }
    }
}