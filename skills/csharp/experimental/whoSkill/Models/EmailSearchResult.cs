using System.Collections.Generic;
using Newtonsoft.Json;

namespace WhoSkill.Models
{
    public class EmailSearchResult
    {
        public string ODataEtag { get; set; }

        public string Id { get; set; }

        public EmailAddressContainer Sender { get; set; }

        public IEnumerable<EmailAddressContainer> ToRecipients { get; set; }

        public IEnumerable<EmailAddressContainer> CcRecipients { get; set; }
    }
}
