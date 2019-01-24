using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
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

namespace EmailSkill.Dialogs.FindContact
{
    public class FindContact : EmailSkillDialog
    {
        public FindContact(
            SkillConfigurationBase services,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(FindContact), services, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var confirmRecipient = new WaterfallStep[]
            {
                ConfirmName,
                AfterConfirmName
            };


            InitialDialogId = Actions.FindContact;
        }

        public async Task<DialogTurnResult> ConfirmName(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

                var unionList = new List<Person>();
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
                            foreach (var sameNamePerson in personWithSameName.Skip(1))
                            {
                                unionPerson.ScoredEmailAddresses = unionPerson.ScoredEmailAddresses.Union(sameNamePerson.ScoredEmailAddresses);
                            }

                            unionList.Add(unionPerson);
                        }
                    }
                }

                if (unionList.Count == 1)
                {
                    if (unionList.FirstOrDefault().ScoredEmailAddresses.Count() == 1)
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(FindContactResponses.PromptOneNameOneAddress, null, new StringDictionary() { { "UserName", currentRecipientName }, { "EmailAddress", unionList.FirstOrDefault().ScoredEmailAddresses.First().Address } }), });
                    }
                    else
                    {

                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(FindContactResponses.PromptOneNameOneAddress, null, new StringDictionary() { { "UserName", currentRecipientName }, { "EmailAddress", unionList.FirstOrDefault().ScoredEmailAddresses.First().Address } }), });
                    }
                }

                //if (personList.Count == 1)
                //{
                //    var user = personList.FirstOrDefault();
                //    if (user != null)
                //    {
                //        var result =
                //            new FoundChoice()
                //            {
                //                Value =
                //                    $"{user.DisplayName}: {user.ScoredEmailAddresses.FirstOrDefault()?.Address ?? user.UserPrincipalName}",
                //            };

                //        return await sc.NextAsync(result);
                //    }
                //}

                //if (state.EmailList.Count == 1)
                //{
                //    var email = state.EmailList.FirstOrDefault();
                //    if (email != null)
                //    {
                //        var result =
                //            new FoundChoice()
                //            {
                //                Value =
                //                    $"{email}: {email}",
                //            };

                //        return await sc.NextAsync(result);
                //    }
                //}

                //if (sc.Options is UpdateUserDialogOptions updateUserDialogOptions)
                //{
                //    state.ShowRecipientIndex = 0;
                //    state.ReadRecipientIndex = 0;
                //    return await sc.BeginDialogAsync(Actions.UpdateRecipientName, updateUserDialogOptions);
                //}

                //// TODO: should be simplify
                //var selectOption = await GenerateOptions(personList, userList, sc.Context);

                //var startIndex = ConfigData.GetInstance().MaxReadSize * state.ReadRecipientIndex;
                //var choices = new List<Choice>();
                //for (int i = startIndex; i < selectOption.Choices.Count; i++)
                //{
                //    choices.Add(selectOption.Choices[i]);
                //}

                //selectOption.Choices = choices;
                //state.RecipientChoiceList = choices;

                //// If no more recipient to show, start update name flow and reset the recipient paging index.
                //if (selectOption.Choices.Count == 0)
                //{
                //    state.ShowRecipientIndex = 0;
                //    state.ReadRecipientIndex = 0;
                //    state.RecipientChoiceList.Clear();
                //    return await sc.BeginDialogAsync(Actions.UpdateRecipientName, new UpdateUserDialogOptions(UpdateUserDialogOptions.UpdateReason.NotFound));
                //}

                //selectOption.Prompt.Speak = SpeakHelper.ToSpeechSelectionDetailString(selectOption, ConfigData.GetInstance().MaxReadSize);

                //// Update prompt string to include the choices because the list style is none;
                //// TODO: should be removed if use adaptive card show choices.
                //var choiceString = GetSelectPromptString(selectOption, true);
                //selectOption.Prompt.Text += "\r\n" + choiceString;
                //selectOption.RetryPrompt = sc.Context.Activity.CreateReply(EmailSharedResponses.NoChoiceOptions_Retry);

                //return await sc.PromptAsync(Actions.Choice, selectOption);

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

        }
    }
}