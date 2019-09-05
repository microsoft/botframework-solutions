using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Models
{
    public class UserHistoryInfo
    {
        public string PreviousIntent;

        public string PreviousUtterance;

        public string PreviousLocation;

        public UserHistoryInfo()
        {

        }

        public void Clean()
        {

        }
    }
}
