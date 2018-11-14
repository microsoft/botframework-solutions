using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CalendarSkill.ServiceClients.GoogleAPI;
using Microsoft.Graph;

namespace CalendarSkill.ServiceClients
{
    public class GraphClientHelper
    {
        /// <summary>
        /// Get an authenticated ms graph client use access token.
        /// </summary>
        /// <param name="accessToken">access token.</param>
        /// <param name="info">timeZone info.</param>
        /// <returns>Authenticated graph service client.</returns>
        public static IGraphServiceClient GetAuthenticatedClient(string accessToken, TimeZoneInfo info)
        {
            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                        // Get event times in the current time zone.
                        requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + info.Id + "\"");
                        await Task.CompletedTask;
                    }));
            return graphClient;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetCalendarService"/> class.
        /// </summary>
        /// <param name="token">the access token.</param>
        /// <param name="source">the calendar provider.</param>
        /// <param name="config">the config for the Google application.</param>
        /// <returns>ICalendar. </returns>
        public static ICalendar GetCalendarService(string token, EventSource source, GoogleClient config)
        {
            switch (source)
            {
                case EventSource.Microsoft:
                    return new MSGraphCalendarAPI(token);
                case EventSource.Google:
                    // Todo: Google API timezone?
                    return new GoogleCalendarAPI(config, token);
                default:
                    throw new Exception("Event Type not Defined");
            }
        }
    }
}
