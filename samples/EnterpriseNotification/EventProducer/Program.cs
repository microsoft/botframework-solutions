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
        private static IConfigurationRoot configurationRoot;

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            configurationRoot = builder.Build();
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            var connectionStringBuilder = new EventHubsConnectionStringBuilder(configurationRoot.GetValue<string>("EventHubConnectionString"))
            {
                EntityPath = configurationRoot.GetValue<string>("EventHubName")
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
                var message = "{'userid':'c7879ddc-9b7a-4ffb-b934-abc1b34a41ba','message':'heres an event'}";
                Console.WriteLine($"Sending message: {message}");
                await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(message)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} > Exception: {ex.Message}");
            }

            Console.WriteLine("message sent.");
        }
    }
}