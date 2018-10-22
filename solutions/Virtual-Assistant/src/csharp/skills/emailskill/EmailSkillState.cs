﻿using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Graph;
using System;
using System.Collections.Generic;

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
            MsGraphToken = null;
            DirectlyToMe = false;
            StartDateTime = DateTime.UtcNow.Add(new TimeSpan(-7, 0, 0, 0));
            EndDateTime = DateTime.UtcNow;
        }

        public DialogState ConversationDialogState { get; set; }

        public User User { get; set; }

        public UserInformation UserInfo { get; set; } = new UserInformation();

        public List<Message> Message { get; set; }

        public List<Message> MessageList { get; set; }

        public List<string> NameList { get; set; }

        public string SenderName { get; set; }

        public TimeZoneInfo TimeZoneInfo { get; set; }

        public List<Recipient> Recipients { get; set; }

        public string Subject { get; set; }

        public string Content { get; set; }

        public bool IsUnreadOnly { get; set; }

        public bool IsImportant { get; set; }

        public bool IsFlaged { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public string MsGraphToken { get; set; }

        public int ConfirmRecipientIndex { get; set; }

        public bool DirectlyToMe { get; set; }

        public int ShowEmailIndex { get; set; }

        public int ShowRecipientIndex { get; set; }

        public Email LuisResult { get; set; }

        public General GeneralLuisResult { get; set; }

        public IRecognizerConvert LuisResultPassedFromSkill { get; set; }

        public TimeZoneInfo GetUserTimeZone()
        {
            if ((UserInfo != null) && (UserInfo.Timezone != null))
            {
                return UserInfo.Timezone;
            }

            return TimeZoneInfo.Local;
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
            IsUnreadOnly = true;
            IsImportant = false;
            StartDateTime = DateTime.UtcNow.Add(new TimeSpan(-7, 0, 0, 0));
            EndDateTime = DateTime.UtcNow;
            DirectlyToMe = false;
            SenderName = null;
            ShowRecipientIndex = 0;
            LuisResultPassedFromSkill = null;
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
