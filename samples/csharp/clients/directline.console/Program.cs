// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;
using WebSocketSharp;
using Activity = Microsoft.Bot.Connector.DirectLine.Activity;

namespace DirectLine.Console
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
            var creds = new DirectLineClientCredentials(botDirectLineSecret);
            var client = new DirectLineClient(creds);

            var conversation = await client.Conversations.StartConversationAsync();

            using (var webSocketClient = new WebSocket(conversation.StreamUrl))
            {
                webSocketClient.OnMessage += WebSocketClient_OnMessage;
                webSocketClient.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                webSocketClient.Connect();

                // Optional, helps provide additional context on the user for some skills/scenarios
                await SendStartupEvents(client, conversation);

                while (true)
                {
                    var input = System.Console.ReadLine().Trim();

                    if (input.ToLower() == "exit")
                    {
                        break;
                    }
                    else
                    {
                        if (input.Length > 0)
                        {
                            var userMessage = new Activity
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
        private static async Task SendStartupEvents(DirectLineClient client, Conversation conversation)
        {
            var locationEvent = new Activity
            {
                Name = "VA.Location",
                From = new ChannelAccount(fromUserId, fromUserName),
                Type = ActivityTypes.Event,
                Value = "47.659291, -122.140633"
            };

            await client.Conversations.PostActivityAsync(conversation.ConversationId, locationEvent);

            var timezoneEvent = new Activity
            {
                Name = "VA.Timezone",
                From = new ChannelAccount(fromUserId, fromUserName),
                Type = ActivityTypes.Event,
                Value = "Pacific Standard Time"
            };

            await client.Conversations.PostActivityAsync(conversation.ConversationId, timezoneEvent);
        }

        private static async void WebSocketClient_OnMessage(object sender, MessageEventArgs e)
        {
            // Occasionally, the Direct Line service sends an empty message as a live ping test. Ignore these messages.
            if (string.IsNullOrWhiteSpace(e.Data))
            {
                return;
            }

            var activitySet = JsonConvert.DeserializeObject<ActivitySet>(e.Data);
            var activities = from x in activitySet.Activities
                             where x.From.Id == botId
                             select x;

            foreach (var activity in activities)
            {
                switch (activity.Type)
                {
                    case ActivityTypes.Message:

                        // No visual cards so let's use Speak property to be more descriptive.
                        System.Console.WriteLine(activity.Speak ?? activity.Text ?? "No activity text found");

                        // Do we have any attachments on this message?
                        if (activity.Attachments != null && activity.Attachments.Count > 0)
                        {
                            // There could be multiple attachments (e.g. carousel) but we only want one for the Speech variant
                            var attachment = activity.Attachments.First<Attachment>();

                            switch (attachment.ContentType)
                            {
                                case "application/vnd.microsoft.card.hero":
                                case "application/vnd.microsoft.card.oauth":
                                    System.Console.WriteLine(activity.Speak ?? activity.Text);

                                    // Show how to open up the card in case it's needed.
                                    RenderHeroCard(attachment);
                                    break;
                                case "application/vnd.microsoft.card.adaptive":
                                    // Show how to open up the card in case it's needed.
                                    await RenderAdaptiveCard(attachment);
                                    break;
                                case "image/png":
                                    System.Console.WriteLine($"Opening the requested image '{attachment.ContentUrl}'");
                                    Process.Start(attachment.ContentUrl);
                                    break;
                            }
                        }

                        // If input is being accepted show prompt
                        if (activity.InputHint == InputHints.ExpectingInput || activity.InputHint == InputHints.AcceptingInput)
                        {
                            System.Console.Write("Message> ");
                        }

                        break;
                    case ActivityTypes.Event:
                        System.Console.ForegroundColor = ConsoleColor.Magenta;
                        System.Console.WriteLine($"* Received {activity.Name} event from the Virtual Assistant. * ");
                        System.Console.ForegroundColor = ConsoleColor.Gray;
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
                System.Console.WriteLine("/{0}", new string('*', Width + 1));
                System.Console.WriteLine("*{0}*", contentLine(heroCard.Title));
                System.Console.WriteLine("*{0}*", new string(' ', Width));
                System.Console.WriteLine("*{0}*", contentLine(heroCard.Text));
                System.Console.WriteLine("{0}/", new string('*', Width + 1));
            }
            else
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("Hero card could not be parsed");
                System.Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        private static async Task RenderAdaptiveCard(Attachment attachment)
        {
            // Adaptive Cards cannot be rendered in a console app, so display the Speak property if available
            try
            {
                var result = AdaptiveCard.FromJson(attachment.Content.ToString());

                var adaptiveCard = AdaptiveCard.FromJson(attachment.Content.ToString());
                if (adaptiveCard != null)
                {
                    System.Console.WriteLine($"Adaptive Card Speak Property: {adaptiveCard.Card.Speak}");
                }

            }
            catch (Exception)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("Adaptive card not parsed");
                System.Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}