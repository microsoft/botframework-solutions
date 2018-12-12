using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NewsSkill
{
    public class FindArticlesDialog : ComponentDialog
    {
        private NewsClient _client;
        private FindArticlesResponses _responder = new FindArticlesResponses();

        public FindArticlesDialog(SkillConfiguration botServices) 
            : base(nameof(FindArticlesDialog))
        {
            var key = botServices.Properties["BingNewsKey"] ?? throw new Exception("The BingNewsKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");

            _client = new NewsClient((string)key);

            var findArticles = new WaterfallStep[]
            {
                GetQuery,
                ShowArticles,
            };

            AddDialog(new WaterfallDialog(nameof(FindArticlesDialog), findArticles));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
        }

        private async Task<DialogTurnResult> GetQuery(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, FindArticlesResponses.TopicPrompt)
            });
        }

        private async Task<DialogTurnResult> ShowArticles(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var query = (string)stepContext.Result;
            var articles = await _client.GetNewsForTopic(query);
            await _responder.ReplyWith(stepContext.Context, FindArticlesResponses.ShowArticles, articles);

            return await stepContext.EndDialogAsync();
        }
    }
}
