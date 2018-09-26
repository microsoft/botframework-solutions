// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.Shared;
using EmailSkill.Dialogs.Shared.Resources;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;

namespace EmailSkill
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class RootDialog : RouterDialog
    {
        private const string CancelCode = "cancel";
        private bool _skillMode = false;
        private EmailSkillAccessors _accessors;
        private EmailSkillResponses _responder;
        private EmailSkillServices _services;
        private IMailSkillServiceManager _serviceManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="RootDialog"/> class.
        /// </summary>
        /// <param name="skillMode">Skill mode.</param>
        /// <param name="services">Email skill service.</param>
        /// <param name="emailSkillAccessors">Email skill accessors.</param>
        /// <param name="serviceManager">Email provider service.</param>
        public RootDialog(bool skillMode, EmailSkillServices services, EmailSkillAccessors emailSkillAccessors, IMailSkillServiceManager serviceManager)
            : base(nameof(RootDialog))
        {
            this._skillMode = skillMode;
            this._accessors = emailSkillAccessors;
            this._serviceManager = serviceManager;
            this._responder = new EmailSkillResponses();
            this._services = services;

            // Initialise dialogs
            this.RegisterDialogs();
        }

        /// <summary>
        /// Run every turn of the conversation. Handles orchestration of messages.
        /// </summary>
        /// <param name="dc">Current dialog context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Completed Task.</returns>
        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Get the conversation state from the turn context
            var state = await this._accessors.EmailSkillState.GetAsync(dc.Context, () => new EmailSkillState());
            var dialogState = await this._accessors.ConversationDialogState.GetAsync(dc.Context, () => new DialogState());

            Email luisResult;

            // If invoked by a Skill we get the Luis IRecognizerConvert passed to use so we don't have to do that locally
            if (this._skillMode && state.LuisResultPassedFromSkill != null)
            {
                luisResult = (Email)state.LuisResultPassedFromSkill;
                state.LuisResultPassedFromSkill = null;
            }
            else if (this._services?.LuisRecognizer != null)
            {
                luisResult = await this._services.LuisRecognizer.RecognizeAsync<Email>(dc.Context, cancellationToken);
            }
            else
            {
                throw new Exception("EmailSkill: Could not get Luis Recognizer result.");
            }

            await EmailSkillHelper.DigestEmailLuisResult(dc.Context, this._accessors, luisResult);

            var topLuisIntent = luisResult?.TopIntent().intent;

            SkillDialogOptions options = new SkillDialogOptions { SkillMode = this._skillMode };
            switch (topLuisIntent)
            {
                case Email.Intent.SendEmail:
                    {
                        await dc.BeginDialogAsync(SendEmailDialog.Name, options);
                        break;
                    }

                case Email.Intent.Forward:
                    {
                        await dc.BeginDialogAsync(ForwardEmailDialog.Name, options);
                        break;
                    }

                case Email.Intent.Reply:
                    {
                        await dc.BeginDialogAsync(ReplyEmailDialog.Name, options);
                        break;
                    }

                case Email.Intent.Help:
                    {
                        await dc.BeginDialogAsync(HelpDialog.Name, options);
                        break;
                    }

                case Email.Intent.SearchMessages:
                case Email.Intent.ShowNext:
                case Email.Intent.ShowPrevious:
                case Email.Intent.CheckMessages:
                    {
                        await dc.BeginDialogAsync(ShowEmailDialog.Name, options);
                        break;
                    }

                case Email.Intent.None:
                    {
                        await this._responder.ReplyWith(dc.Context, EmailSkillResponses.Confused);
                        if (_skillMode)
                        {
                            await CompleteAsync(dc);
                        }

                        break;
                    }

                default:
                    {
                        await dc.Context.SendActivityAsync("This feature is not yet available in the Email Skill. Please try asking something else.");
                        if (_skillMode)
                        {
                            await CompleteAsync(dc);
                        }

                        break;
                    }
            }
        }

        /// <summary>
        /// Complete the conversation.
        /// </summary>
        /// <param name="dc">Current dialog context.</param>
        /// <param name="result">Result returned when dialog completed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Completed Task.</returns>
        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (result?.Result != null && result.Result.ToString() == "StartNew")
            {
                await this.RouteAsync(dc);
            }
            else
            {
                var response = dc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.EndOfConversation;

                await dc.Context.SendActivityAsync(response);

                // End active dialog
                await dc.EndDialogAsync(result);
            }
        }

        /// <summary>
        /// On event.
        /// </summary>
        /// <param name="dc">Current dialog context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Completed Task.</returns>
        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dc.Context.Activity.Name == "skillBegin")
            {
                var state = await this._accessors.EmailSkillState.GetAsync(dc.Context, () => new EmailSkillState());
                var skillMetadata = dc.Context.Activity.Value as SkillMetadata;

                if (skillMetadata != null)
                {
                    var luisService = skillMetadata.LuisService;
                    var luisApp = new LuisApplication(luisService.AppId, luisService.SubscriptionKey, luisService.GetEndpoint());
                    _services.LuisRecognizer = new LuisRecognizer(luisApp);

                    state.LuisResultPassedFromSkill = skillMetadata.LuisResult;
                    if (state.UserInfo == null)
                    {
                        state.UserInfo = new EmailSkillState.UserInformation();
                    }

                    // Each skill is configured to explicitly request certain items to be passed across
                    if (skillMetadata.Parameters.TryGetValue("IPA.Timezone", out var timezone))
                    {
                        // we have a timezone
                        state.UserInfo.Timezone = (TimeZoneInfo)timezone;
                    }
                    else
                    {
                        // TODO Error handling if parameter isn't passed (or default)
                    }
                }
            }
            else if (dc.Context.Activity.Name == "tokens/response")
            {
                // Auth dialog completion
                var result = await dc.ContinueDialogAsync();

                // If the dialog completed when we sent the token, end the skill conversation
                if (result.Status != DialogTurnStatus.Waiting)
                {
                    var response = dc.Context.Activity.CreateReply();
                    response.Type = ActivityTypes.EndOfConversation;

                    await dc.Context.SendActivityAsync(response);
                }
            }
        }

        /// <summary>
        /// On start.
        /// </summary>
        /// <param name="dc">Current dialog context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Completed Task.</returns>
        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = dc.Context.Activity;
            var reply = activity.CreateReply(EmailBotResponses.EmailWelcomeMessage);
            await dc.Context.SendActivityAsync(reply);
        }

        /// <summary>
        /// On interrupt.
        /// </summary>
        /// <param name="dc">Current dialog context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Completed Task.</returns>
        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dc.Context.Activity.Text?.ToLower() == CancelCode)
            {
                await CompleteAsync(dc);

                return InterruptionAction.StartedDialog;
            }
            else
            {
                if (!this._skillMode && dc.Context.Activity.Type == ActivityTypes.Message)
                {
                    var luisResult = await this._services.LuisRecognizer.RecognizeAsync<Email>(dc.Context, cancellationToken);
                    var topIntent = luisResult.TopIntent().intent;

                    // check intent
                    switch (topIntent)
                    {
                        case Email.Intent.Cancel:
                            {
                                return await this.OnCancel(dc);
                            }

                        case Email.Intent.Help:
                            {
                                return await this.OnHelp(dc);
                            }

                        case Email.Intent.Logout:
                            {
                                return await this.OnLogout(dc);
                            }
                    }
                }

                return InterruptionAction.NoAction;
            }
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            var cancelling = dc.Context.Activity.CreateReply(EmailBotResponses.CancellingMessage);
            await dc.Context.SendActivityAsync(cancelling);

            await this._accessors.EmailSkillState.SetAsync(dc.Context, new EmailSkillState());

            await dc.CancelAllDialogsAsync();

            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            var helpMessage = dc.Context.Activity.CreateReply(EmailSkillResponses.Help);
            await dc.Context.SendActivityAsync(helpMessage);

            return InterruptionAction.MessageSentToUser;
        }

        private async Task<InterruptionAction> OnLogout(DialogContext dc)
        {
            BotFrameworkAdapter adapter;
            var supported = dc.Context.Adapter is BotFrameworkAdapter;
            if (!supported)
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
            else
            {
                adapter = (BotFrameworkAdapter)dc.Context.Adapter;
            }

            await dc.CancelAllDialogsAsync();

            // Sign out user
            await adapter.SignOutUserAsync(dc.Context, "msgraph");
            var logoutMessage = dc.Context.Activity.CreateReply(EmailBotResponses.LogOut);
            await dc.Context.SendActivityAsync(logoutMessage);

            await this._accessors.EmailSkillState.SetAsync(dc.Context, new EmailSkillState());

            return InterruptionAction.StartedDialog;
        }

        private void RegisterDialogs()
        {
            // Email
            this.AddDialog(new ForwardEmailDialog(this._services, this._accessors, this._serviceManager));
            this.AddDialog(new SendEmailDialog(this._services, this._accessors, this._serviceManager));
            this.AddDialog(new ShowEmailDialog(this._services, this._accessors, this._serviceManager));
            this.AddDialog(new ReplyEmailDialog(this._services, this._accessors, this._serviceManager));
        }
    }
}
