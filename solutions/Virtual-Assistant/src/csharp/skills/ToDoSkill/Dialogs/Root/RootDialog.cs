// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.Dialogs.Root
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::ToDoSkill.Dialogs.AddToDoTask;
    using global::ToDoSkill.Dialogs.DeleteToDoTask;
    using global::ToDoSkill.Dialogs.Greeting;
    using global::ToDoSkill.Dialogs.Shared;
    using global::ToDoSkill.Dialogs.Shared.Resources;
    using global::ToDoSkill.Dialogs.ShowToDoTasks;
    using global::ToDoSkill.ServiceClients;
    using Luis;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.AI.Luis;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Solutions;
    using Microsoft.Bot.Solutions.Dialogs;
    using Microsoft.Bot.Solutions.Extensions;
    using Microsoft.Bot.Solutions.Skills;
    using ToDoSkillLibrary.Dialogs.MarkToDoTask;

    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class RootDialog : RouterDialog
    {
        private const string CancelCode = "cancel";
        private bool skillMode = false;
        private ToDoSkillAccessors toDoSkillAccessors;
        private ToDoSkillResponses toDoSkillResponses;
        private ToDoSkillServices toDoSkillServices;
        private IToDoService toDoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RootDialog"/> class.
        /// </summary>
        /// <param name="skillMode">Skill mode.</param>
        /// <param name="toDoSkillServices">To Do skill service.</param>
        /// <param name="toDoSkillAccessors">To Do skill accessors.</param>
        /// <param name="toDoService">To Do provider service.</param>
        public RootDialog(bool skillMode, ToDoSkillServices toDoSkillServices, ToDoSkillAccessors toDoSkillAccessors, IToDoService toDoService)
            : base(nameof(RootDialog))
        {
            this.skillMode = skillMode;
            this.toDoSkillAccessors = toDoSkillAccessors;
            this.toDoService = toDoService;
            this.toDoSkillResponses = new ToDoSkillResponses();
            this.toDoSkillServices = toDoSkillServices;

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
            var state = await this.toDoSkillAccessors.ToDoSkillState.GetAsync(dc.Context, () => new ToDoSkillState());
            var dialogState = await this.toDoSkillAccessors.ConversationDialogState.GetAsync(dc.Context, () => new DialogState());

            ToDo luisResult = null;
            if (this.skillMode && state.LuisResultPassedFromSkill != null)
            {
                luisResult = (ToDo)state.LuisResultPassedFromSkill;
                state.LuisResultPassedFromSkill = null;
            }
            else if (this.toDoSkillServices?.LuisRecognizer != null)
            {
                luisResult = await this.toDoSkillServices.LuisRecognizer.RecognizeAsync<ToDo>(dc.Context, cancellationToken);
                if (luisResult == null)
                {
                    throw new Exception("ToDoSkill: Could not get Luis Recognizer result.");
                }
            }
            else
            {
                throw new Exception("ToDoSkill: Could not get Luis Recognizer result.");
            }

            state.LuisResult = luisResult;
            await ToDoHelper.DigestLuisResultAsync(dc.Context, this.toDoSkillAccessors);

            var skillOptions = new ToDoSkillDialogOptions
            {
                SkillMode = this.skillMode,
            };

            var intent = luisResult?.TopIntent().intent;

            switch (intent)
            {
                case ToDo.Intent.Greeting:
                    {
                        await dc.BeginDialogAsync(GreetingDialog.Name, skillOptions);
                        break;
                    }

                case ToDo.Intent.ShowToDo:
                case ToDo.Intent.Previous:
                case ToDo.Intent.Next:
                    {
                        await dc.BeginDialogAsync(ShowToDoTasksDialog.Name, skillOptions);
                        break;
                    }

                case ToDo.Intent.AddToDo:
                    {
                        await dc.BeginDialogAsync(AddToDoTaskDialog.Name, skillOptions);
                        break;
                    }

                case ToDo.Intent.MarkToDo:
                    {
                        await dc.BeginDialogAsync(MarkToDoTaskDialog.Name, skillOptions);
                        break;
                    }

                case ToDo.Intent.DeleteToDo:
                    {
                        await dc.BeginDialogAsync(DeleteToDoTaskDialog.Name, skillOptions);
                        break;
                    }

                case ToDo.Intent.None:
                    {
                        await this.toDoSkillResponses.ReplyWith(dc.Context, ToDoSkillResponses.Confused);
                        if (skillMode)
                        {
                            await CompleteAsync(dc);
                        }

                        break;
                    }

                default:
                    {
                        await dc.Context.SendActivityAsync("This feature is not yet available in the To Do Skill. Please try asking something else.");
                        if (skillMode)
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
            var response = dc.Context.Activity.CreateReply();
            response.Type = ActivityTypes.EndOfConversation;

            await dc.Context.SendActivityAsync(response);

            // End active dialog
            await dc.EndDialogAsync(result);
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
                var state = await this.toDoSkillAccessors.ToDoSkillState.GetAsync(dc.Context, () => new ToDoSkillState());
                var skillMetadata = dc.Context.Activity.Value as SkillMetadata;

                if (skillMetadata != null)
                {
                    var luisService = skillMetadata.LuisService;
                    var luisApp = new LuisApplication(luisService.AppId, luisService.SubscriptionKey, luisService.GetEndpoint());
                    toDoSkillServices.LuisRecognizer = new LuisRecognizer(luisApp);

                    state.LuisResultPassedFromSkill = skillMetadata.LuisResult;
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
            var reply = activity.CreateReply(ToDoBotResponses.ToDoWelcomeMessage);
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
                if (!skillMode && dc.Context.Activity.Type == ActivityTypes.Message)
                {
                    var luisResult = await this.toDoSkillServices.LuisRecognizer.RecognizeAsync<ToDo>(dc.Context, cancellationToken);
                    var topIntent = luisResult.TopIntent().intent;

                    // check intent
                    switch (topIntent)
                    {
                        case ToDo.Intent.Cancel:
                            {
                                return await this.OnCancel(dc);
                            }

                        case ToDo.Intent.Help:
                            {
                                return await this.OnHelp(dc);
                            }

                        case ToDo.Intent.Logout:
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
            var cancelling = dc.Context.Activity.CreateReply(ToDoBotResponses.CancellingMessage);
            await dc.Context.SendActivityAsync(cancelling);

            var state = await this.toDoSkillAccessors.ToDoSkillState.GetAsync(dc.Context);
            state.Clear();
            await dc.CancelAllDialogsAsync();

            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            var helpMessage = dc.Context.Activity.CreateReply(ToDoBotResponses.ToDoHelpMessage);
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
            // await adapter.SignOutUserAsync(dc.Context, "googleapi", default(CancellationToken)).ConfigureAwait(false);
            await adapter.SignOutUserAsync(dc.Context, "msgraph");
            var logoutMessage = dc.Context.Activity.CreateReply(ToDoBotResponses.LogOut);
            await dc.Context.SendActivityAsync(logoutMessage);

            var state = await this.toDoSkillAccessors.ToDoSkillState.GetAsync(dc.Context);
            state.Clear();

            return InterruptionAction.StartedDialog;
        }

        private void RegisterDialogs()
        {
            this.AddDialog(new GreetingDialog(this.toDoSkillServices, this.toDoService, this.toDoSkillAccessors));
            this.AddDialog(new ShowToDoTasksDialog(this.toDoSkillServices, this.toDoService, this.toDoSkillAccessors));
            this.AddDialog(new AddToDoTaskDialog(this.toDoSkillServices, this.toDoService, this.toDoSkillAccessors));
            this.AddDialog(new MarkToDoTaskDialog(this.toDoSkillServices, this.toDoService, this.toDoSkillAccessors));
            this.AddDialog(new DeleteToDoTaskDialog(this.toDoSkillServices, this.toDoService, this.toDoSkillAccessors));
        }
    }
}