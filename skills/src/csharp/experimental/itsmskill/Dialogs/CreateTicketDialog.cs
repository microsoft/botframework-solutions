using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Responses.CreateTicket;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector;

namespace ITSMSkill.Dialogs
{
    public class CreateTicketDialog : SkillDialogBase
    {
        public CreateTicketDialog(
             BotSettings settings,
             BotServices services,
             ResponseManager responseManager,
             ConversationState conversationState,
             IServiceManager serviceManager,
             IBotTelemetryClient telemetryClient)
            : base(nameof(CreateTicketDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient)
        {
            var createTicket = new WaterfallStep[]
            {
                CheckDescription,
                InputDescription,
                SetDescription,
                GetAuthToken,
                AfterGetAuthToken,
                DisplayExisting,
                IfExistingSolve,
                CheckUrgency,
                InputUrgency,
                SetUrgency,
                GetAuthToken,
                AfterGetAuthToken,
                CreateTicket
            };
            AddDialog(new WaterfallDialog(Actions.CreateTicket, createTicket) { TelemetryClient = telemetryClient });

            InitialDialogId = Actions.CreateTicket;
        }

        public async Task<DialogTurnResult> CheckDescription(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

        public async Task<DialogTurnResult> InputDescription(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

        public async Task<DialogTurnResult> SetDescription(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            state.TicketDescription = (string)sc.Result;
            return await sc.NextAsync();
        }

        public async Task<DialogTurnResult> DisplayExisting(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            if (state.Token == null)
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.AuthFailed));
                return await sc.EndDialogAsync();
            }

            var management = ServiceManager.CreateManagement(Settings, state.Token);
            var result = await management.SearchKnowledge(state.TicketDescription);

            if (!result.Success)
            {
                var errorReplacements = new StringDictionary
                {
                    { "Error", result.ErrorMessage }
                };
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.ServiceFailed, errorReplacements));
                return await sc.EndDialogAsync();
            }

            if (result.Knowledges == null || result.Knowledges.Length == 0)
            {
                return await sc.NextAsync(false);
            }
            else
            {
                var cards = new List<Card>();
                foreach (var knowledge in result.Knowledges)
                {
                    cards.Add(new Card()
                    {
                        Name = GetDivergedCardName(sc.Context, "Knowledge"),
                        Data = ConvertKnowledge(knowledge)
                    });
                }

                var options = new PromptOptions()
                {
                    Prompt = ResponseManager.GetCardResponse(SharedResponses.IfExistingSolve, cards)
                };

                // Workaround. In teams, HeroCard will be used for prompt and adaptive card could not be shown. So send them separatly
                if (Channel.GetChannelId(sc.Context) == Channels.Msteams)
                {
                    await sc.Context.SendActivityAsync(options.Prompt);
                    options.Prompt = null;
                }

                return await sc.PromptAsync(nameof(ConfirmPrompt), options);
            }
        }

        public async Task<DialogTurnResult> IfExistingSolve(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if ((bool)sc.Result)
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.ExistingSolve));
                return await sc.EndDialogAsync();
            }
            else
            {
                return await sc.NextAsync();
            }
        }

        public async Task<DialogTurnResult> CheckUrgency(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

        public async Task<DialogTurnResult> InputUrgency(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

        public async Task<DialogTurnResult> SetUrgency(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            Enum.TryParse(((FoundChoice)sc.Result).Value, out UrgencyLevel urgency);
            state.UrgencyLevel = urgency;
            return await sc.NextAsync();
        }

        public async Task<DialogTurnResult> CreateTicket(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            if (state.Token == null)
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.AuthFailed));
                return await sc.EndDialogAsync();
            }

            var management = ServiceManager.CreateManagement(Settings, state.Token);
            var result = await management.CreateTicket(state.TicketDescription, state.UrgencyLevel);

            if (!result.Success)
            {
                var errorReplacements = new StringDictionary
                {
                    { "Error", result.ErrorMessage }
                };
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.ServiceFailed, errorReplacements));
                return await sc.EndDialogAsync();
            }

            var card = new Card()
            {
                Name = GetDivergedCardName(sc.Context, "Ticket"),
                Data = ConvertTicket(result.Ticket)
            };

            await sc.Context.SendActivityAsync(ResponseManager.GetCardResponse(CreateTicketResponses.TicketCreated, card, null));
            return await sc.NextAsync();
        }
    }
}
