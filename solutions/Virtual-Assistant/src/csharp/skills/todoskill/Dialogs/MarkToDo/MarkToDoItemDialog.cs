using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Skills;
using ToDoSkill.Dialogs.MarkToDo.Resources;
using ToDoSkill.Dialogs.Shared.Resources;

namespace ToDoSkill
{
    public class MarkToDoItemDialog : ToDoSkillDialog
    {
        public MarkToDoItemDialog(
            ISkillConfiguration services,
            IStatePropertyAccessor<ToDoSkillState> toDoStateAccessor,
            IStatePropertyAccessor<ToDoSkillUserState> userStateAccessor,
            ITaskService serviceManager,
            IMailService mailService)
            : base(nameof(MarkToDoItemDialog), services, toDoStateAccessor, userStateAccessor, serviceManager, mailService)
        {
            var markToDoTask = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ClearContext,
                InitAllTasks,
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
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                state.LastListType = state.ListType;
                var service = await InitListTypeIds(sc);
                BotResponse botResponse;
                string taskTopicToBeMarked = null;
                if (state.MarkOrDeleteAllTasksFlag)
                {
                    await service.MarkTasksCompletedAsync(state.ListType, state.AllTasks);
                    botResponse = MarkToDoResponses.AfterAllToDoTasksCompleted;
                }
                else
                {
                    taskTopicToBeMarked = state.AllTasks[state.TaskIndexes[0]].Topic;
                    var tasksToBeMarked = new List<TaskItem>();
                    state.TaskIndexes.ForEach(i => tasksToBeMarked.Add(state.AllTasks[i]));
                    await service.MarkTasksCompletedAsync(state.ListType, tasksToBeMarked);
                    botResponse = MarkToDoResponses.AfterToDoTaskCompleted;
                }

                state.AllTasks = await service.GetTasksAsync(state.ListType);
                var allTasksCount = state.AllTasks.Count;
                var currentTaskIndex = state.ShowTaskPageIndex * state.PageSize;
                state.Tasks = state.AllTasks.GetRange(currentTaskIndex, Math.Min(state.PageSize, allTasksCount - currentTaskIndex));
                var markToDoAttachment = ToAdaptiveCardForOtherFlows(
                    state.Tasks,
                    state.AllTasks.Count,
                    taskTopicToBeMarked,
                    botResponse,
                    ToDoSharedResponses.ShowToDoTasks);
                var markToDoReply = sc.Context.Activity.CreateReply();
                markToDoReply.Attachments.Add(markToDoAttachment);
                await sc.Context.SendActivityAsync(markToDoReply);
                return await sc.EndDialogAsync(true);
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }
    }
}
