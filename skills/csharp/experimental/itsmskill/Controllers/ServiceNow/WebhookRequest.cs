using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Controllers
{
    public class WebhookRequest
    {
        [JsonExtensionData]
        public IDictionary<string, JToken> Data { get; set; }

        public string WebhookId
        {
            get => this.GetPayloadProperty("WebhookId");

            set
            {
                if (this.Data != null)
                {
                    this.Data["WebhookId"] = value;
                }
            }
        }

        public string Serialize() =>
            JsonConvert.SerializeObject(this.Data);

        public string GetPayloadProperty(string key) => this.Data != null && this.Data.ContainsKey(key)
                ? this.Data[key].ToString()
                : null;
    }
}
