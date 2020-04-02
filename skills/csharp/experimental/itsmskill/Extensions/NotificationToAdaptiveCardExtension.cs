namespace ITSMSkill.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AdaptiveCards;
    using ITSMSkill.Models.ServiceNow;

    public static class NotificationToAdaptiveCardExtension
    {
        /// <summary>
        /// Creates AdaptiveCard from Notification.
        /// </summary>
        /// <param name="serviceNowNotification">ServiceNow Notification.</param>
        /// <returns>AdaptiveCard.</returns>
        public static AdaptiveCard ToAdaptiveCard(this ServiceNowNotification serviceNowNotification)
        {
            var card = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveContainer
                    {
                        Items = new List<AdaptiveElement>
                        {
                            new AdaptiveColumnSet
                            {
                                Columns = new List<AdaptiveColumn>
                                {
                                    new AdaptiveColumn
                                    {
                                        Width = AdaptiveColumnWidth.Stretch,
                                        Items = new List<AdaptiveElement>
                                        {
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Title: {serviceNowNotification.Title}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Urgency: {serviceNowNotification.Urgency}",
                                                Color = AdaptiveTextColor.Good,
                                                MaxLines = 1,
                                                Weight = AdaptiveTextWeight.Bolder,
                                                Size = AdaptiveTextSize.Large
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Description: {serviceNowNotification.Description}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Impact: {serviceNowNotification.Impact}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            return card;
        }
    }
}
