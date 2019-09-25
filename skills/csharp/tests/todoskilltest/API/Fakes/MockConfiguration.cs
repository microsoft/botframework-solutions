using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace ToDoSkillTest.API.Fakes
{
    public static class MockConfiguration
    {
        static MockConfiguration()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

            string mockAppSettingpath = Path.GetFullPath("../../../API/Fakes/MockAppSettings.json");
            configurationBuilder.AddJsonFile(mockAppSettingpath);
            Configuration = configurationBuilder.Build();
        }

        public static IConfiguration Configuration { get; set; }
    }
}
