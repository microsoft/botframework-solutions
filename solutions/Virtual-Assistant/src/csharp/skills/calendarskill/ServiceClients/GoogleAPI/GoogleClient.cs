namespace CalendarSkill.ServiceClients.GoogleAPI
{
    public class GoogleClient
    {
        public string ApplicationName { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string[] Scopes { get; set; }
    }
}