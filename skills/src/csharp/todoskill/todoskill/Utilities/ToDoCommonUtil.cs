using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Schema;

namespace ToDoSkill.Utilities
{
    public class ToDoCommonUtil
    {
        public const int DefaultDisplaySize = 4;

        public static async Task<Activity> GetToDoResponseActivity(string templateName, ITurnContext turnContext, object data)
        {
            return await new ActivityTemplate(templateName).BindToData(turnContext, data);
        }
    }
}
