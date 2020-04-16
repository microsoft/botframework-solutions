namespace ITSMSkill.Dialogs.Teams.View
{
    using System.Collections.Generic;
    using AdaptiveCards;
    using ITSMSkill.Dialogs.Teams;
    using ITSMSkill.Extensions.Teams.TaskModule;
    using Microsoft.Bot.Schema;

    public class RenderCreateIncidentHelper
    {
        public static TaskEnvelope GetUserInput()
        {
            var response = new TaskEnvelope
            {
                Task = new TaskProperty()
                {
                    Type = "continue",
                    TaskInfo = new TaskInfo()
                    {
                        Title = "ImpactTracker",
                        Height = "medium",
                        Width = 500,
                        Card = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = TicketDialogHelper.CreateIncidentAdaptiveCard()
                        }
                    }
                }
            };

            return response;
        }

        public static TaskEnvelope IncidentAddFailed()
        {
            var response = new TaskEnvelope
            {
                Task = new TaskProperty()
                {
                    Type = "continue",
                    TaskInfo = new TaskInfo()
                    {
                        Title = "Incident Create Failed",
                        Height = "medium",
                        Width = 500,
                        Card = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = ImpactTrackerResponseCard("Incident Create Failed")
                        }
                    }
                }
            };

            return response;
        }

        public static TaskEnvelope ImpactAddEnvelope()
        {
            var response = new TaskEnvelope
            {
                Task = new TaskProperty()
                {
                    Type = "continue",
                    TaskInfo = new TaskInfo()
                    {
                        Title = "Incident Added",
                        Height = "small",
                        Width = 500,
                        Card = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = ImpactTrackerResponseCard("Incident has been created")
                        }
                    }
                }
            };

            return response;
        }

        /// <returns>Adaptive Card.</returns>
        public static AdaptiveCard ImpactTrackerResponseCard(string trackerResponse)
        {
            var card = new AdaptiveCard("1.0");

            var columns = new List<AdaptiveColumn>
            {
                new AdaptiveColumn
                {
                    VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center,
                    Items = new List<AdaptiveElement>
                        {
                            new AdaptiveTextBlock
                            {
                                Text = trackerResponse,
                                Size = AdaptiveTextSize.Small,
                                Weight = AdaptiveTextWeight.Bolder,
                                Color = AdaptiveTextColor.Accent,
                                Wrap = true
                            }
                        }
                }
            };

            return card;
        }
    }
}
