using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System.Collections.Generic;

namespace ToDoSkill
{
    public class ToDoSkillState : DialogState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToDoSkillState"/> class.
        /// </summary>
        public ToDoSkillState()
        {
            TaskContent = null;
            Task = new ToDoItem();
            Tasks = new List<ToDoItem>();
            TaskIndex = -1;
            MsGraphToken = null;
            ShowToDoPageIndex = 0;
            AllTasks = new List<ToDoItem>();
            DeleteTaskConfirmation = false;
            MarkOrDeleteAllTasksFlag = false;
            OneNotePageId = null;
            LuisResult = null;
            ConversationDialogState = null;
        }

        /// <summary>
        /// Gets PageSize.
        /// </summary>
        /// <value>
        /// PageSize.
        /// </value>
        public int PageSize { get; } = 5;

        /// <summary>
        /// Gets Luis intent score threshold.
        /// </summary>
        /// <value>
        /// Luis intent score threshold.
        /// </value>
        public double ScoreThreshold { get; } = 0.7;

        /// <summary>
        /// Gets or sets ToDoTaskContent.
        /// </summary>
        /// <value>
        /// ToDoTaskContent.
        /// </value>
        public string TaskContent { get; set; }

        /// <summary>
        /// Gets or sets ToDoTaskActivity.
        /// </summary>
        /// <value>
        /// ToDoTaskActivity.
        /// </value>
        public ToDoItem Task { get; set; }

        /// <summary>
        /// Gets or sets ToDoTaskActivities.
        /// </summary>
        /// <value>
        /// ToDoTaskActivities.
        /// </value>
        public List<ToDoItem> Tasks { get; set; }

        /// <summary>
        /// Gets or sets ToDoTaskIndex.
        /// </summary>
        /// <value>
        /// ToDoTaskIndex.
        /// </value>
        public int TaskIndex { get; set; }

        /// <summary>
        /// Gets or sets MsGraphToken.
        /// </summary>
        /// <value>
        /// MsGraphToken.
        /// </value>
        public string MsGraphToken { get; set; }

        /// <summary>
        /// Gets or sets ShowToDoPageIndex.
        /// </summary>
        /// <value>
        /// ShowToDoPageIndex.
        /// </value>
        public int ShowToDoPageIndex { get; set; }

        /// <summary>
        /// Gets or sets ToDoTaskAllActivities.
        /// </summary>
        /// <value>
        /// ToDoTaskAllActivities.
        /// </value>
        public List<ToDoItem> AllTasks { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether DeleteTaskConfirmation.
        /// </summary>
        /// <value>
        /// A value indicating whether DeleteTaskConfirmation.
        /// </value>
        public bool DeleteTaskConfirmation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether MarkOrDeleteAllTasksFlag.
        /// </summary>
        /// <value>
        /// A value indicating whether MarkOrDeleteAllTasksFlag.
        /// </value>
        public bool MarkOrDeleteAllTasksFlag { get; set; }

        /// <summary>
        /// Gets or sets OneNotePageId.
        /// </summary>
        /// <value>
        /// OneNotePageId.
        /// </value>
        public string OneNotePageId { get; set; }

        /// <summary>
        /// Gets or sets LuisResult.
        /// </summary>
        /// <value>
        /// LuisResult.
        /// </value>
        public ToDo LuisResult { get; set; }

        /// <summary>
        /// Gets or sets ConversationDialogState.
        /// </summary>
        /// <value>
        /// ConversationDialogState.
        /// </value>
        public DialogState ConversationDialogState { get; set; }

        /// <summary>
        /// Clear state.
        /// </summary>
        public void Clear()
        {
            TaskContent = null;
            Task = new ToDoItem();
            Tasks = new List<ToDoItem>();
            TaskIndex = -1;
            MsGraphToken = null;
            ShowToDoPageIndex = 0;
            AllTasks = new List<ToDoItem>();
            DeleteTaskConfirmation = false;
            MarkOrDeleteAllTasksFlag = false;
            OneNotePageId = null;
            LuisResult = null;
            ConversationDialogState = null;
        }
    }
}
