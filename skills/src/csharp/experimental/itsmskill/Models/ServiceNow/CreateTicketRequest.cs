namespace ITSMSkill.Models.ServiceNow
{
    public class CreateTicketRequest
    {
        public string caller_id { get; set; }

        public string short_description { get; set; }

        public string urgency { get; set; }
    }
}
