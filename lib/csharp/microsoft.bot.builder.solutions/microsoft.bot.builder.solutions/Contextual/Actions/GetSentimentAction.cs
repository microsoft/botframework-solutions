using Microsoft.Bot.Builder.Solutions.Contextual.Services;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Actions
{
    public class GetSentimentAction : ISkillContextualAction
    {
        public GetSentimentAction()
        {
            BeforeTurnAction = async turnContext =>
            {
                await AnalysisSentimentActionAsync(turnContext);
            };
        }

        private async Task AnalysisSentimentActionAsync(ITurnContext turnContext)
        {
            if (turnContext.Activity.Text != null)
            {
                var sentimentService = await SentimentService.GetSentimentAnalysisAsync(turnContext.Activity.Text, "en");

                var activity = new Activity()
                {
                    Type = ActivityTypes.Trace,
                    Text = sentimentService.ToString(),
                };
                await turnContext.SendActivityAsync(activity);
            }
        }
    }
}
