using EmailSkill.Models.DialogModel;
using Microsoft.Bot.Builder;
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
        protected IStatePropertyAccessor<EmailSkillState> EmailStateAccessor { get; set; }

        public EmailWaterfallDialog(string id, IEnumerable<WaterfallStep> steps = null, IStatePropertyAccessor<EmailSkillState> statePropertyAccessor = null)
            : base(id, steps)
        {
            EmailStateAccessor = statePropertyAccessor;
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

        protected override async Task<DialogTurnResult> OnStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (EmailStateAccessor != null)
            {
                var userState = await EmailStateAccessor.GetAsync(stepContext.Context);

                if (stepContext.State.Dialog.ContainsKey("EmailState"))
                {
                    var state = (EmailStateBase)stepContext.State.Dialog["EmailState"];
                    userState.CacheModel = state;
                }
            }

            return await base.OnStepAsync(stepContext, cancellationToken);
        }
    }
}
