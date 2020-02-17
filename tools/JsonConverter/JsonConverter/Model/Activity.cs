using System;
using System.Collections.Generic;
using System.Text;

namespace JsonConverter
{
    internal class Activity
    {
        public List<Reply> Replies { get; set; }
        public List<string> SuggestedActions { get; set; }
        public string InputHint { get; set; }

        public void Correct()
        {
            foreach (var reply in Replies)
            {
                reply.Correct();
            }
        }
    }
}
