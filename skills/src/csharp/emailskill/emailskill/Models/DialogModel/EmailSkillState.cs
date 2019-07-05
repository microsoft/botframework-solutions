using System;
using System.Collections.Generic;
using EmailSkill.Models;
using EmailSkill.Models.DialogModel;
using EmailSkill.Responses.Shared;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Graph;

namespace EmailSkill.Models
{
    public class EmailSkillState
    {
        public EmailSkillState()
        {

            TimeZoneInfo = TimeZoneInfo.Utc;

            Token = null;

            MailSourceType = MailSource.Other;

            CacheModel = null;
        }

        public UserInformation UserInfo { get; set; } = new UserInformation();

        public TimeZoneInfo TimeZoneInfo { get; set; }

        public string Token { get; set; }

        public emailLuis LuisResult { get; set; }

        public General GeneralLuisResult { get; set; }

        public MailSource MailSourceType { get; set; }

        public EmailStateBase CacheModel { get; set; }

        public TimeZoneInfo GetUserTimeZone()
        {
            if ((UserInfo != null) && (UserInfo.Timezone != null))
            {
                return UserInfo.Timezone;
            }

            return TimeZoneInfo.Local;
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