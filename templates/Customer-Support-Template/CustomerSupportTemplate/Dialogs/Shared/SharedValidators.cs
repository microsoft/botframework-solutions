using CustomerSupportTemplate.ServiceClients;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Dialogs.Shared
{
    public class SharedValidators
    {
        private static IServiceClient _client = new DemoServiceClient();

        public static Task<bool> OrderNumberValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // check regex - 7 digit number placeholder
            var regex = new Regex(@"\d{7}", RegexOptions.IgnoreCase);

            var match = regex.Match(promptContext.Recognized.Value);

            if (match.Success)
            {
                var id = promptContext.Recognized.Value = match.Value;

                // lookup order number to verify it exists
                var order = _client.GetOrderByNumber(id);

                // add to state
                if (order != null)
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        public static Task<bool> CartIdValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // check regex - 10 digit number placeholder
            var regex = new Regex(@"\d{10}", RegexOptions.IgnoreCase);

            var match = regex.Match(promptContext.Recognized.Value);

            if (match.Success)
            {
                var id = promptContext.Recognized.Value = match.Value;

                // lookup order number to verify it exists
                var order = _client.GetOrderByNumber(id);

                // add to state
                if (order != null)
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        public static Task<bool> ChoiceValidator(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                return Task.FromResult(true);
            }
            else if (int.TryParse(promptContext.Context.Activity.Text, out var index))
            {
                if (index <= promptContext.Options.Choices.Count)
                {
                    var selectedChoice = promptContext.Options.Choices[index - 1];

                    promptContext.Recognized.Value = new FoundChoice()
                    {
                        Index = index -1,
                        Value = selectedChoice.Value
                    };

                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        public static Task<bool> ItemNumberValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // check regex
            var regex = new Regex(@"\d{9}", RegexOptions.IgnoreCase);

            var match = regex.Match(promptContext.Recognized.Value);

            if (match.Success)
            {
                var id = promptContext.Recognized.Value = match.Value;

                // lookup item number to verify it exists
                var item = _client.GetItemById(id);

                // add to state
                if (item != null)
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        public static Task<bool> PhoneNumberValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var regex = new Regex(@"^\s*(?:\+?(\d{1,3}))?[-. (]*(\d{3})[-. )]*(\d{3})[-. ]*(\d{4})(?: *x(\d+))?\s*$", RegexOptions.IgnoreCase);

            var match = regex.Match(promptContext.Recognized.Value);

            if (match.Success)
            {
                var phone = promptContext.Recognized.Value = match.Value;

                // if phone == order phone number from state
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public static Task<bool> ZipCodeValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // check if response matches a zip code regex
            var regex = new Regex(@"^\d{5}$", RegexOptions.IgnoreCase);

            var match = regex.Match(promptContext.Recognized.Value);

            if (match.Success)
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public static Task<bool> ConfirmValidator(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                return Task.FromResult(true);
            }
            else if (promptContext.Context.Activity.Text == "1" || promptContext.Context.Activity.Text == "2")
            {
                if (promptContext.Context.Activity.Text == "1")
                {
                    promptContext.Recognized.Value = true;
                }
                else
                {
                    promptContext.Recognized.Value = false;
                }

                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }
    }
}
