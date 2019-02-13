using System;

namespace Assistant_WebTest.Models
{
    public class WebChatViewModel
    {
        public string UserID { get; set; }

        public string UserName { get; set; }

        public string DirectLineToken { get; set; }

        public string VoiceName { get; set; }

        public string SpeechKey { get; set; }

        public string SpeechRegion { get; set; }
    }
}