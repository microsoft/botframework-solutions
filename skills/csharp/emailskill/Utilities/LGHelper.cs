using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Schema;

namespace EmailSkill.Utilities
{
    public class LGHelper
    {
        public static async Task<IMessageActivity> GenerateMessageAsync(
            ITurnContext turnContext,
            string replyTemp,
            object replyArg)
        {
            string templateName = "@{" + replyTemp + "()}";
            return await new ActivityTemplate(templateName).BindToData(turnContext, replyArg);
        }
    }
}
