using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Utilities
{
    public class LGHelper
    {
        public static async Task<IMessageActivity> GenerateMessageAsync(
            ResourceMultiLanguageGenerator lgMultiLangEngine,
            ITurnContext turnContext,
            string replyTemp,
            object replyArg)
        {
            var result = await lgMultiLangEngine.Generate(turnContext, replyTemp, replyArg);
            var activity = await new TextMessageActivityGenerator().CreateActivityFromText(result, null, turnContext, null);

            return activity;
        }
    }
}
