using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VirtualAssistant.ServiceClients
{
    public class BaiduMapClient
    {
        public async Task<List<Poi>> PoiSearch(PoiQuery query)
        {
            var url = "http://api.map.baidu.com/place/v2/search?query={QUERY}&location={LOCATION}&filter=industry_type:cater|sort_name:price|sort_rule:0|price_section:{PRICE_SECTION}&scope=2&radius=1000&output=json&ak=AGC1l7lGc2qIriDhu7wwE4tsbNt4uZUS";
            url = url.Replace("{QUERY}", query.Query);
            url = url.Replace("{LOCATION}", query.Location.Lat + "," + query.Location.Lng);
            url = url.Replace("{PRICE_SECTION}", query.Price_section);

            var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(url).ConfigureAwait(false);

            return await Task.Run(() => ParseJsonResponse(response)).ConfigureAwait(false);
        }

        private static List<Poi> ParseJsonResponse(string response)
        {
            JObject jo = (JObject)JsonConvert.DeserializeObject(response);
            if (jo["results"].HasValues)
            {
                return JsonConvert.DeserializeObject<List<Poi>>(jo["results"].ToString());
            }
            else
            {
                return null;
            }
        }
    }
}
