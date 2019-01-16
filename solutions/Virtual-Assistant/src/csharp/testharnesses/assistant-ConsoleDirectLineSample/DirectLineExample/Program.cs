// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using AdaptiveCards;
using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WebSocketSharp;

namespace DirectLineExample
{
    /// <summary>
    /// A sample Application to demonstrate the use of DirectLine (WebSockets) to communicate with a Virtual Assistant
    /// It demonstrates the ability to send and receive Events as well as process responses including Adaptive Cards
    /// This simple example provides a clear demonstration of the basic communciation required to embed a Custom Assistant
    /// within a device.
    /// </summary>
    class Program
    {
        // Set this to the Secret for the Bot you wish to communicate with
        private static string botDirectLineSecret = "";
        private static string botId = "";
        private static string fromUser = "";

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

                // The Contoso Assistant Bot requires a "startup" event to get going, this is NOT needed for other bots
                await SendStartupEvent(client, conversation);

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
                                From = new ChannelAccount(fromUser),
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
        private async static Task SendStartupEvent(DirectLineClient client, Conversation conversation)
        {
            Activity startupEvent = new Activity
            {
                Name = "startConversation",
                From = new ChannelAccount(fromUser),
                Type = ActivityTypes.Event,
                Locale = "en-us"
            };

            await client.Conversations.PostActivityAsync(conversation.ConversationId, startupEvent);

            Activity locationEvent = new Activity
            {
                Name = "IPA.Location",
                From = new ChannelAccount(fromUser),
                Type = ActivityTypes.Event,
                Value = "47.659291, -122.140633"
            };

            await client.Conversations.PostActivityAsync(conversation.ConversationId, locationEvent);          
        }    

        private static void WebSocketClient_OnMessage(object sender, MessageEventArgs e)
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
                        // If we have an attachment / hero card then we use the "Speak" field on the Card not the Activity text
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
                                    RenderAdaptiveCard(attachment);
                                    break;
                                case "image/png":
                                    Console.WriteLine($"Opening the requested image '{attachment.ContentUrl}'");
                                    Process.Start(attachment.ContentUrl);
                                    break;
                            }
                        }
                        else
                        {
                            Console.WriteLine(activity.Text);
                        }
                        break;
                    case ActivityTypes.Event:

                        Console.WriteLine($"* Received a {activity.Name} event from the Custom Assistant.");
                        break;
                }

                Console.Write("Message> ");
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
        }

        private static void RenderAdaptiveCard(Attachment attachment)
        {
            try
            {
                AdaptiveCardParseResult result = AdaptiveCard.FromJson(attachment.Content.ToString());
                
                var adaptiveCard = AdaptiveCard.FromJson(attachment.Content.ToString());
                Console.WriteLine(adaptiveCard.Card.Speak);
            }
            catch (Exception e)
            {
                Console.WriteLine("Adaptive card not parsed");
            }
        }

        public class DeviceInformation
        {
            [JsonProperty(PropertyName = "deviceId")]
            public string DeviceId { get; set; }
            // Add other information that you need to send
        }    
    }
}
