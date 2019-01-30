using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using ToDoSkill.Models;
using static ToDoSkill.Dialogs.Shared.ServiceProviderTypes;

namespace ToDoSkill
{
    public class ToDoSkillState : DialogState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToDoSkillState"/> class.
        /// </summary>
        public ToDoSkillState()
        {
            PageSize = 0;
            ReadSize = 0;
            Tasks = new List<TaskItem>();
            TaskIndexes = new List<int>();
            MsGraphToken = null;
            ShowTaskPageIndex = 0;
            ReadTaskIndex = 0;
            AllTasks = new List<TaskItem>();
            DeleteTaskConfirmation = false;
            MarkOrDeleteAllTasksFlag = false;
            ListTypeIds = new Dictionary<string, string>();
            LuisResult = null;
            GeneralLuisResult = null;
            ConversationDialogState = null;
            ListType = null;
            LastListType = null;
            FoodOfGrocery = null;
            HasShopVerb = false;
            ShopContent = null;
            TaskContentPattern = null;
            TaskContentML = null;
            TaskContent = null;
            SwitchListType = false;
            TaskServiceType = ProviderTypes.Other;
            AddDupTask = false;
            UserStateId = null;
        }

        /// <summary>
        /// Gets or sets PageSize.
        /// </summary>
        /// <value>
        /// PageSize.
        /// </value>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets ReadSize.
        /// </summary>
        /// <value>
        /// ReadSize.
        /// </value>
        public int ReadSize { get; set; }

        /// <summary>
        /// Gets or sets ToDoTaskActivities.
        /// </summary>
        /// <value>
        /// ToDoTaskActivities.
        /// </value>
        public List<TaskItem> Tasks { get; set; }

        /// <summary>
        /// Gets or sets ToDoTaskIndex.
        /// </summary>
        /// <value>
        /// ToDoTaskIndex.
        /// </value>
        public List<int> TaskIndexes { get; set; }

        /// <summary>
        /// Gets or sets MsGraphToken.
        /// </summary>
        /// <value>
        /// MsGraphToken.
        /// </value>
        public string MsGraphToken { get; set; }

        /// <summary>
        /// Gets or sets ShowTaskPageIndex.
        /// </summary>
        /// <value>
        /// ShowToDoPageIndex.
        /// </value>
        public int ShowTaskPageIndex { get; set; }

        /// <summary>
        /// Gets or sets ReadTaskIndex.
        /// </summary>
        /// <value>
        /// ReadTaskIndex.
        /// </value>
        public int ReadTaskIndex { get; set; }

        /// <summary>
        /// Gets or sets ToDoTaskAllActivities.
        /// </summary>
        /// <value>
        /// ToDoTaskAllActivities.
        /// </value>
        public List<TaskItem> AllTasks { get; set; }

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
        /// Gets or sets ListTypeIds.
        /// </summary>
        /// <value>
        /// OneNotePageId.
        /// </value>
        public Dictionary<string, string> ListTypeIds { get; set; }

        /// <summary>
        /// Gets or sets LuisResult.
        /// </summary>
        /// <value>
        /// LuisResult.
        /// </value>
        public ToDo LuisResult { get; set; }

        /// <summary>
        /// Gets or sets GeneralLuisResult.
        /// </summary>
        /// <value>
        /// LuisResult.
        /// </value>
        public General GeneralLuisResult { get; set; }

        /// <summary>
        /// Gets or sets ConversationDialogState.
        /// </summary>
        /// <value>
        /// ConversationDialogState.
        /// </value>
        public DialogState ConversationDialogState { get; set; }

        /// <summary>
        /// Gets or sets TaskType.
        /// </summary>
        /// <value>
        /// TaskType.
        /// </value>
        public string ListType { get; set; }

        /// <summary>
        /// Gets or sets LastListType.
        /// </summary>
        /// <value>
        /// TaskType.
        /// </value>
        public string LastListType { get; set; }

        /// <summary>
        /// Gets or sets FoodOfGrocery.
        /// </summary>
        /// <value>
        /// TaskType.
        /// </value>
        public string FoodOfGrocery { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets HasShopVerb.
        /// </summary>
        /// <value>
        /// TaskType.
        /// </value>
        public bool HasShopVerb { get; set; }

        /// <summary>
        /// Gets or sets ShopContent.
        /// </summary>
        /// <value>
        /// TaskType.
        /// </value>
        public string ShopContent { get; set; }

        /// <summary>
        /// Gets or sets TaskContentPattern.
        /// </summary>
        /// <value>
        /// ToDoTaskContent.
        /// </value>
        public string TaskContentPattern { get; set; }

        /// <summary>
        /// Gets or sets TaskContentML.
        /// </summary>
        /// <value>
        /// ToDoTaskContent.
        /// </value>
        public string TaskContentML { get; set; }

        /// <summary>
        /// Gets or sets TaskContent.
        /// </summary>
        /// <value>
        /// ToDoTaskContent.
        /// </value>
        public string TaskContent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets SwitchListType.
        /// </summary>
        /// <value>
        /// SwitchListType.
        /// </value>
        public bool SwitchListType { get; set; }

        /// <summary>
        /// Gets or sets TaskContent.
        /// </summary>
        /// <value>
        /// ToDoTaskContent.
        /// </value>
        public ProviderTypes TaskServiceType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets AddDupTask.
        /// </summary>
        /// <value>
        /// bool.
        /// </value>
        public bool AddDupTask { get; set; }

        /// <summary>
        /// Gets or sets UserStateId.
        /// </summary>
        /// <value>
        /// UserStateId.
        /// </value>
        public string UserStateId { get; set; }

        /// <summary>
        /// Clear state.
        /// </summary>
        public void Clear()
        {
            PageSize = 0;
            ReadSize = 0;
            Tasks = new List<TaskItem>();
            TaskIndexes = new List<int>();
            MsGraphToken = null;
            ShowTaskPageIndex = 0;
            AllTasks = new List<TaskItem>();
            DeleteTaskConfirmation = false;
            MarkOrDeleteAllTasksFlag = false;
            ListTypeIds = new Dictionary<string, string>();
            LuisResult = null;
            GeneralLuisResult = null;
            ConversationDialogState = null;
            ListType = null;
            LastListType = null;
            FoodOfGrocery = null;
            HasShopVerb = false;
            ShopContent = null;
            TaskContentPattern = null;
            TaskContentML = null;
            TaskContent = null;
            SwitchListType = false;
            TaskServiceType = ProviderTypes.Other;
            AddDupTask = false;
            UserStateId = null;
        }
    }
}