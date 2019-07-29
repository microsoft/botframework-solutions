using System;
using System.Threading.Tasks;
using Microsoft.Bot.StreamingExtensions;

namespace Microsoft.Bot.Builder.Skills.Protocol
{
    public class RouteAction
    {
        public Func<ReceiveRequest, dynamic, Task<object>> Action { get; set; }
    }
}