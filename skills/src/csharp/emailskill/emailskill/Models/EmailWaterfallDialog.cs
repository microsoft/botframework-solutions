using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EmailSkill.Models
{
    public class EmailWaterfallDialog : WaterfallDialog
    {
        public EmailWaterfallDialog(string id, IEnumerable<WaterfallStep> steps = null)
            : base(id, steps)
        {
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            EmailSkillDialogOptions skillOptions;
            if (options is EmailSkillDialogOptions)
            {
                skillOptions = (EmailSkillDialogOptions)options;
            }
            else
            {
                skillOptions = ((JObject)options).ToObject<EmailSkillDialogOptions>();
            }

            return await base.BeginDialogAsync(dc, skillOptions, cancellationToken);
        }
    }
}
