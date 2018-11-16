using System.Collections.Generic;

namespace VirtualAssistant
{
    public class VirtualAssistantState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualAssistantState"/> class.
        /// </summary>
        public VirtualAssistantState()
        {
            ExecutedIntents = new List<string>();
        }

        /// <summary>
        /// Gets or sets ExecutedIntents.
        /// </summary>
        /// <value>
        /// ToDoTaskContent.
        /// </value>
        public List<string> ExecutedIntents { get; set; }
    }
}
