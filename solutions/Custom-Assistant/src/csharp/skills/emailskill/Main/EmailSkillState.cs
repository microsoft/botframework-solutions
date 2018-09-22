// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Graph;

namespace EmailSkill
{
    /// <summary>
    /// Conversation state used in EmailBot.
    /// </summary>
    public class EmailSkillState : DialogState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailSkillState"/> class.
        /// </summary>
        public EmailSkillState()
        {
            this.User = new User();
            this.Message = new List<Message>();
            this.MessageList = new List<Message>();
            this.NameList = new List<string>();
            this.SenderName = null;
            this.TimeZoneInfo = TimeZoneInfo.Utc;
            this.Recipients = new List<Recipient>();
            this.Subject = null;
            this.Content = null;
            this.IsFlaged = false;
            this.IsRead = false;
            this.IsImportant = false;
            this.ConfirmRecipientIndex = 0;
            this.ShowEmailIndex = 0;
            this.ShowRecipientIndex = 0;
            this.MsGraphToken = null;
            this.DirectlyToMe = false;
            this.StartDateTime = DateTime.UtcNow.Add(new TimeSpan(-7, 0, 0, 0));
            this.EndDateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets or sets conversation dialog state.
        /// </summary>
        /// <value>The conversation dialog state.</value>
        public DialogState ConversationDialogState { get; set; }

        /// <summary>
        /// Gets or sets current User.
        /// </summary>
        /// <value>The current User.</value>
        public User User { get; set; }

        /// <summary>
        /// Gets or sets user info.
        /// </summary>
        /// <value>
        /// User info.
        /// </value>
        public UserInformation UserInfo { get; set; }

        /// <summary>
        /// Gets or sets current focused message.
        /// </summary>
        /// <value>
        /// Current focused message.
        /// </value>
        public List<Message> Message { get; set; }

        /// <summary>
        /// Gets or sets focused message list.
        /// </summary>
        /// <value>
        /// Focused message list.
        /// </value>
        public List<Message> MessageList { get; set; }

        /// <summary>
        /// Gets or sets the name list from user input.
        /// </summary>
        /// <value>
        /// The name list from user input.
        /// </value>
        public List<string> NameList { get; set; }

        /// <summary>
        /// Gets or sets sender's name.
        /// </summary>
        /// <value>
        /// Sender's name.
        /// </value>
        public string SenderName { get; set; }

        /// <summary>
        /// Gets or sets time zone info.
        /// </summary>
        /// <value>
        /// Time zone info.
        /// </value>
        public TimeZoneInfo TimeZoneInfo { get; set; }

        /// <summary>
        /// Gets or sets the recipients of email.
        /// </summary>
        /// <value>
        /// The recipients of email.
        /// </value>
        public List<Recipient> Recipients { get; set; }

        /// <summary>
        /// Gets or sets email subject.
        /// </summary>
        /// <value>
        /// Email subject.
        /// </value>
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets email message body.
        /// </summary>
        /// <value>
        /// Email message body.
        /// </value>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether bool flag, if email been read.
        /// </summary>
        /// <value>
        /// A value indicating whether bool flag, if email been read.
        /// </value>
        public bool IsRead { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether bool flag, if email is important.
        /// </summary>
        /// <value>
        /// A value indicating whether bool flag, if email is important.
        /// </value>
        public bool IsImportant { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether bool flag, if email has been flag.
        /// </summary>
        /// <value>
        /// A value indicating whether bool flag, if email has been flag.
        /// </value>
        public bool IsFlaged { get; set; }

        /// <summary>
        /// Gets or sets start date time.
        /// </summary>
        /// <value>
        /// Start date time.
        /// </value>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets end date time.
        /// </summary>
        /// <value>
        /// End date time.
        /// </value>
        public DateTime EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets cached token.
        /// </summary>
        /// <value>
        /// Cached token.
        /// </value>
        public string MsGraphToken { get; set; }

        /// <summary>
        /// Gets or sets the index when confirm recipient.
        /// </summary>
        /// <value>
        /// The index when confirm recipient.
        /// </value>
        public int ConfirmRecipientIndex { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether bool flag, if email directly to user.
        /// </summary>
        /// <value>
        /// A value indicating whether bool flag, if email directly to user.
        /// </value>
        public bool DirectlyToMe { get; set; }

        /// <summary>
        /// Gets or sets the page index when show email list.
        /// </summary>
        /// <value>
        /// The page index when show email list.
        /// </value>
        public int ShowEmailIndex { get; set; }

        /// <summary>
        /// Gets or sets the page index when confirm recipient.
        /// </summary>
        /// <value>
        /// The page index when confirm recipient.
        /// </value>
        public int ShowRecipientIndex { get; set; }

        /// <summary>
        /// Gets or sets luis result passed from other skill.
        /// </summary>
        /// <value>
        /// Luis result passed from other skill.
        /// </value>
        public IRecognizerConvert LuisResult { get; set; }

        /// <summary>
        /// Gets or sets luis result passed from other skill.
        /// </summary>
        /// <value>
        /// Luis result passed from other skill.
        /// </value>
        public IRecognizerConvert LuisResultPassedFromSkill { get; set; }

        public TimeZoneInfo GetUserTimeZone()
        {
            if ((UserInfo != null) && (UserInfo.Timezone != null))
            {
                return UserInfo.Timezone;
            }

            return TimeZoneInfo.Local;
        }

        /// <summary>
        /// The user Information.
        /// </summary>
        public class UserInformation
        {
            /// <summary>
            /// Gets or sets user name.
            /// </summary>
            /// <value>
            /// User name.
            /// </value>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets primary email address.
            /// </summary>
            /// <value>
            /// Primary email address.
            /// </value>
            public string PrimaryMail { get; set; }

            /// <summary>
            /// Gets or sets secondary email.
            /// </summary>
            /// <value>
            /// Secondary email.
            /// </value>
            public string SecondaryMail { get; set; }

            /// <summary>
            /// Gets or sets user timezone info.
            /// </summary>
            /// <value>
            /// User timezone info.
            /// </value>
            public TimeZoneInfo Timezone { get; set; }
        }
    }
}
