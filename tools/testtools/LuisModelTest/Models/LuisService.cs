using System;
using System.Collections.Generic;
using System.Text;

namespace LuisModelTest.Models
{
    public class LuisService
    {
        public string subscriptionKey { get; set; }
        public string applicationId { get; set; }
        public string authoringKey { get; set; }
        public string modelName { get; set; }
        public string appName { get; set; }
        public string luisFileName { get; set; }
        public string testFileName { get; set; }
    }

}
