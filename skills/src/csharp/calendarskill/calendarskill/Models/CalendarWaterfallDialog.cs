using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarSkill.Models
{
    public class CalendarWaterfallDialog : WaterfallDialog
    {
        public CalendarWaterfallDialog(string id, IEnumerable<WaterfallStep> steps = null, IStatePropertyAccessor<CalendarSkillState> statePropertyAccessor = null)
            : base(id, steps)
        {
            CalendarStateAccessor = statePropertyAccessor;
        }

        protected IStatePropertyAccessor<CalendarSkillState> CalendarStateAccessor { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            CalendarSkillDialogOptions skillOptions;
            if (options is CalendarSkillDialogOptions)
            {
                skillOptions = (CalendarSkillDialogOptions)options;
            }
            else
            {
                skillOptions = ((JObject)options).ToObject<CalendarSkillDialogOptions>();
            }

            return await base.BeginDialogAsync(dc, skillOptions, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (CalendarStateAccessor != null)
            {
                var userState = await CalendarStateAccessor.GetAsync(stepContext.Context);

                if (stepContext.State.Dialog.ContainsKey("CalendarState"))
                {
                    var state = (CalendarDialogStateBase)stepContext.State.Dialog["CalendarState"];
                    userState.CacheModel = state;
                }
            }

            return await base.OnStepAsync(stepContext, cancellationToken);
        }
    }
}
