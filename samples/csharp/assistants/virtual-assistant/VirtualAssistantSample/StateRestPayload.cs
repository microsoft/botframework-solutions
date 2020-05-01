using System.Collections.Generic;

namespace VirtualAssistantSample
{
    public class StateRestPayload
    {
        public string ConversationId { get; set; }

        public string PropertyName { get; set; }

        public IDictionary<string, object> Data { get; set; }
    }
}
