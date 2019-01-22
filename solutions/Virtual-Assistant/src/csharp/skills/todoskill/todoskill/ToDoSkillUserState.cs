using System.Collections.Generic;
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
            ListTypeIds = new Dictionary<string, Dictionary<string, string>>();
        }

        /// <summary>
        /// Gets or sets TaskContent.
        /// </summary>
        /// <value>
        /// ToDoTaskContent.
        /// </value>
        public Dictionary<string, Dictionary<string, string>> ListTypeIds { get; set; }
    }
}
