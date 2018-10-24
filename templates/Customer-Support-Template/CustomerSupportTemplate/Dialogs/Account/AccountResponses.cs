using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Dialogs.Account.Resources
{
    public class AccountResponses : TemplateManager
    {
        // Fields
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { ResponseIds.PayBillPolicyCard, (context, data) => CreateAdaptiveCardResponse(context, @".\Dialogs\Account\Resources\PayBill.json") },
                { ResponseIds.AccountIdPrompt, (context, data) => ResetPasswordStrings.AccountIdPrompt },
                { ResponseIds.EmailPrompt, (context, data) => ResetPasswordStrings.EmailPrompt },
                { ResponseIds.InvalidEmailMessage, (context, data) => ResetPasswordStrings.InvalidEmailMessage },
                { ResponseIds.ResetEmailSentMessage, (context, data) => string.Format(ResetPasswordStrings.ResetEmailSent, data) },
                { ResponseIds.UpdateContactInfoMessage, (context, data) => UpdateAccountStrings.UpdateContactInfoMessage },
                { ResponseIds.LoginPrompt, (context, data) => UpdateAccountStrings.LoginPrompt },
                { ResponseIds.NewInfoPrompt, (context, data) => UpdateAccountStrings.NewInfoPrompt },
                { ResponseIds.NewInfoCard, (context, data) => CreateAdaptiveCardResponse(context, @".\Dialogs\Account\Resources\UpdateContactInfo.json") },
                { ResponseIds.NewInfoSavedPrompt, (context, data) => UpdateAccountStrings.NewInfoSavedMessage },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public AccountResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        private static IMessageActivity CreateAdaptiveCardResponse(ITurnContext context, string path)
        {
            var response = context.Activity.CreateReply();

            var introCard = File.ReadAllText(path);

            response.Attachments = new List<Attachment>();
            response.Attachments.Add(new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(introCard),
            });

            return response;
        }

        public class ResponseIds
        {
            public const string PayBillPolicyCard = "payBillPolicy";
            public const string AccountIdPrompt = "accountIdPrompt";
            public const string EmailPrompt = "emailPrompt";
            public const string InvalidEmailMessage = "invalidEmailMessage";
            public const string ResetEmailSentMessage = "resetEmailSentMessage";
            public const string UpdateContactInfoMessage = "updateContactInfoMessage";
            public const string LoginPrompt = "loginPrompt";
            public const string NewInfoPrompt = "newInfoPrompt";
            public const string NewInfoCard = "updateContactInfo";
            public const string NewInfoSavedPrompt = "newInfoSavedPrompt";
        }
    }
}
