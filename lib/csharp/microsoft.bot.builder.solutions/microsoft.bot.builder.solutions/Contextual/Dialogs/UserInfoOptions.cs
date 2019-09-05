using Microsoft.Bot.Builder.Solutions.Contextual.Models;
using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Dialogs
{
    public class UserInfoOptions
    {
        public RelatedEntityInfo QueryItem { get; set; }

        public IList<string> QueryResult { get; set; }
    }
}
