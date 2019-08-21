using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ToDoSkill.Utilities.FeedbackMiddleware
{
    public class PromptFreeForm
    {
        public bool UserCanGiveFreeForm { get; set; } = false;

        public List<FeedbackAction> PromptFreeFormAction { get; set; } = null;
    }
}
