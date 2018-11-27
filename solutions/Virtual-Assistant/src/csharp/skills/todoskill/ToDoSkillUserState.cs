using Microsoft.Bot.Builder.Dialogs;

namespace ToDoSkill
{
    public class ToDoSkillUserState : DialogState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToDoSkillUserState"/> class.
        /// </summary>
        public ToDoSkillUserState()
        {
            TaskContent = null;
        }

        /// <summary>
        /// Gets or sets TaskContent.
        /// </summary>
        /// <value>
        /// ToDoTaskContent.
        /// </value>
        public string TaskContent { get; set; }

        /// <summary>
        /// Clear state.
        /// </summary>
        public void Clear()
        {
            TaskContent = null;
        }
    }
}
