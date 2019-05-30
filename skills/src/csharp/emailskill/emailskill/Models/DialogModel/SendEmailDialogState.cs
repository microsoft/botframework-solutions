using EmailSkill.Responses.Shared;
using static EmailSkill.Models.EmailSkillState;

namespace EmailSkill.Models.DialogModel
{
    public class SendEmailDialogState : EmailStateBase
    {
        public SendEmailDialogState()
            : base()
        {
            Subject = null;
            Content = null;
            FindContactInfor = new FindContactInformation();
        }

        public SendEmailDialogState(EmailStateBase stateBase)
        {
            Message = stateBase.Message;
            MessageList = stateBase.MessageList;
            SenderName = stateBase.SenderName;
            IsFlaged = stateBase.IsFlaged;
            IsUnreadOnly = stateBase.IsUnreadOnly;
            IsImportant = stateBase.IsImportant;
            DirectlyToMe = stateBase.DirectlyToMe;
            StartDateTime = stateBase.StartDateTime;
            EndDateTime = stateBase.EndDateTime;
            UserSelectIndex = stateBase.UserSelectIndex;
            SearchTexts = stateBase.SearchTexts;

            Subject = null;
            Content = null;
            FindContactInfor = new FindContactInformation();
        }

        public string Subject { get; set; }

        public string Content { get; set; }

        public FindContactInformation FindContactInfor { get; set; }

        public void ClearParticipants()
        {
            FindContactInfor.Clear();

            Subject = string.IsNullOrEmpty(Subject) ? EmailCommonStrings.Skip : Subject;
            Content = string.IsNullOrEmpty(Content) ? EmailCommonStrings.Skip : Content;
        }

        public void ClearSubject()
        {
            Subject = null;
            Content = string.IsNullOrEmpty(Content) ? EmailCommonStrings.Skip : Content;
        }

        public void ClearContent()
        {
            Content = null;
            Subject = string.IsNullOrEmpty(Subject) ? EmailCommonStrings.Skip : Subject;
        }
    }
}
