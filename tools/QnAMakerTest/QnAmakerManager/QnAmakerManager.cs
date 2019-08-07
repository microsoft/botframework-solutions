using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using QnAMakerTest.Models;

namespace QnAMakerTest.QnAmakerManager
{
    class QnAMakerManager
    {
        public QnAMakerService QnAMakerService { get; }

        public QnAMakerManager(string configPath)
        {
            using (StreamReader r = new StreamReader(configPath))
            {
                string json = r.ReadToEnd();
                QnAMakerService = JsonConvert.DeserializeObject<QnAMakerService>(json);
            }
        }
    }
}
