using Microsoft.Azure.CognitiveServices.ContentModerator;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FoodOrderSkill.Services
{
    public class TakeAwayService
    {
        private const string apiRootUrl = "https://www.citymeal.com/rest/";
        private static string apiKey = "vAHm3k7BAqBba7m";
        private static HttpClient httpClient = new HttpClient();
        private static string testPostCode = "88888";
        private static string testCountryCode = "DE";
        private static string testUserId = "microsoft_voice_test";
        private static string testLanguage = "DE";

        private string ToQueryString(NameValueCollection nvc)
        {
            var array = (
                from key in nvc.AllKeys
                from value in nvc.GetValues(key)
                select string.Format(
            "{0}={1}",
            HttpUtility.UrlEncode(key),
            HttpUtility.UrlEncode(value))
                ).ToArray();
            return "?" + string.Join("&", array);
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public async Task<string> getRestaurants()
        {
            TakeAwayGetResterauntParams args = new TakeAwayGetResterauntParams();
            args.user = testUserId;
            args.countrycode = testCountryCode;
            args.postcode = testPostCode;
            args.method = "getRestaurants";
            args.language = testLanguage;
            args.checksum = CreateMD5(args.method + args.postcode + args.countrycode + apiKey);

            NameValueCollection queryParams = new NameValueCollection();

            queryParams.Add("method", args.method);
            queryParams.Add("user", args.user);
            queryParams.Add("countrycode", args.countrycode);
            queryParams.Add("postcode", args.postcode);
            queryParams.Add("language", args.language);
            queryParams.Add("checksum", args.checksum);

            var requestUrl = apiRootUrl + this.ToQueryString(queryParams);
            var response = await httpClient.GetStringAsync(requestUrl);
            return response;
        }

        public async void placeOrder (TakeAwayOrderParams args)
        {

        }

    }

    public class TakeAwayOrderParams
    {
        public string firstName;
        public string lastName;
        public string street;
        public string houseNumber;
        public string postCode;
        public string town;
        public string telephone;
        public string email;
        public string deliveryTime;
        public string paysWith;
        public string remarks;
        public string newsletter;
        public string resterauntid;
        public string orderstr;
        public string language;
        public string uniquedeviceid;
        public string orderreference;
        public string totalamountcharged;
        public string pad;
        public string transactionid;
        public string transactioncosts;
        public string checksum;

        // optional params
        public string companyName = null;

    }

    public class TakeAwayGetResterauntParams
    {
        public string method;
        public string user;
        public string postcode;
        public string countrycode;
        public string checksum;
        public string language;
    }
}
