using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Services
{
    public class StanfordNLPService
    {
        public static async Task<string> PostToStanfordNLPAsync(string text)
        {
            var url = "http://localhost:32199/api/values";
            var content = new StringContent("\"" + text + "\"", Encoding.UTF8, "application/json");
            using (HttpClient httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync(url, content);
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
