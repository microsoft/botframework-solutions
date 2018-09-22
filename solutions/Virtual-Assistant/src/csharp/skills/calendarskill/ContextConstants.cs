namespace CalendarSkill
{
    using System.Collections.Generic;

    /// <summary>
    /// ContextConstants.
    /// </summary>
    public class ContextConstants
    {
        // common

        /// <summary>
        /// UserTimeZone.
        /// </summary>
        public const string UserTimeZone = "timeZone";

        /// <summary>
        /// Location.
        /// </summary>
        public const string Location = "location";

        // msgraph

        /// <summary>
        /// MailList.
        /// </summary>
        public const string MailList = "mailList";

        /// <summary>
        /// MsGraphToken.
        /// </summary>
        public const string MsGraphToken = "msgraphToken";

        /// <summary>
        /// MsGraphClient.
        /// </summary>
        public const string MsGraphClient = "msgraphClient";

        /// <summary>
        /// FocusedEmail.
        /// </summary>
        public const string FocusedEmail = "focusEmail";

        /// <summary>
        /// UserLastAction.
        /// </summary>
        public const string UserLastAction = "userLastAction";

        /// <summary>
        /// ToRecipients.
        /// </summary>
        public const string ToRecipients = "recipients";

        /// <summary>
        /// ForwardComment.
        /// </summary>
        public const string ForwardComment = "forwardComment";

        /// <summary>
        /// ReplyMailContent.
        /// </summary>
        public const string ReplyMailContent = "replyContent";

        /// <summary>
        /// MailSubject.
        /// </summary>
        public const string MailSubject = "mailSubject";

        /// <summary>
        /// MailContent.
        /// </summary>
        public const string MailContent = "mailContent";

        /// <summary>
        /// NameList.
        /// </summary>
        public const string NameList = "nameList";

        /// <summary>
        /// SenderName.
        /// </summary>
        public const string SenderName = "sendName";

        /// <summary>
        /// PageSize.
        /// </summary>
        public const int PageSize = 5;

        // search parameter

        /// <summary>
        /// IsRead.
        /// </summary>
        public const string IsRead = "isRead";

        /// <summary>
        /// IsImportant.
        /// </summary>
        public const string IsImportant = "isImportant";

        /// <summary>
        /// FromDateTime.
        /// </summary>
        public const string FromDateTime = "fromDateTime";

        /// <summary>
        /// EndDateTime.
        /// </summary>
        public const string EndDateTime = "endDateTime";

        // common message

        /// <summary>
        /// DefaultWelcomeMessage.
        /// </summary>
        public const string DefaultWelcomeMessage = "Welcome!, I am Calendar Bot";

        /// <summary>
        /// DefaultErrorMessage.
        /// </summary>
        public const string DefaultErrorMessage = "Something went wrong.";

        /// <summary>
        /// CancelMessage.
        /// </summary>
        public const string CancelMessage = "Ok... Cancelled";

        /// <summary>
        /// NoAuth.
        /// </summary>
        public const string NoAuth = "Please log in before take further action.";

        /// <summary>
        /// NoFocusMessage.
        /// </summary>
        public const string NoFocusMessage = "Which message your are talking about?";

        /// <summary>
        /// NoEmailContent.
        /// </summary>
        public const string NoEmailContent = "What would you like to add as text?";

        /// <summary>
        /// NoRecipients.
        /// </summary>
        public const string NoRecipients = "Who would you like to send?";

        /// <summary>
        /// NoFowardRecipients.
        /// </summary>
        public const string NoFowardRecipients = "Who would you like to forward this to?";

        /// <summary>
        /// NoSubject.
        /// </summary>
        public const string NoSubject = "What is the email subject?";

        /// <summary>
        /// EmailNotFound.
        /// </summary>
        public const string EmailNotFound = "No Email found this time. please try again.";

        /// <summary>
        /// ConfirmRecipient.
        /// </summary>
        public const string ConfirmRecipient = "Which user you are talking about?";

        /// <summary>
        /// ConfirmSend.
        /// </summary>
        public const string ConfirmSend = "Please confirm before sending this email.";

        /// <summary>
        /// ConfirmSendFailed.
        /// </summary>
        public const string ConfirmSendFailed = "Please confirm before sending this email, say 'yes' or 'no' or something like that.";

        /// <summary>
        /// SentSuccessfully.
        /// </summary>
        public const string SentSuccessfully = "Your email has been successfully sent.";

        /// <summary>
        /// ReadOutPrompt.
        /// </summary>
        public const string ReadOutPrompt = "Of course, your latest email directly to you contains:";

        /// <summary>
        /// PromptPersonNotFound.
        /// </summary>
        public const string PromptPersonNotFound = "Person not found, please input the right name.";

        /// <summary>
        /// ActionEnded.
        /// </summary>
        public const string ActionEnded = "What else can I help you with?";

        /// <summary>
        /// AuthFailed.
        /// </summary>
        public const string AuthFailed = "Auth Failed, Please Try again.";

        /// <summary>
        /// NothingToCancelMessage.
        /// </summary>
        public const string NothingToCancelMessage = "Nothing to cancel";

        public static string NoForwardComment(List<string> nameList)
        {
            string names = string.Join(", ", nameList);
            return $"Yes of course, you want to send this email to {names}. What would you like to add as text?";
        }

        public static string NoReplyComment(List<string> nameList)
        {
            string names = string.Join(", ", nameList);
            return $"Yes of course, you want to send a reply to {names}. Do you want to add any additional text?";
        }

        public static string PromptTooManyPeople(string name)
        {
            return $"There are too many people named {name}, Please re-enter the name.";
        }

        public static string ConfirmReadOut(string name)
        {
            return $"You have an urgent email from {name}. Would you like me to read the content?";
        }
    }

    /// <summary>
    /// GraphQueryConstants.
    /// </summary>
    public class GraphQueryConstants
    {
        /// <summary>
        /// Search.
        /// </summary>
        public const string Search = "$search";

        /// <summary>
        /// Count.
        /// </summary>
        public const string Count = "$count";

        /// <summary>
        /// Top.
        /// </summary>
        public const string Top = "$top";

        /// <summary>
        /// Retrieves related resources.
        /// </summary>
        public const string Expand = "$expand";

        /// <summary>
        /// Filters properties (columns) use OData V4 query language
        /// (http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part2-url-conventions/odata-v4.0-errata03-os-part2-url-conventions-complete.html#_Toc453752356).
        /// </summary>
        public const string Filter = "$filter";

        /// <summary>
        /// Skip.
        /// </summary>
        public const string Skip = "$skip";

        /// <summary>
        /// Orderby.
        /// </summary>
        public const string Orderby = "$orderby";

        /// <summary>
        /// Format.
        /// </summary>
        public const string Format = "$format";

        /// <summary>
        /// Select.
        /// </summary>
        public const string Select = "$select";
    }
}