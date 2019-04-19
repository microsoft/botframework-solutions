namespace Microsoft.Bot.Builder.Skills.Protocol
{
    public class RouteTemplate
    {
        public string Method { get; set; }

        public string Path { get; set; }

        public RouteAction Action { get; set; }
    }
}