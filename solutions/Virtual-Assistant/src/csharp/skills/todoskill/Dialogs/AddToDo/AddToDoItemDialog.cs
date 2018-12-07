using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Skills;

namespace ToDoSkill
{
    public class AddToDoItemDialog : ToDoSkillDialog
    {
        public AddToDoItemDialog(
            ISkillConfiguration services,
            IStatePropertyAccessor<ToDoSkillState> toDoStateAccessor,
            IStatePropertyAccessor<ToDoSkillUserState> userStateAccessor,
            ITaskService serviceManager,
            IMailService mailService)
            : base(nameof(AddToDoItemDialog), services, toDoStateAccessor, userStateAccessor, serviceManager, mailService)
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
