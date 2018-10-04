using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using System;
using System.Threading;
using System.Threading.Tasks;
using ToDoSkill.Dialogs.MarkToDo.Resources;
using ToDoSkill.Dialogs.Shared.Resources;

namespace ToDoSkill
{
    public class MarkToDoItemDialog : ToDoSkillDialog
    {
        public MarkToDoItemDialog(
            SkillConfiguration services,
            IStatePropertyAccessor<ToDoSkillState> accessor,
            IToDoService serviceManager)
            : base(nameof(MarkToDoItemDialog), services, accessor, serviceManager)
        {
            var markToDoTask = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ClearContext,
                CollectToDoTaskIndex,
                this.MarkToDoTaskCompleted,
            };

            var collectToDoTaskIndex = new WaterfallStep[]
            {
                AskToDoTaskIndex,
                AfterAskToDoTaskIndex,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.MarkToDoTaskCompleted, markToDoTask));
            AddDialog(new WaterfallDialog(Action.CollectToDoTaskIndex, collectToDoTaskIndex));

            // Set starting dialog for component
            InitialDialogId = Action.MarkToDoTaskCompleted;
        }

        public async Task<DialogTurnResult> MarkToDoTaskCompleted(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.OneNotePageId))
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SharedResponses.SettingUpOneNoteMessage));
                }

                var service = await _serviceManager.Init(state.MsGraphToken, state.OneNotePageId);
                var page = await service.GetDefaultToDoPage();
                BotResponse botResponse;
                string taskToBeMarked = null;
                if (state.MarkOrDeleteAllTasksFlag)
                {
                    await service.MarkAllToDoItemsCompleted(state.AllTasks, page.ContentUrl);
                    botResponse = MarkToDoResponses.AfterAllToDoTasksCompleted;
                }
                else
                {
                    await service.MarkToDoItemCompleted(state.Tasks[state.TaskIndex], page.ContentUrl);
                    botResponse = MarkToDoResponses.AfterToDoTaskCompleted;
                    taskToBeMarked = state.Tasks[state.TaskIndex].Topic;
                }

                var todosAndPageIdTuple = await service.GetMyToDoList();
                state.OneNotePageId = todosAndPageIdTuple.Item2;
                state.AllTasks = todosAndPageIdTuple.Item1;
                var allTasksCount = state.AllTasks.Count;
                var currentTaskIndex = state.ShowToDoPageIndex * state.PageSize;
                state.Tasks = state.AllTasks.GetRange(currentTaskIndex, Math.Min(state.PageSize, allTasksCount - currentTaskIndex));
                var markToDoAttachment = ToAdaptiveCardAttachmentForOtherFlows(
                    state.Tasks,
                    state.AllTasks.Count,
                    taskToBeMarked,
                    botResponse,
                    SharedResponses.ShowToDoTasks);
                var markToDoReply = sc.Context.Activity.CreateReply();
                markToDoReply.Attachments.Add(markToDoAttachment);
                await sc.Context.SendActivityAsync(markToDoReply);
                return await sc.EndDialogAsync(true);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }
    }
}
