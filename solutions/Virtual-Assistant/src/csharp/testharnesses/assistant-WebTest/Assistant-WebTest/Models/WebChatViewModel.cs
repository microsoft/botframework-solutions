using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assistant_WebTest.Models
{
    public class WebChatViewModel
    {
        public string UserID { get; set; }

        public string UserName { get; set; }

        public string DirectLineToken { get; set; }

        public string VoiceName { get; set; }

        public string SpeechKey { get; set; }
    }
}
