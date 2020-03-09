using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

namespace BotProject.Dialogs
{
    public class EventDateTimeDialog : ComponentDialog
    {
        public EventDateTimeDialog()
            : base(nameof(EventDateTimeDialog))
        {
            var steps = new WaterfallStep[]
            {
                TimexPromptStep,
                EndStep
            };

            AddDialog(new WaterfallDialog(this.Id, steps));
        }

        private Task<DialogTurnResult> TimexPromptStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Prompt for date time
            throw new NotImplementedException();
        }

        private Task<DialogTurnResult> EndStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // if we have a full datetime range, end dialog
            // otherwise, save current data in state and replace this dialog with self
            throw new NotImplementedException();
        }
    }
}
