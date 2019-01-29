using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PointOfInterestSkill.ServiceClients
{
    /// <summary>
    /// Point of Interest skill helper class.
    /// </summary>
    public class ServiceHelper
    {
        private static HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Generate httpClient.
        /// </summary>
        /// <param name="accessToken">API access token.</param>
        /// <returns>Generated httpClient.</returns>
        public static HttpClient GetHttpClient()
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }
    }
}
