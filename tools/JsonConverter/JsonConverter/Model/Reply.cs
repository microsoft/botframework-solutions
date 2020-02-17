using System;
using System.Collections.Generic;
using System.Text;

namespace JsonConverter
{
    internal class Reply
    {
        public string Text { get; set; }
        public string Speak { get; set; }

        public void Correct()
        {
            Text = Correct(Text);

            // TODO fill speak
            if (string.IsNullOrEmpty(Speak))
            {
                Speak = Text;
            }
            else
            {
                Speak = Correct(Speak);
            }
        }

        private string Correct(string text)
        {
            return text.Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }
}
