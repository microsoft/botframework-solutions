using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using BingSearchSkill.Models;
using BingSearchSkill.Responses.Sample;
using BingSearchSkill.Services;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;

namespace BingSearchSkill.Dialogs
{
    public class SearchDialog : SkillDialogBase
    {
        private BotServices _services;
        private IStatePropertyAccessor<SkillState> _stateAccessor;

        public SearchDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IBotTelemetryClient telemetryClient)
            : base(nameof(SearchDialog), settings, services, responseManager, conversationState, telemetryClient)
        {
            _stateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));
            _services = services;
            Settings = settings;

            var sample = new WaterfallStep[]
            {
                // NOTE: Uncomment these lines to include authentication steps to this dialog
                // GetAuthToken,
                // AfterGetAuthToken,
                PromptForQuestion,
                ShowResult,
                End,
            };

            AddDialog(new WaterfallDialog(nameof(SearchDialog), sample));
            AddDialog(new TextPrompt(DialogIds.NamePrompt));

            InitialDialogId = nameof(SearchDialog);
        }

        private async Task<DialogTurnResult> PromptForQuestion(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // NOTE: Uncomment the following lines to access LUIS result for this turn.
            // var state = await ConversationStateAccessor.GetAsync(stepContext.Context);
            // var intent = state.LuisResult.TopIntent().intent;
            // var entities = state.LuisResult.Entities;

            var prompt = ResponseManager.GetResponse(SampleResponses.AskEntityPrompt);
            return await stepContext.PromptAsync(DialogIds.NamePrompt, new PromptOptions { Prompt = prompt });
        }

        private async Task<DialogTurnResult> ShowResult(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context);
            var intent = state.LuisResult.TopIntent().intent;
            var entities = state.LuisResult.Entities.CelebrityNamePatten;

            var client = new BingSearchClient("0cc713be514e4ba48eae87465c4a29e4");
            var entitiesResult = await client.GetSearchResult(entities[0]);

            var tokens = new StringDictionary
            {
                { "Name", entitiesResult.Value[0].Description },
            };

            var response = ResponseManager.GetResponse(SampleResponses.EntityKnowledge, tokens);
            await stepContext.Context.SendActivityAsync(response);

            return await stepContext.NextAsync();
        }

        private Task<DialogTurnResult> End(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return stepContext.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }
    }
}
