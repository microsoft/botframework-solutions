// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using AdaptiveCards;
using AdaptiveCards.Rendering;
using AdaptiveCards.Rendering.Wpf;
using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace DirectLineExample
{
    /// <summary>
    /// A sample Application to demonstrate the use of DirectLine (WebSockets) to communicate with a Virtual Assistant
    /// It demonstrates the ability to send and receive Events as well as process responses including Adaptive Cards
    /// This simple example provides a clear demonstration of the basic steps required to embed a Custom Assistant
    /// within a device.
    /// </summary>
    class Program
    {
        // Set this to the Secret and botId for the Bot you wish to communicate with
        private static string botDirectLineSecret = "";
        private static string botId = "";
        private static string fromUserId = "YourUserId";
        private static string fromUserName = "YourUserName";

        static async Task Main(string[] args)
        {
            DirectLineClientCredentials creds = new DirectLineClientCredentials(botDirectLineSecret);
            DirectLineClient client = new DirectLineClient(creds);

            Conversation conversation = await client.Conversations.StartConversationAsync();

            using (var webSocketClient = new WebSocket(conversation.StreamUrl))
            {
                webSocketClient.OnMessage += WebSocketClient_OnMessage;
                webSocketClient.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                webSocketClient.Connect();

                // Optional, helps provide additional context on the user for some skills/scenarios
                await SendStartupEvents(client, conversation);

                while (true)
                {
                    string input = Console.ReadLine().Trim();

                    if (input.ToLower() == "exit")
                    {
                        break;
                    }
                    else
                    {
                        if (input.Length > 0)
                        {
                            Activity userMessage = new Activity
                            {
                                From = new ChannelAccount(fromUserId, fromUserName),
                                Text = input,
                                Type = ActivityTypes.Message
                            };

                            await client.Conversations.PostActivityAsync(conversation.ConversationId, userMessage);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// These are sample startup events used in the Virtual Assistant for setting 
        /// locale and providing a user's current coordinates.
        /// </summary>
        private async static Task SendStartupEvents(DirectLineClient client, Conversation conversation)
        {
            Activity locationEvent = new Activity
            {
                Name = "VA.Location",
                From = new ChannelAccount(fromUserId, fromUserName),
                Type = ActivityTypes.Event,
                Value = "47.659291, -122.140633"
            };

            await client.Conversations.PostActivityAsync(conversation.ConversationId, locationEvent);

            Activity timezoneEvent = new Activity
            {
                Name = "VA.Timezone",
                From = new ChannelAccount(fromUserId, fromUserName),
                Type = ActivityTypes.Event,
                Value = "Pacific Standard Time"
            };

            await client.Conversations.PostActivityAsync(conversation.ConversationId, timezoneEvent);
        }    

        private async static void WebSocketClient_OnMessage(object sender, MessageEventArgs e)
        {
            // Occasionally, the Direct Line service sends an empty message as a liveness ping. Ignore these messages.

            if (string.IsNullOrWhiteSpace(e.Data))
            {
                return;
            }

            var activitySet = JsonConvert.DeserializeObject<ActivitySet>(e.Data);
            var activities = from x in activitySet.Activities
                             where x.From.Id == botId
                             select x;

            foreach (Activity activity in activities)
            {
                switch (activity.Type)
                {                
                    case ActivityTypes.Message:

                        // No visual cards so let's use Speak property to be more descriptive.                
                        Console.WriteLine(activity.Speak ?? activity.Text ?? "No activity text found");

                        // Do we have any attachments on this message?
                        if (activity.Attachments != null && activity.Attachments.Count > 0)
                        {
                            // There could be multiple attachments (e.g. carousel) but we only want one for the Speech variant
                            Attachment attachment = activity.Attachments.First<Attachment>();

                            switch (attachment.ContentType)
                            {
                                case "application/vnd.microsoft.card.hero":
                                case "application/vnd.microsoft.card.oauth":
                                    Console.WriteLine(activity.Speak ?? activity.Text);
                                    // Show how to open up the card in case it's needed.
                                    RenderHeroCard(attachment);
                                    break;
                                case "application/vnd.microsoft.card.adaptive":
                                    // Show how to open up the card in case it's needed.
                                    await RenderAdaptiveCard(attachment);
                                    break;
                                case "image/png":
                                    Console.WriteLine($"Opening the requested image '{attachment.ContentUrl}'");
                                    Process.Start(attachment.ContentUrl);
                                    break;
                            }
                        }

                        // If input is being accepted show prompt
                        if (activity.InputHint == InputHints.ExpectingInput || activity.InputHint == InputHints.AcceptingInput)
                        {
                            Console.Write("Message> ");
                        }

                        break;
                    case ActivityTypes.Event:

                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"* Received {activity.Name} event from the Virtual Assistant. * ");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;
                }
            }            
        }

        private static void RenderHeroCard(Attachment attachment)
        {
            const int Width = 70;
            Func<string, string> contentLine = (content) => string.Format($"{{0, -{Width}}}", string.Format("{0," + ((Width + content.Length) / 2).ToString() + "}", content));

            var heroCard = JsonConvert.DeserializeObject<HeroCard>(attachment.Content.ToString());

            if (heroCard != null)
            {
                Console.WriteLine("/{0}", new string('*', Width + 1));
                Console.WriteLine("*{0}*", contentLine(heroCard.Title));
                Console.WriteLine("*{0}*", new string(' ', Width));
                Console.WriteLine("*{0}*", contentLine(heroCard.Text));
                Console.WriteLine("{0}/", new string('*', Width + 1));
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Hero card could not be parsed");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        private async static Task RenderAdaptiveCard(Attachment attachment)
        {
            // This is entirely optional, just shows how to render an adaptive card which in a console app isn't really very practical!

            try
            {
                AdaptiveCardParseResult result = AdaptiveCard.FromJson(attachment.Content.ToString());
                
                var adaptiveCard = AdaptiveCard.FromJson(attachment.Content.ToString());
                if (adaptiveCard != null)
                {
                    // Create a host config with no interactivity 
                    // (buttons in images would be deceiving)
                    AdaptiveHostConfig hostConfig = new AdaptiveHostConfig()
                    {
                        SupportsInteractivity = false
                    };

                    // Create a renderer
                    AdaptiveCardRenderer renderer = new AdaptiveCardRenderer(hostConfig);

                    // Render the card to png
                    RenderedAdaptiveCardImage renderedCard = await renderer.RenderCardToImageAsync(adaptiveCard.Card, createStaThread: true, cancellationToken: default(CancellationToken));
                    string fileName = $"{Guid.NewGuid()}.png";
                    using (var fileStream = File.Create(fileName))
                    {
                        renderedCard.ImageStream.Seek(0, SeekOrigin.Begin);
                        renderedCard.ImageStream.CopyTo(fileStream);
                    }

                    Console.WriteLine($"Adaptive Card rendered to {fileName} for debug purposes.");
                    Process.Start(fileName);
                }

                Console.WriteLine($"Adaptive Card Speak Property: {adaptiveCard.Card.Speak}");
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Adaptive card not parsed");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}
