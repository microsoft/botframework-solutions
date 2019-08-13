using LuisModelTest.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LuisModelTest.LuisManager
{
    class LuisManager
    {
        public LuisService LuisService { get; }

        public LuisManager(string configPath)
        {
            using (StreamReader r = new StreamReader(configPath))
            {
                string json = r.ReadToEnd();
                LuisService = JsonConvert.DeserializeObject<LuisService>(json);
            }
        }
    }
}
