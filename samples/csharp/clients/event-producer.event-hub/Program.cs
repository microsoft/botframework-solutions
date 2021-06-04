// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Configuration;

namespace EventProducer
{
    class Program
    {
        private static EventHubClient eventHubClient;
        private static string eventHubConnectionString;
        private static string eventHubName;
        private static string userId;
        private static IConfigurationRoot configurationRoot;

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            configurationRoot = builder.Build();
            eventHubConnectionString = configurationRoot.GetValue<string>("EventHubConnectionString");
            eventHubName = configurationRoot.GetValue<string>("EventHubName");
            userId = configurationRoot.GetValue<string>("UserId");
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            // Creates an EventHubsConnectionStringBuilder object from a the connection string, and sets the EntityPath.
            // Typically the connection string should have the Entity Path in it, but for the sake of this simple scenario
            // we are using the connection string from the namespace.
            var connectionStringBuilder = new EventHubsConnectionStringBuilder(eventHubConnectionString)
            {
                EntityPath = eventHubName
            };

            eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

            await SendMessagesToEventHub();

            await eventHubClient.CloseAsync();

            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
        }

        private static async Task SendMessagesToEventHub()
        {
            try
            {
                var message = "{'userid':'" + userId + "','message':'You have a new notification!'}";
                Console.WriteLine($"Sending message: {message}");
                await eventHubClient.SendAsync(new Microsoft.Azure.EventHubs.EventData(Encoding.UTF8.GetBytes(message)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} > Exception: {ex.Message}");
            }

            Console.WriteLine("Message sent.");
        }
    }
}