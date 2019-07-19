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
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace EmailSkill.Dialogs
{
    public class ShowEmailDialog : EmailSkillDialogBase
    {
        private ResourceMultiLanguageGenerator _lgMultiLangEngine;

        public ShowEmailDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            DeleteEmailDialog deleteEmailDialog,
            ReplyEmailDialog replyEmailDialog,
            ForwardEmailDialog forwardEmailDialog,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(ShowEmailDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            // combine path for cross platform support
            _lgMultiLangEngine = new ResourceMultiLanguageGenerator("ShowEmail.lg");

            var skillOptions = new EmailSkillDialogOptions
            {
                SubFlowMode = true
            };

            var rootDialog = new AdaptiveDialog("ShowEmailRootDialog")
            {
                Recognizer = CreateRecognizer(),
                Rules = new List<IRule>()
                {
                    new IntentRule("Forward") { Steps = new List<IDialog>() { new BeginDialog(nameof(ForwardEmailDialog), options: skillOptions) } },
                    new IntentRule("Delete") { Steps = new List<IDialog>() { new BeginDialog(nameof(DeleteEmailDialog), options: skillOptions) } },
                    new IntentRule("Reply") { Steps = new List<IDialog>() { new BeginDialog(nameof(ReplyEmailDialog), options: skillOptions) } },
                    new IntentRule("CheckMessages") { Steps = new List<IDialog>() { new BeginDialog(Actions.DisplayFiltered, options: skillOptions) } },
                    new IntentRule("SearchMessages") { Steps = new List<IDialog>() { new BeginDialog(Actions.DisplayFiltered, options: skillOptions) } },
                    new IntentRule("ShowNext") { Steps = new List<IDialog>() { new ReplaceDialog(Actions.Show, options: skillOptions) } },
                    new IntentRule("ShowPrevious") { Steps = new List<IDialog>() { new ReplaceDialog(Actions.Show, options: skillOptions) } },
                },

                Steps = new List<IDialog>()
                {
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
                PromptToReshow,
                HandleReshow
            };

            var displayFilteredEmail = new WaterfallStep[]
            {
                SaveDialogState,
                FilterEmail,
                ShowFilteredEmails,
            };

            // Define the conversation flow using a waterfall model.
            var showEmailDialog = new EmailWaterfallDialog(Actions.Show, showEmail, EmailStateAccessor) { TelemetryClient = telemetryClient };
            var readEmailDialog = new EmailWaterfallDialog(Actions.Read, readEmail, EmailStateAccessor) { TelemetryClient = telemetryClient };
            var displayDialog = new EmailWaterfallDialog(Actions.Display, displayEmail, EmailStateAccessor) { TelemetryClient = telemetryClient };
            var displayFilteredDialog = new EmailWaterfallDialog(Actions.DisplayFiltered, displayFilteredEmail, EmailStateAccessor) { TelemetryClient = telemetryClient };
            var promptDialog = new TextPrompt(Actions.Prompt);

            AddDialog(rootDialog);
            rootDialog.AddDialog(new List<IDialog>() { showEmailDialog, readEmailDialog, displayDialog, displayFilteredDialog, deleteEmailDialog, replyEmailDialog, forwardEmailDialog, promptDialog });
            InitialDialogId = "ShowEmailRootDialog";
        }

        private static IRecognizer CreateRecognizer()
        {
            return new LuisRecognizer(new LuisApplication()
            {
                Endpoint = "https://westus.api.cognitive.microsoft.com/",
                EndpointKey = "21c62c8c1a864552b4396511023e7fe3",
                ApplicationId = "b63d15d6-213f-46f5-adf5-da60d8b6d835",
            });
        }

        private async Task<DialogTurnResult> SaveDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var skillOptions = (EmailSkillDialogOptions)sc.Options;
            var dialogState = skillOptions?.DialogState != null ? skillOptions?.DialogState : new EmailStateBase();

            var state = await EmailStateAccessor.GetAsync(sc.Context);

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
                var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ReadOut]", new { messageList = state.MessageList });
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = activity as Activity });
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
                var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ReadOutMore]", null);
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = activity as Activity });
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

                var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[Cancel]", null);
                await sc.Context.SendActivityAsync(activity);
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
                    var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[Cancel]", null);
                    await sc.Context.SendActivityAsync(activity);
                    return await sc.EndDialogAsync(false);
                }
                else
                {
                    return await sc.BeginDialogAsync(Actions.Read, skillOptions);
                }
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

                if ((topIntent == emailLuis.Intent.None
                    || topIntent == emailLuis.Intent.SearchMessages
                    || (topIntent == emailLuis.Intent.ReadAloud && !IsReadMoreIntent(generalTopIntent, sc.Context.Activity.Text))
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
                        SenderIcon = senderIcon,
                        RecipientsCount = message.ToRecipients.Count()
                    };

                    emailCard = await ProcessRecipientPhotoUrl(sc.Context, emailCard, message.ToRecipients);

                    var replyArg = new
                        {
                            emailDetails = SpeakHelper.ToSpeechEmailDetailString(message, userState.GetUserTimeZone()),
                            emailDetailsWithContent = SpeakHelper.ToSpeechEmailDetailString(message, userState.GetUserTimeZone(), true)
                        };

                    var emailDetailCard = await LGHelper.GenerateAdaptiveCardAsync(
                        _lgMultiLangEngine,
                        sc.Context,
                        "[ReadOutMessages(emailDetails, emailDetailsWithContent)]",
                        replyArg,
                        "[EmailDetailCard(emailDetails)]",
                        new { emailDetails = emailCard });

                    // Set email as read.
                    var service = ServiceManager.InitMailService(userState.Token, userState.GetUserTimeZone(), userState.MailSourceType);
                    await service.MarkMessageAsReadAsync(message.Id);

                    await sc.Context.SendActivityAsync(emailDetailCard);
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
    }
}