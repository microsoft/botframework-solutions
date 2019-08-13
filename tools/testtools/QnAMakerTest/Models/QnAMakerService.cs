using System;
using System.Collections.Generic;
using System.Text;

namespace QnAMakerTest.Models
{
    class QnAMakerService
    {
        public string name { get; set; }
        public string subscriptionKey { get; set; }
        public string hostname { get; set; }
        public string endpointKey { get; set; }
        public string Id { get; set; }
        public string kbID { get; set; }
        public string trainFileName { get; set; }
        public string testFileName { get; set; }
    }

}
