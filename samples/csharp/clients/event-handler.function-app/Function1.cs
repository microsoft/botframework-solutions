// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EventHandler
{
    public static class Function1
    {
        private static DocumentClient documentDbclient = new DocumentClient(new Uri(Environment.GetEnvironmentVariable("DocumentDbEndpointUrl")), Environment.GetEnvironmentVariable("DocumentDbPrimaryKey"));

        [FunctionName("EventHubTrigger")]
        public static async Task Run([EventHubTrigger("YOUR_EVENT_HUB_NAME", Connection = "EventHubConnection")] EventData[] events, ILogger log)
        {
            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    var data = JsonConvert.DeserializeObject<EventDataType>(messageBody);
                    await SendEventToBot(data);
                    await Task.Yield();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not send event to bot due to exception: {ex.Message}");
                }
            }
        }

        private static async Task SendEventToBot(EventDataType eventData)
        {
            // Read from the user preference store to determine where to send notifications
            var databaseName = "UserPreference";
            var databaseCollectionName = "UserPreferenceCollection";
            await documentDbclient.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseName });
            await documentDbclient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(databaseName).ToString(), new DocumentCollection { Id = databaseCollectionName });

            var userPreferences = documentDbclient.CreateDocumentQuery<UserPreference>(UriFactory.CreateDocumentCollectionUri(databaseName, databaseCollectionName)).Where(r => r.UserId == eventData.UserId);
            UserPreference userPreference = null;

            if (userPreferences.Count() > 0)
            {
                foreach (var preference in userPreferences)
                {
                    userPreference = preference;
                    break;
                }
            }

            if (userPreference == null)
            {
                userPreference = new UserPreference
                {
                    UserId = eventData.UserId,
                    SendNotificationToConversation = true,
                    SendNotificationToMobileDevice = true,
                };
                await documentDbclient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, databaseCollectionName), userPreference);
            }

            // If the SendNotificationToConversation flag is true, send an event activity to a user's active conversation with the message.
            if (userPreference.SendNotificationToMobileDevice)
            {
                // Optional: You can use Azure Notification Hubs to push notifications to devices
                // https://docs.microsoft.com/en-us/azure/notification-hubs/notification-hubs-aspnet-backend-ios-apple-apns-notification
            }
            else
            {
                Console.WriteLine($"Not sending the message to the device because the user preference has the setting as disabled for user {eventData.UserId}");
            }

            // If the SendNotificationToConversation flag is true, send an event activity to a user's active conversation with the message.
            if (userPreference.SendNotificationToConversation)
            {
                // Connect to the Direct Line channel
                var client = new DirectLineClient(Environment.GetEnvironmentVariable("DirectLineSecret"));

                var conversation = await client.Conversations.StartConversationAsync();

                // Send an event activity with the user id and message
                var userMessage = Activity.CreateEventActivity() as Activity;
                userMessage.Text = eventData.Message;
                userMessage.Name = "BroadcastEvent";
                userMessage.Value = eventData;
                userMessage.From = new ChannelAccount("user1");

                var response = await client.Conversations.PostActivityAsync(conversation.ConversationId, userMessage);
            }
            else
            {
                Console.WriteLine($"Not sending the message to the bot because the user preference has the setting as disabled for user {eventData.UserId}");
            }
        }
    }
}