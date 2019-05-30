using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Models.DialogModel;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using EmailSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;

namespace EmailSkill.Dialogs
{
    public class ForwardEmailDialog : EmailSkillDialogBase
    {
        public ForwardEmailDialog(
           BotSettings settings,
           BotServices services,
           ResponseManager responseManager,
           ConversationState conversationState,
           FindContactDialog findContactDialog,
           IServiceManager serviceManager,
           IBotTelemetryClient telemetryClient)
           : base(nameof(ForwardEmailDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var forwardEmail = new WaterfallStep[]
            {
                InitDialogState,
                GetAuthToken,
                AfterGetAuthToken,
                SetDisplayConfig,
                CollectSelectedEmail,
                AfterCollectSelectedEmail,
                CollectRecipient,
                CollectAdditionalText,
                AfterCollectAdditionalText,
                ConfirmBeforeSending,
                ConfirmAllRecipient,
                ForwardEmail,
            };

            var showEmail = new WaterfallStep[]
            {
                SaveDialogState,
                PagingStep,
                ShowEmails,
            };

            var collectRecipients = new WaterfallStep[]
            {
                SaveDialogState,
                PromptRecipientCollection,
                GetRecipients,
            };

            var updateSelectMessage = new WaterfallStep[]
            {
                SaveDialogState,
                UpdateMessage,
                PromptUpdateMessage,
                AfterUpdateMessage,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new EmailWaterfallDialog(Actions.Forward, forwardEmail, EmailStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new EmailWaterfallDialog(Actions.Show, showEmail, EmailStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new EmailWaterfallDialog(Actions.CollectRecipient, collectRecipients, EmailStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new EmailWaterfallDialog(Actions.UpdateSelectMessage, updateSelectMessage, EmailStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new FindContactDialog(settings, services, responseManager, conversationState, serviceManager, telemetryClient));
            InitialDialogId = Actions.Forward;
        }

        protected virtual async Task<DialogTurnResult> InitDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var userState = await EmailStateAccessor.GetAsync(sc.Context);
                var dialogState = new SendEmailDialogState();

                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = Services.CognitiveModelSets[locale];

                // Update state with email luis result and entities --- todo: use luis result in adaptive dialog
                var luisResult = await localeConfig.LuisServices["email"].RecognizeAsync<EmailLuis>(sc.Context);
                userState.LuisResult = luisResult;
                localeConfig.LuisServices.TryGetValue("general", out var luisService);
                var generalLuisResult = await luisService.RecognizeAsync<General>(sc.Context);
                userState.GeneralLuisResult = generalLuisResult;

                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (skillOptions != null && skillOptions.SubFlowMode)
                {
                    dialogState = userState?.CacheModel != null ? new SendEmailDialogState(userState?.CacheModel) : dialogState;
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

        protected virtual async Task<DialogTurnResult> SaveDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var dialogState = new SendEmailDialogState();
                if (skillOptions != null && skillOptions.DialogState != null)
                {
                    if (skillOptions.DialogState is SendEmailDialogState)
                    {
                        dialogState = (SendEmailDialogState)skillOptions.DialogState;
                    }

                    dialogState = skillOptions.DialogState != null ? new SendEmailDialogState(skillOptions.DialogState) : dialogState;
                }

                var userState = await EmailStateAccessor.GetAsync(sc.Context);

                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = Services.CognitiveModelSets[locale];

                // Update state with email luis result and entities --- todo: use luis result in adaptive dialog
                var luisResult = await localeConfig.LuisServices["email"].RecognizeAsync<EmailLuis>(sc.Context);
                userState.LuisResult = luisResult;
                localeConfig.LuisServices.TryGetValue("general", out var luisService);
                var generalLuisResult = await luisService.RecognizeAsync<General>(sc.Context);
                userState.GeneralLuisResult = generalLuisResult;

                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

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

        public async Task<DialogTurnResult> ForwardEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var state = (SendEmailDialogState)sc.State.Dialog[EmailStateKey];
                    var userState = await EmailStateAccessor.GetAsync(sc.Context);

                    var token = userState.Token;
                    var message = state.Message;
                    var id = message.FirstOrDefault()?.Id;
                    var recipients = state.FindContactInfor.Contacts;

                    var service = ServiceManager.InitMailService(token, userState.GetUserTimeZone(), userState.MailSourceType);

                    // send user message.
                    var content = state.Content.Equals(EmailCommonStrings.EmptyContent) ? string.Empty : state.Content;
                    await service.ForwardMessageAsync(id, content, recipients);

                    var emailCard = new EmailCardData
                    {
                        Subject = state.Subject.Equals(EmailCommonStrings.EmptySubject) ? null : state.Subject,
                        EmailContent = state.Content.Equals(EmailCommonStrings.EmptyContent) ? null : state.Content,
                    };
                    emailCard = await ProcessRecipientPhotoUrl(sc.Context, emailCard, state.FindContactInfor.Contacts);

                    var stringToken = new StringDictionary
                    {
                        { "Subject", state.Subject },
                    };

                    var recipientCard = state.FindContactInfor.Contacts.Count() > 5 ? "ConfirmCard_RecipientMoreThanFive" : "ConfirmCard_RecipientLessThanFive";
                    var reply = ResponseManager.GetCardResponse(
                        EmailSharedResponses.SentSuccessfully,
                        new Card("EmailWithOutButtonCard", emailCard),
                        stringToken,
                        "items",
                        new List<Card>().Append(new Card(recipientCard, emailCard)));

                    await sc.Context.SendActivityAsync(reply);
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(EmailSharedResponses.CancellingMessage));
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }

            await ClearConversationState(sc);
            return await sc.EndDialogAsync(true);
        }

        protected EmailStateBase DigestLuisResult(DialogContext dc, EmailLuis luisResult, General generalLuisResult, SendEmailDialogState state, bool isBeginDialog)
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
                        case EmailLuis.Intent.SendEmail:
                        case EmailLuis.Intent.Forward:
                        case EmailLuis.Intent.Reply:
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