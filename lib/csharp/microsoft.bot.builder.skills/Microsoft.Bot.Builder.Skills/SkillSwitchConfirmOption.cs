using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Skills
{
    public class SkillSwitchConfirmOption
    {
        public Activity LastActivity { get; set; }

        public string TargetIntent { get; set; }

        public Activity UserInputActivity { get; set; }
    }
}
