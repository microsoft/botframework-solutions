using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WhoSkill.Models
{
    public class Candidate
    {
        public string UserType { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Mail { get; set; } = string.Empty;

        public string JobTitle { get; set; } = "Mock JobTitle";

        public string UserPrincipalName { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public string OfficeLocation { get; set; } = "Mock OfficeLocation";

        public string MobilePhone { get; set; } = "Mock MobilePhone";

        public string Department { get; set; } = "Mock Department";
    }
}
