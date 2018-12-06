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
        public async Task<List<Poi>> PoiSearchAsync(PoiQuery query)
        {
            var url = "http://api.map.baidu.com/place/v2/search?query={QUERY}&location={LOCATION}&filter=industry_type:cater|sort_name:price|sort_rule:0|price_section:{PRICE_SECTION}&scope=2&radius=1000&output=json&ak=AGC1l7lGc2qIriDhu7wwE4tsbNt4uZUS";
            url = url.Replace("{QUERY}", query.Query);
            url = url.Replace("{LOCATION}", query.Location.Lat + "," + query.Location.Lng);
            url = url.Replace("{PRICE_SECTION}", query.Price_section);

            var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(url).ConfigureAwait(false);

            return await Task.Run(() => ParsePlaceResponse(response)).ConfigureAwait(false);
        }

        public async Task<List<Poi>> PlaceSearchAsync(string query, string region)
        {
            var url = "http://api.map.baidu.com/place/v2/search?query={QUERY}&region={REGION}&output=json&ak=AGC1l7lGc2qIriDhu7wwE4tsbNt4uZUS";
            url = url.Replace("{QUERY}", query);
            url = url.Replace("{LOCATION}", region);

            var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(url).ConfigureAwait(false);

            return await Task.Run(() => ParsePlaceResponse(response)).ConfigureAwait(false);
        }

        public async Task<List<Route>> GetDirectionAsync(Coordinate start, Coordinate end)
        {
            var url = "http://api.map.baidu.com/directionlite/v1/driving?origin={START}&destination={END}&ak=AGC1l7lGc2qIriDhu7wwE4tsbNt4uZUS";
            url = url.Replace("{START}", start.Lat + "," + start.Lng);
            url = url.Replace("{END}", end.Lat + "," + end.Lng);

            var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(url).ConfigureAwait(false);

            return await Task.Run(() => ParseRouteResponse(response)).ConfigureAwait(false);
        }

        private static List<Poi> ParsePlaceResponse(string response)
        {
            JObject jo = (JObject)JsonConvert.DeserializeObject(response);
            if (jo["results"].HasValues )
            {
                return JsonConvert.DeserializeObject<List<Poi>>(jo["results"].ToString());
            }
            else
            {
                return null;
            }
        }

        private static List<Route> ParseRouteResponse(string response)
        {
            JObject jo = (JObject)JsonConvert.DeserializeObject(response);
            if (jo["result"].HasValues && jo["result"]["routes"].HasValues)
            {
                return JsonConvert.DeserializeObject<List<Route>>(jo["result"]["routes"].ToString());
            }
            else
            {
                return null;
            }
        }


    }
}
