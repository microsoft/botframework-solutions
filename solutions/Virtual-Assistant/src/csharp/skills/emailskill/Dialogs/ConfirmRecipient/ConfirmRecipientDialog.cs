using EmailSkill.Dialogs.ConfirmRecipient.Resources;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Extensions;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EmailSkill
{
    public class ConfirmRecipientDialog : EmailSkillDialog
    {
        public ConfirmRecipientDialog(
            ISkillConfiguration services,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IMailSkillServiceManager serviceManager)
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
            AddDialog(new WaterfallDialog(Action.ConfirmRecipient, confirmRecipient));
            AddDialog(new WaterfallDialog(Action.UpdateRecipientName, updateRecipientName));
            InitialDialogId = Action.ConfirmRecipient;
        }

       public async Task<DialogTurnResult> UpdateUserName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _emailStateAccessor.GetAsync(sc.Context);
                var currentRecipientName = state.NameList[state.ConfirmRecipientIndex];

                // todo: should make a reason enum
                if (((UpdateUserDialogOptions)sc.Options).Reason == UpdateUserDialogOptions.UpdateReason.TooMany)
                {
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(ConfirmRecipientResponses.PromptTooManyPeople, null, new StringDictionary() { { "UserName", currentRecipientName } }), });
                }

                return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(ConfirmRecipientResponses.PromptPersonNotFound, null, new StringDictionary() { { "UserName", currentRecipientName } }), });
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
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

                var state = await _emailStateAccessor.GetAsync(sc.Context);
                state.NameList[state.ConfirmRecipientIndex] = userInput;

                // should not return with value, next step use the return value for confirmation.
                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        public async Task<DialogTurnResult> ConfirmRecipient(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _emailStateAccessor.GetAsync(sc.Context);
                var currentRecipientName = state.NameList[state.ConfirmRecipientIndex];

                //var personList = await GetPeopleWorkWithAsync(sc, currentRecipientName);
                var personList = await GetContactAsync(sc, currentRecipientName);

                // msa account can not get user from your org. and token type is not jwt.
                // TODO: find a way to check the account is msa or aad.
                var handler = new JwtSecurityTokenHandler();
                var userList = new List<Person>();
                try
                {
                    userList = await GetUserAsync(sc, currentRecipientName);
                }
                catch
                {
                    // do nothing when get user failed. because can not use token to ensure user use a work account.
                }

                // todo: should set updatename reason in stepContext.Result
                if (personList.Count > 10)
                {
                    return await sc.BeginDialogAsync(Action.UpdateRecipientName, new UpdateUserDialogOptions(UpdateUserDialogOptions.UpdateReason.TooMany));
                }

                if (personList.Count < 1 && userList.Count < 1)
                {
                    return await sc.BeginDialogAsync(Action.UpdateRecipientName, new UpdateUserDialogOptions(UpdateUserDialogOptions.UpdateReason.NotFound));
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

                if (sc.Options is UpdateUserDialogOptions updateUserDialogOptions)
                {
                    state.ShowRecipientIndex = 0;
                    return await sc.BeginDialogAsync(Action.UpdateRecipientName, updateUserDialogOptions);
                }

                // TODO: should be simplify
                var selectOption = await GenerateOptions(personList, userList, sc);

                // If no more recipient to show, start update name flow and reset the recipient paging index.
                if (selectOption.Choices.Count == 0)
                {
                    state.ShowRecipientIndex = 0;
                    return await sc.BeginDialogAsync(Action.UpdateRecipientName, new UpdateUserDialogOptions(UpdateUserDialogOptions.UpdateReason.TooMany));
                }

                // Update prompt string to include the choices because the list style is none;
                // TODO: should be removed if use adaptive card show choices.
                var choiceString = GetSelectPromptString(selectOption, true);
                selectOption.Prompt.Text = choiceString;
                return await sc.PromptAsync(Action.Choice, selectOption);
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        public async Task<DialogTurnResult> AfterConfirmRecipient(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _emailStateAccessor.GetAsync(sc.Context);

                // result is null when just update the recipient name. show recipients page should be reset.
                if (sc.Result == null)
                {
                    state.ShowRecipientIndex = 0;
                    return await sc.BeginDialogAsync(Action.ConfirmRecipient);
                }

                var choiceResult = (sc.Result as FoundChoice)?.Value.Trim('*');
                if (choiceResult != null)
                {
                    if (choiceResult == General.Intent.Next.ToString())
                    {
                        state.ShowRecipientIndex++;
                        return await sc.BeginDialogAsync(Action.ConfirmRecipient);
                    }

                    if (choiceResult == UpdateUserDialogOptions.UpdateReason.TooMany.ToString())
                    {
                        state.ShowRecipientIndex++;
                        return await sc.BeginDialogAsync(Action.ConfirmRecipient, new UpdateUserDialogOptions(UpdateUserDialogOptions.UpdateReason.TooMany));
                    }

                    if (choiceResult == General.Intent.Previous.ToString())
                    {
                        if (state.ShowRecipientIndex > 0)
                        {
                            state.ShowRecipientIndex--;
                        }

                        return await sc.BeginDialogAsync(Action.ConfirmRecipient);
                    }

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
                }

                if (state.ConfirmRecipientIndex < state.NameList.Count)
                {
                    return await sc.BeginDialogAsync(Action.ConfirmRecipient);
                }

                return await sc.EndDialogAsync(true);
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }
    }
}
