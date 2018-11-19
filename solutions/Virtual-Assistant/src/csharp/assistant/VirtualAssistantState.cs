namespace VirtualAssistant
{
    public class VirtualAssistantState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualAssistantState"/> class.
        /// </summary>
        public VirtualAssistantState()
        {
            LastIntent = null;
        }

        /// <summary>
        /// Gets or sets LastIntent.
        /// </summary>
        /// <value>
        /// ToDoTaskContent.
        /// </value>
        public string LastIntent { get; set; }
    }
}
