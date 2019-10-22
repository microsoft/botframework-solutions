using System;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;

namespace AdaptiveAssistant.Input
{
    public class ContactInput : InputDialog
    {
        public string EmailProperty { get; set; }

        protected override Task<InputState> OnRecognizeInput(DialogContext dc)
        {
            var input = dc.State.GetValue<string>(PROCESS_INPUT_PROPERTY);

            // check if email
            if (IsEmailValid(input))
            {
                dc.State.SetValue(EmailProperty, input);
            }
            else
            {
                dc.State.SetValue(PROCESS_INPUT_PROPERTY, input);
            }

            return input.Length > 0 ? Task.FromResult(InputState.Valid) : Task.FromResult(InputState.Unrecognized);
        }

        public bool IsEmailValid(string emailaddress)
        {
            try
            {
                var m = new MailAddress(emailaddress);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}

