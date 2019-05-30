using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Extensions;
using EmailSkill.Models;
using EmailSkill.Models.DialogModel;
using EmailSkill.Responses.Shared;
using EmailSkill.Responses.ShowEmail;
using EmailSkill.Services;
using EmailSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Graph;

namespace EmailSkill.Dialogs
{
    public class ShowEmailDialog : EmailSkillDialogBase
    {
        public ShowEmailDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            DeleteEmailDialog deleteEmailDialog,
            ReplyEmailDialog replyEmailDialog,
            ForwardEmailDialog forwardEmailDialog,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ShowEmailDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var skillOptions = new EmailSkillDialogOptions
            {
                SubFlowMode = true
            };

            var rootDialog = new AdaptiveDialog("ShowEmailRootDialog")
            {
                Recognizer = CreateRecognizer(),
                Rules = new List<IRule>()
                {
                    new IntentRule("Forward")
                    {
                        Steps = new List<IDialog>()
                        {
                            new BeginDialog(nameof(ForwardEmailDialog), options: skillOptions),
                        }
                    },

                    new IntentRule("Delete")
                    {
                        Steps = new List<IDialog>()
                        {
                            new BeginDialog(nameof(DeleteEmailDialog), options: skillOptions),
                        }
                    },

                    new IntentRule("Reply")
                    {
                        Steps = new List<IDialog>()
                        {
                            new BeginDialog(nameof(ReplyEmailDialog), options: skillOptions),
                        }
                    },

                    new IntentRule("CheckMessages")
                    {
                        Steps = new List<IDialog>()
                        {
                            new BeginDialog(Actions.DisplayFiltered, options: skillOptions),
                        }
                    },

                    new IntentRule("SearchMessages")
                    {
                        Steps = new List<IDialog>()
                        {
                            new BeginDialog(Actions.DisplayFiltered, options: skillOptions),
                        }
                    },

                    new IntentRule("ShowNext")
                    {
                        Steps = new List<IDialog>()
                        {
                            new ReplaceDialog(Actions.Show, options: skillOptions),
                        }
                    },

                    new IntentRule("ShowPrevious")
                    {
                        Steps = new List<IDialog>()
                        {
                            new ReplaceDialog(Actions.Show, options: skillOptions),
                        }
                    },
                },

                Steps = new List<IDialog>()
                {
                    //new CodeStep(SaveLuisState1),
                    //new CodeStep(SaveLuisState2),
                    new BeginDialog(Actions.Show, options: skillOptions),
                },
            };

            var showEmail = new WaterfallStep[]
            {
                SaveDialogState,
                GetAuthToken,
                AfterGetAuthToken,
                Display
            };

            var readEmail = new WaterfallStep[]
            {
                SaveDialogState,
                ReadEmail,
            };

            var displayEmail = new WaterfallStep[]
            {
                SaveDialogState,
                IfClearPagingConditionStep,
                PagingStep,
                ShowEmails,
                PromptToHandle,
                CheckRead,
                //HandleMore
                PromptToReshow,
                HandleReshow
            };

            var displayFilteredEmail = new WaterfallStep[]
            {
                SaveDialogState,
                FilterEmail,
                ShowFilteredEmails,
                //PromptToHandle,
                //CheckRead,
                //HandleMore
            };

            //var redisplayEmail = new WaterfallStep[]
            //{
            //    PromptToReshow,
            //    CheckReshow,
            //    HandleMore,
            //};

            // Define the conversation flow using a waterfall model.
            var showEmailDialog = new EmailWaterfallDialog(Actions.Show, showEmail, EmailStateAccessor) { TelemetryClient = telemetryClient };
            var readEmailDialog = new EmailWaterfallDialog(Actions.Read, readEmail, EmailStateAccessor) { TelemetryClient = telemetryClient };
            var displayDialog = new EmailWaterfallDialog(Actions.Display, displayEmail, EmailStateAccessor) { TelemetryClient = telemetryClient };
            var displayFilteredDialog = new EmailWaterfallDialog(Actions.DisplayFiltered, displayFilteredEmail, EmailStateAccessor) { TelemetryClient = telemetryClient };
            var promptDialog = new TextPrompt(Actions.Prompt);

            AddDialog(rootDialog);
            rootDialog.AddDialog(new List<IDialog>() {
                showEmailDialog,
                readEmailDialog,
                displayDialog,
                displayFilteredDialog,
                deleteEmailDialog,
                replyEmailDialog,
                forwardEmailDialog,
                promptDialog
            });
            InitialDialogId = "ShowEmailRootDialog";
        }

        private static IRecognizer CreateRecognizer()
        {
            return new LuisRecognizer(new LuisApplication()
            {
                Endpoint = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/1a441c29-5a3f-4615-9e6c-7473ffaa815c?verbose=true&timezoneOffset=-360&subscription-key=d5435d1a9181476cb1e4b192a1d4efec&q=",
                EndpointKey = "d5435d1a9181476cb1e4b192a1d4efec",
                ApplicationId = "1a441c29-5a3f-4615-9e6c-7473ffaa815c",
            });
        }

        //private async Task<DialogTurnResult> SaveLuisState1(DialogContext dc, System.Object options)
        //{
        //    dc.State.Dialog.Add("haha", "hahahaha");

        //    return new DialogTurnResult(DialogTurnStatus.Complete, options);
        //}

        //private async Task<DialogTurnResult> SaveLuisState2(DialogContext dc, System.Object options)
        //{
        //    var state = (string)dc.State.Dialog["haha"];
        //    options = state;
        //    return new DialogTurnResult(DialogTurnStatus.Complete, options);
        //}

        private async Task<DialogTurnResult> SaveDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var skillOptions = (EmailSkillDialogOptions)sc.Options;
            var dialogState = skillOptions?.DialogState != null ? skillOptions?.DialogState : new EmailStateBase();

            var state = await EmailStateAccessor.GetAsync(sc.Context);

            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = Services.CognitiveModelSets[locale];

            // Update state with email luis result and entities --- todo: use luis result in adaptive dialog
            var emailLuisResult = await localeConfig.LuisServices["email"].RecognizeAsync<EmailLuis>(sc.Context);
            state.LuisResult = emailLuisResult;
            localeConfig.LuisServices.TryGetValue("general", out var luisService);
            var luisResult = await luisService.RecognizeAsync<General>(sc.Context);
            state.GeneralLuisResult = luisResult;

            var newState = DigestLuisResult(sc, state.LuisResult, state.GeneralLuisResult, dialogState, true);
            sc.State.Dialog.Add(EmailStateKey, newState);

            return await sc.NextAsync();
        }

        protected async Task<DialogTurnResult> IfClearPagingConditionStep(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = (EmailStateBase)sc.State.Dialog[EmailStateKey];

                // Clear focus item
                state.UserSelectIndex = 0;

                // Clear search condition
                state.SenderName = null;
                state.SearchTexts = null;

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> PromptToHandle(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = (EmailStateBase)sc.State.Dialog[EmailStateKey];

                if (state.MessageList.Count == 1)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(ShowEmailResponses.ReadOutOnlyOnePrompt) });
                }
                else
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(ShowEmailResponses.ReadOutPrompt) });
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> PromptToReshow(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(ShowEmailResponses.ReadOutMorePrompt) });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> HandleReshow(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = (EmailStateBase)sc.State.Dialog[EmailStateKey];
                var userState = await EmailStateAccessor.GetAsync(sc.Context);
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.DialogState = state;

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    return await sc.BeginDialogAsync(Actions.Display, skillOptions);
                }

                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(EmailSharedResponses.CancellingMessage));
                return await sc.EndDialogAsync(false);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CheckRead(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;
                skillOptions.DialogState = (EmailStateBase)sc.State.Dialog[EmailStateKey];

                var userState = await EmailStateAccessor.GetAsync(sc.Context);
                var luisResult = userState.LuisResult;

                var topIntent = luisResult?.TopIntent().intent;
                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true);
                }

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(EmailSharedResponses.CancellingMessage));
                    return await sc.EndDialogAsync(false);
                }
                //else if (
                //    skillOptions.DialogState.UserSelectIndex != -1 ||
                //    (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true))
                else
                {
                    return await sc.BeginDialogAsync(Actions.Read, skillOptions);
                }

                //return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ReadEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = (EmailStateBase)sc.State.Dialog[EmailStateKey];
                var userState = await EmailStateAccessor.GetAsync(sc.Context);
                var skillOptions = (EmailSkillDialogOptions)sc.Options;

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                var luisResult = userState.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;
                var generalLuisResult = userState.GeneralLuisResult;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true);
                }

                await DigestFocusEmailAsync(sc);

                var message = state.Message.FirstOrDefault();
                if (message == null)
                {
                    state.Message.Add(state.MessageList[0]);
                    message = state.Message.FirstOrDefault();
                }

                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);

                if ((topIntent == EmailLuis.Intent.None
                    || topIntent == EmailLuis.Intent.SearchMessages
                    || (topIntent == EmailLuis.Intent.ReadAloud && !IsReadMoreIntent(generalTopIntent, sc.Context.Activity.Text))
                    || (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true))
                    && message != null)
                {
                    var senderIcon = await GetUserPhotoUrlAsync(sc.Context, message.Sender.EmailAddress);
                    var emailCard = new EmailCardData
                    {
                        Subject = message.Subject,
                        Sender = message.Sender.EmailAddress.Name,
                        EmailContent = message.BodyPreview,
                        EmailLink = message.WebLink,
                        ReceivedDateTime = message?.ReceivedDateTime == null
                            ? CommonStrings.NotAvailable
                            : message.ReceivedDateTime.Value.UtcDateTime.ToDetailRelativeString(userState.GetUserTimeZone()),
                        Speak = SpeakHelper.ToSpeechEmailDetailOverallString(message, userState.GetUserTimeZone()),
                        SenderIcon = senderIcon
                    };

                    emailCard = await ProcessRecipientPhotoUrl(sc.Context, emailCard, message.ToRecipients);

                    var tokens = new StringDictionary()
                    {
                        { "EmailDetails", SpeakHelper.ToSpeechEmailDetailString(message, userState.GetUserTimeZone()) },
                        { "EmailDetailsWithContent", SpeakHelper.ToSpeechEmailDetailString(message, userState.GetUserTimeZone(), true) },
                    };

                    var recipientCard = message.ToRecipients.Count() > 5 ? "DetailCard_RecipientMoreThanFive" : "DetailCard_RecipientLessThanFive";
                    var replyMessage = ResponseManager.GetCardResponse(
                        ShowEmailResponses.ReadOutMessage,
                        new Card("EmailDetailCard", emailCard),
                        tokens,
                        "items",
                        new List<Card>().Append(new Card(recipientCard, emailCard)));

                    // Set email as read.
                    var service = ServiceManager.InitMailService(userState.Token, userState.GetUserTimeZone(), userState.MailSourceType);
                    await service.MarkMessageAsReadAsync(message.Id);

                    await sc.Context.SendActivityAsync(replyMessage);
                }

                return await sc.NextAsync();
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> Display(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.DialogState = (EmailStateBase)sc.State.Dialog[EmailStateKey];
                return await sc.ReplaceDialogAsync(Actions.Display, skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> FilterEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                await SearchEmailsFromList(sc, cancellationToken);

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ShowFilteredEmails(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = (EmailStateBase)sc.State.Dialog[EmailStateKey];

                if (state.MessageList.Count > 0)
                {
                    if (state.Message.Count == 0)
                    {
                        state.Message.Add(state.MessageList[0]);
                        if (state.MessageList.Count > 1)
                        {
                            var importCount = 0;

                            foreach (var msg in state.MessageList)
                            {
                                if (msg.Importance.HasValue && msg.Importance.Value == Importance.High)
                                {
                                    importCount++;
                                }
                            }

                            await ShowMailList(sc, state.MessageList, state.MessageList.Count(), importCount, cancellationToken);
                            return await sc.NextAsync();
                        }
                        else if (state.MessageList.Count == 1)
                        {
                            return await sc.ReplaceDialogAsync(Actions.Read, options: sc.Options);
                        }
                    }
                    else
                    {
                        return await sc.ReplaceDialogAsync(Actions.Read, options: sc.Options);
                    }

                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(EmailSharedResponses.DidntUnderstandMessage));
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(EmailSharedResponses.EmailNotFound));
                }

                return await sc.EndDialogAsync(true);
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected EmailStateBase DigestLuisResult(DialogContext dc, EmailLuis luisResult, General generalLuisResult, EmailStateBase state, bool isBeginDialog)
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
                        case EmailLuis.Intent.CheckMessages:
                        case EmailLuis.Intent.SearchMessages:
                        case EmailLuis.Intent.ReadAloud:
                        case EmailLuis.Intent.ShowNext:
                        case EmailLuis.Intent.ShowPrevious:
                            {
                                // Get email search type
                                if (dc.Context.Activity.Text != null)
                                {
                                    var words = dc.Context.Activity.Text.Split(' ');
                                    {
                                        foreach (var word in words)
                                        {
                                            var lowerInput = word.ToLower();

                                            if (lowerInput.Contains(EmailCommonStrings.High) || lowerInput.Contains(EmailCommonStrings.Important))
                                            {
                                                state.IsImportant = true;
                                            }
                                            else if (lowerInput.Contains(EmailCommonStrings.Unread))
                                            {
                                                state.IsUnreadOnly = true;
                                            }
                                            else if (lowerInput.Contains(EmailCommonStrings.All))
                                            {
                                                state.IsUnreadOnly = false;
                                            }
                                        }
                                    }
                                }

                                if (entity.SenderName != null)
                                {
                                    state.SenderName = entity.SenderName[0];
                                }

                                if (entity.SearchTexts != null)
                                {
                                    state.SearchTexts = entity.SearchTexts[0];
                                }
                                else if (entity.EmailSubject != null)
                                {
                                    state.SearchTexts = entity.EmailSubject[0];
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