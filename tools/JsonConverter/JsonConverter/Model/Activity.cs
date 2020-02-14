using System;
using System.Collections.Generic;
using System.Text;

namespace JsonConverter
{
    class Activity
    {
        public List<Reply> Replies { get; set; }
        public List<string> SuggestedActions { get; set; }
        public string InputHint { get; set; }
    }
}
