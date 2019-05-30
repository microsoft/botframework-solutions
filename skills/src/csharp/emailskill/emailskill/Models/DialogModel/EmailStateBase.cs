using System;
using System.Collections.Generic;
using Microsoft.Graph;

namespace EmailSkill.Models.DialogModel
{
    public class EmailStateBase
    {
        public EmailStateBase()
        {
            Message = new List<Message>();
            MessageList = new List<Message>();
            SenderName = null;
            IsFlaged = false;
            IsUnreadOnly = true;
            IsImportant = false;
            DirectlyToMe = false;
            StartDateTime = DateTime.UtcNow.Add(new TimeSpan(-7, 0, 0, 0));
            EndDateTime = DateTime.UtcNow;
            UserSelectIndex = -1;
            SearchTexts = null;
        }

        public List<Message> Message { get; set; }

        public List<Message> MessageList { get; set; }

        public string SenderName { get; set; }

        public string SearchTexts { get; set; }

        public int UserSelectIndex { get; set; }

        public int ShowEmailIndex { get; set; }

        public bool IsUnreadOnly { get; set; }

        public bool IsImportant { get; set; }

        public bool IsFlaged { get; set; }

        public bool DirectlyToMe { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }
    }
}
