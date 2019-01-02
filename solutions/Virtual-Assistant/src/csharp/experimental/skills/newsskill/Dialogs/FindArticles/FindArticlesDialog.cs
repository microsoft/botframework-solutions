using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Skills;

namespace NewsSkill
{
    public class FindArticlesDialog : NewsDialog
    {
        private NewsClient _client;
        private FindArticlesResponses _responder = new FindArticlesResponses();

        public FindArticlesDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<NewsSkillState> accessor,
            IBotTelemetryClient telemetryClient)
            : base(nameof(FindArticlesDialog), services, accessor, telemetryClient)
        {
            Accessor = accessor;
            TelemetryClient = telemetryClient;

            var key = services.Properties["BingNewsKey"] ?? throw new Exception("The BingNewsKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");

            _client = new NewsClient((string)key);

            var findArticles = new WaterfallStep[]
            {
                GetQuery,
                ShowArticles,
            };

            AddDialog(new WaterfallDialog(nameof(FindArticlesDialog), findArticles));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
        }

        private async Task<DialogTurnResult> GetQuery(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context, () => new NewsSkillState());

            // Let's see if we have a topic
            if (state.LuisResult.Entities.topic != null)
            {
                // If we have a topic let's skip the topic prompt
                if (state.LuisResult.Entities.topic.Length > 0)
                {
                    return await sc.NextAsync(state.LuisResult.Entities.topic[0]);
                }
            }

            return await sc.PromptAsync(nameof(TextPrompt), new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale, FindArticlesResponses.TopicPrompt)
            });
        }

        private async Task<DialogTurnResult> ShowArticles(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var query = (string)sc.Result;
            var articles = await _client.GetNewsForTopic(query);
            await _responder.ReplyWith(sc.Context, FindArticlesResponses.ShowArticles, articles);

            return await sc.EndDialogAsync();
        }
    }
}