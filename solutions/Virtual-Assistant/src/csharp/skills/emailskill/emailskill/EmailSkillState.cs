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
            AttendeesNameList = new List<string>();
            SenderName = null;
            EmailList = new List<string>();
            TimeZoneInfo = TimeZoneInfo.Utc;
            Attendees = new List<Recipient>();
            Subject = null;
            Content = null;
            IsFlaged = false;
            IsUnreadOnly = true;
            IsImportant = false;
            ConfirmAttendeesNameIndex = 0;
            ShowEmailIndex = 0;
            ShowAttendeesIndex = 0;
            Token = null;
            ReadEmailIndex = 0;
            ReadRecipientIndex = 0;
            RecipientChoiceList = new List<Choice>();
            DirectlyToMe = false;
            StartDateTime = DateTime.UtcNow.Add(new TimeSpan(-100, 0, 0, 0));
            EndDateTime = DateTime.UtcNow;
            UserSelectIndex = -1;
            MailSourceType = MailSource.Other;
            UnconfirmedPerson = new List<PersonModel>();
            FirstRetryInFindContact = true;
            ConfirmedPerson = new PersonModel();
            FirstEnterFindContact = true;
            SearchTexts = null;
            GeneralSenderName = null;
            GeneralSearchTexts = null;
            CurrentAttendeeName = string.Empty;
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

        // Find contact related:

        public List<string> AttendeesNameList { get; set; }

        public List<string> EmailList { get; set; }

        public List<Recipient> Attendees { get; set; }

        public int ConfirmAttendeesNameIndex { get; set; }

        public List<PersonModel> UnconfirmedPerson { get; set; }

        public bool FirstRetryInFindContact { get; set; }

        public PersonModel ConfirmedPerson { get; set; }

        public int ShowAttendeesIndex { get; set; }

        public int ReadRecipientIndex { get; set; }

        public List<Choice> RecipientChoiceList { get; set; }

        public bool FirstEnterFindContact { get; set; }

        public string CurrentAttendeeName { get; set; }

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

        public bool IsNoRecipientAvailable()
        {
            return (AttendeesNameList.Count == 0) && (EmailList.Count == 0);
        }

        public void ClearParticipants()
        {
            AttendeesNameList.Clear();
            Attendees.Clear();
            ConfirmAttendeesNameIndex = 0;
            ReadRecipientIndex = 0;
            RecipientChoiceList.Clear();
            EmailList = new List<string>();
            ShowAttendeesIndex = 0;

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

        public class UserInformation
        {
            public string Name { get; set; }

            public string PrimaryMail { get; set; }

            public string SecondaryMail { get; set; }

            public TimeZoneInfo Timezone { get; set; }
        }

        public class CustomizedPerson
        {
            public CustomizedPerson()
            {
            }

            public CustomizedPerson(PersonModel person)
            {
                this.Emails = new List<ScoredEmailAddress>();
                person.Emails.ToList().ForEach(e => this.Emails.Add(new ScoredEmailAddress() { Address = e }));
                this.DisplayName = person.DisplayName;
                this.UserPrincipalName = person.UserPrincipalName;
            }

            public List<ScoredEmailAddress> Emails { get; set; }

            public string DisplayName { get; set; }

            public string UserPrincipalName { get; set; }
        }
    }
}