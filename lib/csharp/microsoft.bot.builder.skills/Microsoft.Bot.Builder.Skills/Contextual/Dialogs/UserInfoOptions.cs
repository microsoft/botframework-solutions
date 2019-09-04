using Microsoft.Bot.Builder.Skills.Contextual.Models;
using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Skills.Contextual.Dialogs
{
    public class UserInfoOptions
    {
        public RelatedEntityInfo QueryItem { get; set; }

        public IList<string> QueryResult { get; set; }
    }
}
