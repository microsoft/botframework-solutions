using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Models
{
    public class AnaphoraResolutionState
    {
        public string Text { get; set; }

        public string Pron { get; set; }

        public string QueryText { get; set; }

        public List<string> PreviousContacts { get; set; } = new List<string>();
    }
}
