using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using System;
using System.Threading;
using System.Threading.Tasks;
using ToDoSkill.Dialogs.AddToDo.Resources;
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

        public async Task<DialogTurnResult> CollectToDoTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Action.CollectToDoTaskContent);
        }

        public async Task<DialogTurnResult> AddToDoTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.OneNotePageId))
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.SettingUpOneNoteMessage));
                }

                var service = await _serviceManager.Init(state.MsGraphToken, state.OneNotePageId);
                var page = await service.GetDefaultToDoPage();
                state.OneNotePageId = page.Id;
                await service.AddToDoToOneNote(state.TaskContent, page.ContentUrl);
                var todosAndPageIdTuple = await service.GetMyToDoList();
                state.OneNotePageId = todosAndPageIdTuple.Item2;
                state.AllTasks = todosAndPageIdTuple.Item1;
                state.ShowToDoPageIndex = 0;
                var rangeCount = Math.Min(state.PageSize, state.AllTasks.Count);
                state.Tasks = state.AllTasks.GetRange(0, rangeCount);
                var toDoListAttachment = ToAdaptiveCardAttachmentForOtherFlows(
                    state.Tasks,
                    state.AllTasks.Count,
                    state.TaskContent,
                    AddToDoResponses.AfterToDoTaskAdded,
                    ToDoSharedResponses.ShowToDoTasks);

                var toDoListReply = sc.Context.Activity.CreateReply();
                toDoListReply.Attachments.Add(toDoListAttachment);
                await sc.Context.SendActivityAsync(toDoListReply);
                return await sc.EndDialogAsync(true);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> AskToDoTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var prompt = sc.Context.Activity.CreateReply(AddToDoResponses.AskToDoContentText);
            return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
        }

        public async Task<DialogTurnResult> AfterAskToDoTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var toDoContent);
                    state.TaskContent = toDoContent != null ? toDoContent.ToString() : sc.Context.Activity.Text;
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    return await sc.BeginDialogAsync(Action.CollectToDoTaskContent);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

    }
}
