using System;
using System.Collections.Generic;
using System.Linq;
using EmailSkill.Dialogs.Shared.Resources.Strings;
using EmailSkill.Model;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Graph;

namespace EmailSkill
{
    public class EmailSkillState
    {
        public EmailSkillState()
        {
            User = new User();
            Message = new List<Message>();
            MessageList = new List<Message>();
            FindContactInfor = new FindContactInformation();
            SenderName = null;
            TimeZoneInfo = TimeZoneInfo.Utc;
            Subject = null;
            Content = null;
            IsFlaged = false;
            IsUnreadOnly = true;
            IsImportant = false;
            ShowEmailIndex = 0;
            Token = null;
            ReadEmailIndex = 0;
            DirectlyToMe = false;
            StartDateTime = DateTime.UtcNow.Add(new TimeSpan(-7, 0, 0, 0));
            EndDateTime = DateTime.UtcNow;
            UserSelectIndex = -1;
            MailSourceType = MailSource.Other;
            SearchTexts = null;
            GeneralSenderName = null;
            GeneralSearchTexts = null;
        }

        public DialogState ConversationDialogState { get; set; }

        public User User { get; set; }

        public UserInformation UserInfo { get; set; } = new UserInformation();

        public List<Message> Message { get; set; }

        public List<Message> MessageList { get; set; }

        public string SenderName { get; set; }

        public string SearchTexts { get; set; }

        public string GeneralSenderName { get; set; }

        public string GeneralSearchTexts { get; set; }

        public TimeZoneInfo TimeZoneInfo { get; set; }

        public FindContactInformation FindContactInfor { get; set; }

        public string Subject { get; set; }

        public string Content { get; set; }

        public bool IsUnreadOnly { get; set; }

        public bool IsImportant { get; set; }

        public bool IsFlaged { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public string Token { get; set; }

        public bool DirectlyToMe { get; set; }

        public int ShowEmailIndex { get; set; }

        public int ReadEmailIndex { get; set; }

        public EmailLU LuisResult { get; set; }

        public General GeneralLuisResult { get; set; }

        public IRecognizerConvert LuisResultPassedFromSkill { get; set; }

        public MailSource MailSourceType { get; set; }

        public int UserSelectIndex { get; set; }

        public TimeZoneInfo GetUserTimeZone()
        {
            if ((UserInfo != null) && (UserInfo.Timezone != null))
            {
                return UserInfo.Timezone;
            }

            return TimeZoneInfo.Local;
        }

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

        public void Clear()
        {
            Message.Clear();
            ShowEmailIndex = 0;
            IsUnreadOnly = true;
            IsImportant = false;
            StartDateTime = DateTime.UtcNow.Add(new TimeSpan(-7, 0, 0, 0));
            EndDateTime = DateTime.UtcNow;
            DirectlyToMe = false;
            UserSelectIndex = -1;

            FindContactInfor.Clear();
            Content = null;
            Subject = null;
            SenderName = null;
            LuisResultPassedFromSkill = null;
            ReadEmailIndex = 0;
            SearchTexts = null;
            GeneralSenderName = null;
            GeneralSearchTexts = null;
        }

        // Keep email display and focus data when in sub flow mode
        public void PartialClear()
        {
            FindContactInfor.Clear();
            Content = null;
            Subject = null;
            SenderName = null;
            LuisResultPassedFromSkill = null;
            ReadEmailIndex = 0;
            SearchTexts = null;
            GeneralSenderName = null;
            GeneralSearchTexts = null;
        }

        public class UserInformation
        {
            public string Name { get; set; }

            public string PrimaryMail { get; set; }

            public string SecondaryMail { get; set; }

            public TimeZoneInfo Timezone { get; set; }
        }

        public class FindContactInformation
        {
            public FindContactInformation()
            {
                CurrentContactName = string.Empty;
                ContactsNameList = new List<string>();
                Contacts = new List<Recipient>();
                ConfirmContactsNameIndex = 0;
                ShowContactsIndex = 0;
                UnconfirmedContact = new List<PersonModel>();
                FirstRetryInFindContact = true;
                ConfirmedContact = new PersonModel();
            }

            public List<string> ContactsNameList { get; set; }

            public List<Recipient> Contacts { get; set; }

            public int ConfirmContactsNameIndex { get; set; }

            public List<PersonModel> UnconfirmedContact { get; set; }

            public bool FirstRetryInFindContact { get; set; }

            public PersonModel ConfirmedContact { get; set; }

            public int ShowContactsIndex { get; set; }

            public string CurrentContactName { get; set; }

            public void Clear()
            {
                CurrentContactName = string.Empty;
                ContactsNameList.Clear();
                Contacts.Clear();
                ConfirmContactsNameIndex = 0;
                ShowContactsIndex = 0;
                UnconfirmedContact.Clear();
                FirstRetryInFindContact = true;
                ConfirmedContact = new PersonModel();
            }
        }
    }
}