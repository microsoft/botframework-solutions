using System;
using System.Threading.Tasks;
using Microsoft.Bot.StreamingExtensions;

namespace Microsoft.Bot.Builder.Solutions.Skills.Protocol
{
    public class RouteAction
    {
        public Func<ReceiveRequest, dynamic, Task<object>> Action { get; set; }
    }
}
