using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VirtualAssistant.ServiceClients
{
    public class NetEaseMusicClient
    {
        public async Task<List<Song>> SearchSongAsync(string query)
        {
            var url = "https://api.bzqll.com/music/tencent/search?key=579621905&s={query}&limit={n}&offset=0&type=song";
            url = url.Replace("{query}", query);

            var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(url).ConfigureAwait(false);

            return await Task.Run(() => ParseJsonResponse(response)).ConfigureAwait(false);
        }

        private static List<Song> ParseJsonResponse(string response)
        {
            JObject jo = (JObject)JsonConvert.DeserializeObject(response);
            if (jo["data"].HasValues)
            {
                return JsonConvert.DeserializeObject<List<Song>>(jo["data"].ToString());
            }
            else
            {
                return null;
            }
        }
    }
}
