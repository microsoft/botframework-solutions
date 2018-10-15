using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using System;
using System.Threading;
using System.Threading.Tasks;
using ToDoSkill.Dialogs.Shared.Resources;

namespace ToDoSkill
{
    public class AddToDoItemDialog : ToDoSkillDialog
    {
        public AddToDoItemDialog(
            SkillConfiguration services,
            IStatePropertyAccessor<ToDoSkillState> accessor,
            IToDoService serviceManager)
            : base(nameof(AddToDoItemDialog), services, accessor, serviceManager)
        {
            var addToDoTask = new WaterfallStep[]
           {
                GetAuthToken,
                AfterGetAuthToken,
                ClearContext,
                CollectToDoTaskContent,
                AddToDoTask,
           };

            var collectToDoTaskContent = new WaterfallStep[]
            {
                AskToDoTaskContent,
                AfterAskToDoTaskContent,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.AddToDoTask, addToDoTask));
            AddDialog(new WaterfallDialog(Action.CollectToDoTaskContent, collectToDoTaskContent));

            // Set starting dialog for component
            InitialDialogId = Action.AddToDoTask;
        }
    }
}
