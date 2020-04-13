namespace ITSMSkill.Models.UpdateActivity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class ActivityReference
    {
        public string ThreadId { get; set; }

        public string ActivityId { get; set; }

        public object Data { get; set; }

        public Dictionary<string, DateTimeOffset> PropertyUpdatedTimeMap { get; set; } = new Dictionary<string, DateTimeOffset>();
    }
}
