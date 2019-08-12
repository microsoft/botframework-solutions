// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Prompts;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Graph;

namespace ITSMSkill.Dialogs
{
    public class SkillDialogBase : ComponentDialog
    {
        public SkillDialogBase(
             string dialogId,
             BotSettings settings,
             BotServices services,
             ResponseManager responseManager,
             ConversationState conversationState,
             IServiceManager serviceManager,
             IBotTelemetryClient telemetryClient)
             : base(dialogId)
        {
            Settings = settings;
            Services = services;
            ResponseManager = responseManager;
            StateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));
            ServiceManager = serviceManager;
            TelemetryClient = telemetryClient;

            // NOTE: Uncomment the following if your skill requires authentication
            if (!settings.OAuthConnections.Any())
            {
                throw new Exception("You must configure an authentication connection before using this component.");
            }

            AddDialog(new MultiProviderAuthDialog(settings.OAuthConnections));

            var setDescription = new WaterfallStep[]
            {
                CheckDescription,
                InputDescription,
                SetDescription
            };

            var setUrgency = new WaterfallStep[]
            {
                CheckUrgency,
                InputUrgency,
                SetUrgency
            };

            var attributesForUpdate = new AttributeType[] { AttributeType.Description, AttributeType.Urgency };

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new AttributePrompt(Actions.UpdateAttributeNoYesNo, attributesForUpdate, false));
            AddDialog(new AttributePrompt(Actions.UpdateAttributeHasYesNo, attributesForUpdate, true));
            AddDialog(new WaterfallDialog(Actions.SetDescription, setDescription));
            AddDialog(new WaterfallDialog(Actions.SetUrgency, setUrgency));
        }

        protected BotSettings Settings { get; set; }

        protected BotServices Services { get; set; }

        protected IStatePropertyAccessor<SkillState> StateAccessor { get; set; }

        protected ResponseManager ResponseManager { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        protected async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions());
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

        protected async Task<DialogTurnResult> AfterGetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
                // When the token is cached we get a TokenResponse object.
                if (sc.Result is ProviderTokenResponse providerTokenResponse)
                {
                    var state = await StateAccessor.GetAsync(sc.Context);
                    state.Token = providerTokenResponse.TokenResponse;
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

        protected async Task<DialogTurnResult> CheckId(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            if (string.IsNullOrEmpty(state.Id))
            {
                return await sc.NextAsync(false);
            }
            else
            {
                var replacements = new StringDictionary
                {
                    { "Id", state.Id }
                };

                var options = new PromptOptions()
                {
                    Prompt = ResponseManager.GetResponse(SharedResponses.ConfirmId, replacements)
                };

                return await sc.PromptAsync(nameof(ConfirmPrompt), options);
            }
        }

        protected async Task<DialogTurnResult> InputId(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            if (!(bool)sc.Result || string.IsNullOrEmpty(state.Id))
            {
                var options = new PromptOptions()
                {
                    Prompt = ResponseManager.GetResponse(SharedResponses.InputId)
                };

                return await sc.PromptAsync(nameof(TextPrompt), options);
            }
            else
            {
                return await sc.NextAsync(state.Id);
            }
        }

        protected async Task<DialogTurnResult> SetId(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            state.Id = (string)sc.Result;
            return await sc.NextAsync();
        }

        protected async Task<DialogTurnResult> CheckAttributeNoConfirm(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            if (state.AttributeType == AttributeType.None)
            {
                var options = new PromptOptions()
                {
                    Prompt = ResponseManager.GetResponse(SharedResponses.InputAttribute)
                };

                return await sc.PromptAsync(Actions.UpdateAttributeNoYesNo, options);
            }
            else
            {
                return await sc.NextAsync(state.AttributeType);
            }
        }

        protected async Task<DialogTurnResult> SetAttribute(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            state.AttributeType = (AttributeType)sc.Result;
            return await sc.NextAsync();
        }

        protected async Task<DialogTurnResult> CheckDescription(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            if (string.IsNullOrEmpty(state.TicketDescription))
            {
                return await sc.NextAsync(false);
            }
            else
            {
                var replacements = new StringDictionary
                {
                    { "Description", state.TicketDescription }
                };

                var options = new PromptOptions()
                {
                    Prompt = ResponseManager.GetResponse(SharedResponses.ConfirmDescription, replacements)
                };

                return await sc.PromptAsync(nameof(ConfirmPrompt), options);
            }
        }

        protected async Task<DialogTurnResult> InputDescription(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            if (!(bool)sc.Result || string.IsNullOrEmpty(state.TicketDescription))
            {
                var options = new PromptOptions()
                {
                    Prompt = ResponseManager.GetResponse(SharedResponses.InputDescription)
                };

                return await sc.PromptAsync(nameof(TextPrompt), options);
            }
            else
            {
                return await sc.NextAsync(state.TicketDescription);
            }
        }

        protected async Task<DialogTurnResult> SetDescription(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            state.TicketDescription = (string)sc.Result;
            return await sc.NextAsync();
        }

        protected async Task<DialogTurnResult> CheckUrgency(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            if (state.UrgencyLevel == UrgencyLevel.None)
            {
                return await sc.NextAsync(false);
            }
            else
            {
                var replacements = new StringDictionary
                {
                    { "Urgency", state.UrgencyLevel.ToString() }
                };

                var options = new PromptOptions()
                {
                    Prompt = ResponseManager.GetResponse(SharedResponses.ConfirmUrgency, replacements)
                };

                return await sc.PromptAsync(nameof(ConfirmPrompt), options);
            }
        }

        protected async Task<DialogTurnResult> InputUrgency(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            if (!(bool)sc.Result || state.UrgencyLevel == UrgencyLevel.None)
            {
                var options = new PromptOptions()
                {
                    Prompt = ResponseManager.GetResponse(SharedResponses.InputUrgency),
                    Choices = new List<Choice>()
                    {
                        new Choice()
                        {
                            Value = UrgencyLevel.Low.ToString()
                        },
                        new Choice()
                        {
                            Value = UrgencyLevel.Medium.ToString()
                        },
                        new Choice()
                        {
                            Value = UrgencyLevel.High.ToString()
                        }
                    }
                };

                return await sc.PromptAsync(nameof(ChoicePrompt), options);
            }
            else
            {
                return await sc.NextAsync(new FoundChoice()
                {
                    Value = state.UrgencyLevel.ToString()
                });
            }
        }

        protected async Task<DialogTurnResult> SetUrgency(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            state.UrgencyLevel = Enum.Parse<UrgencyLevel>(((FoundChoice)sc.Result).Value);
            return await sc.NextAsync();
        }

        // Validators
        protected Task<bool> TokenResponseValidator(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
        {
            var activity = pc.Recognized.Value;
            if (activity != null && activity.Type == ActivityTypes.Event)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        protected Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> promptContext, CancellationToken cancellationToken)
        {
            var token = promptContext.Recognized.Value;
            if (token != null)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        // Helpers
        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.ErrorMessage));

            // clear state
            var state = await StateAccessor.GetAsync(sc.Context);
            state.Clear();
        }

        protected TicketCard ConvertTicket(Ticket ticket)
        {
            var card = new TicketCard()
            {
                Description = ticket.Description,
                UrgencyLevel = $"{SharedStrings.Urgency}{ConvertUrgencyLevel(ticket.Urgency)}",
                State = $"{SharedStrings.TicketState}{ConvertTicketState(ticket.State)}",
                OpenedTime = $"{SharedStrings.OpenedAt}{ticket.OpenedTime.ToString()}",
                Id = ticket.Id,
                ResolvedReason = ticket.ResolvedReason,
                Speak = ticket.Description
            };
            return card;
        }

        protected KnowledgeCard ConvertKnowledge(Knowledge knowledge)
        {
            var card = new KnowledgeCard()
            {
                Id = knowledge.Id,
                Title = knowledge.Title,
                UpdatedTime = $"{SharedStrings.UpdatedAt}{knowledge.UpdatedTime.ToString()}",
                Content = knowledge.Content,
                Speak = knowledge.Title
            };
            return card;
        }

        protected string ConvertUrgencyLevel(UrgencyLevel urgency)
        {
            switch (urgency)
            {
                case UrgencyLevel.Low: return SharedStrings.UrgencyLow;
                case UrgencyLevel.Medium: return SharedStrings.UrgencyMedium;
                case UrgencyLevel.High: return SharedStrings.UrgencyHigh;
                default: return string.Empty;
            }
        }

        protected string ConvertTicketState(TicketState state)
        {
            switch (state)
            {
                case TicketState.New: return SharedStrings.TicketStateNew;
                case TicketState.InProgress: return SharedStrings.TicketStateInProgress;
                case TicketState.OnHold: return SharedStrings.TicketStateOnHold;
                case TicketState.Resolved: return SharedStrings.TicketStateResolved;
                case TicketState.Closed: return SharedStrings.TicketStateClosed;
                case TicketState.Canceled: return SharedStrings.TicketStateCanceled;
                default: return string.Empty;
            }
        }

        protected string GetDivergedCardName(ITurnContext turnContext, string card)
        {
            if (Channel.GetChannelId(turnContext) == Channels.Msteams)
            {
                return card + ".1.0";
            }
            else
            {
                return card;
            }
        }

        protected class Actions
        {
            public const string CreateTicket = "CreateTicket";
            public const string SetDescription = "SetDescription";
            public const string SetUrgency = "SetUrgency";
            public const string UpdateTicket = "UpdateTicket";
            public const string UpdateAttribute = "UpdateAttribute";
            public const string UpdateAttributeNoYesNo = "UpdateAttributeNoYesNo";
            public const string UpdateAttributeHasYesNo = "UpdateAttributeHasYesNo";
        }
    }
}