using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Skills.Tests
{
    [TestClass]
    public class ManifestTests
    {     
        [TestMethod]
        public async Task DeserializeValidManifestFile()
        {
            using (StreamReader sr = new StreamReader(@"calendarSkill.json"))
            {
                string manifestBody = await sr.ReadToEndAsync();
                JsonConvert.DeserializeObject(manifestBody);
            }                
        }
    }

}
