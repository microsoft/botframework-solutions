using Microsoft.Azure.CognitiveServices.ContentModerator.Models;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Expressions;
using Microsoft.Bot.Builder.Expressions.Parser;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AdaptiveAssistant.Actions
{
    public class ContactInput : InputDialog
    {
        private Regex EmailRegex = new Regex(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))\z");

        public string EmailProperty { get; set; }

        protected override Task<InputState> OnRecognizeInput(DialogContext dc)
        {
            var input = dc.State.GetValue<string>(PROCESS_INPUT_PROPERTY);

            // check if email
            if (EmailRegex.IsMatch(input))
            {
                dc.State.SetValue(EmailProperty, input);
            }
            else
            {
                dc.State.SetValue(PROCESS_INPUT_PROPERTY, input);
            }

            return input.Length > 0 ? Task.FromResult(InputState.Valid) : Task.FromResult(InputState.Unrecognized);
        }
    }
}

