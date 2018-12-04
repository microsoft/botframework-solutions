using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Models.Proactive
{
    public class ProactiveModel : Dictionary<string, ProactiveModel.ProactiveData>
    {
        public class ProactiveData
        {
            public ConversationReference Conversation { get; set; }
        }
    }
}