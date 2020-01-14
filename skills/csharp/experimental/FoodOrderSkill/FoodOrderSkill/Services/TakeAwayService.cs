using Microsoft.Azure.CognitiveServices.ContentModerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoodOrderSkill.Services
{
    public class TakeAwayService
    {
        private const string apiRootUrl = "https://www.citymeal.com/rest/";
        private static string apiKey = "";
        private static HttpClient httpClient = new HttpClient();
        private static string testPostCode = "88888";
        private static string testCountryCode = "DE";
        private static string testUserId = "";
        private static string testLanguage = "DE";
        private static string testCheckSum;

        public async void placeOrder (orderParams orderParameters)
        {

        }

    }

    public class orderParams
    {
        public string firstName;
        public string lastName;
        public string companyName;
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
    }
}
