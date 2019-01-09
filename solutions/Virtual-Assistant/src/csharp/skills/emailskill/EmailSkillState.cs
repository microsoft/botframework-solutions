using System;
using System.Collections.Generic;
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
            NameList = new List<string>();
            SenderName = null;
            EmailList = new List<string>();
            TimeZoneInfo = TimeZoneInfo.Utc;
            Recipients = new List<Recipient>();
            Subject = null;
            Content = null;
            IsFlaged = false;
            IsUnreadOnly = true;
            IsImportant = false;
            ConfirmRecipientIndex = 0;
            ShowEmailIndex = 0;
            ShowRecipientIndex = 0;
            Token = null;
            ReadEmailIndex = 0;
            ReadRecipientIndex = 0;
            RecipientChoiceList = new List<Choice>();
            DirectlyToMe = false;
            StartDateTime = DateTime.UtcNow.Add(new TimeSpan(-7, 0, 0, 0));
            EndDateTime = DateTime.UtcNow;
            UserSelectIndex = -1;
            MailSourceType = MailSource.Other;
        }

        public DialogState ConversationDialogState { get; set; }

        public User User { get; set; }

        public UserInformation UserInfo { get; set; } = new UserInformation();

        public List<Message> Message { get; set; }

        public List<Message> MessageList { get; set; }

        public List<string> NameList { get; set; }

        public string SenderName { get; set; }

        public List<string> EmailList { get; set; }

        public TimeZoneInfo TimeZoneInfo { get; set; }

        public List<Recipient> Recipients { get; set; }

        public string Subject { get; set; }

        public string Content { get; set; }

        public bool IsUnreadOnly { get; set; }

        public bool IsImportant { get; set; }

        public bool IsFlaged { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public string Token { get; set; }

        public int ConfirmRecipientIndex { get; set; }

        public bool DirectlyToMe { get; set; }

        public int ShowEmailIndex { get; set; }

        public int ReadEmailIndex { get; set; }

        public int ShowRecipientIndex { get; set; }

        public int ReadRecipientIndex { get; set; }

        public List<Choice> RecipientChoiceList { get; set; }

        public Email LuisResult { get; set; }

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

        public bool IsNoRecipientAvailable()
        {
            return (NameList.Count == 0) && (EmailList.Count == 0);
        }

        public void Clear()
        {
            NameList.Clear();
            Message.Clear();
            Content = null;
            Subject = null;
            Recipients.Clear();
            ConfirmRecipientIndex = 0;
            ShowEmailIndex = 0;
            ReadEmailIndex = 0;
            ReadRecipientIndex = 0;
            RecipientChoiceList.Clear();
            IsUnreadOnly = true;
            IsImportant = false;
            StartDateTime = DateTime.UtcNow.Add(new TimeSpan(-7, 0, 0, 0));
            EndDateTime = DateTime.UtcNow;
            DirectlyToMe = false;
            SenderName = null;
            EmailList = new List<string>();
            ShowRecipientIndex = 0;
            LuisResultPassedFromSkill = null;
            MailSourceType = MailSource.Other;
            UserSelectIndex = -1;
        }

        public class UserInformation
        {
            public string Name { get; set; }

            public string PrimaryMail { get; set; }

            public string SecondaryMail { get; set; }

            public TimeZoneInfo Timezone { get; set; }
        }
    }
}