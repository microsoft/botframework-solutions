namespace ITSMSkill.Dialogs.Teams
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveCards;
    using ITSMSkill.Extensions.Teams;
    using ITSMSkill.Extensions.Teams.TaskModule;
    using ITSMSkill.Models;
    using ITSMSkill.Models.UpdateActivity;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;

    /// <summary>
    /// Helper to update Teams Activity
    /// </summary>
    public class UpdateActivityHelper
    {
        public static async Task UpdateTaskModuleActivityAsync(
            ITurnContext context,
            ActivityReference activityReference,
            Ticket details,
            IConnectorClient connectorClient,
            CancellationToken cancellationToken,
            bool isGroupTaskModule = false)
        {
            Activity reply = context.Activity.CreateReply();
            reply.Attachments = new List<Microsoft.Bot.Schema.Attachment>
                {
                    new Microsoft.Bot.Schema.Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = GetIncidentCard(details),
                    },
                };

            var teamsChannelActivity = reply.CreateConversationToTeamsChannel(
                new TeamsChannelData
                {
                    Channel = new ChannelInfo(id: activityReference.ThreadId),
                });

            await connectorClient.Conversations.UpdateActivityAsync(
                activityReference.ThreadId,
                activityReference.ActivityId,
                teamsChannelActivity,
                cancellationToken);
        }

        private static AdaptiveCard GetIncidentCard(Ticket details)
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
                                                Text = $"Title: {details.Title}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                // Incase of IcmForwarder, Triggers do not have incidentUrl hence being explicit here
                                                Text = $"Urgency: {details.Urgency}",
                                                Color = AdaptiveTextColor.Good,
                                                MaxLines = 1,
                                                Weight = AdaptiveTextWeight.Bolder,
                                                Size = AdaptiveTextSize.Large
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Description: {details.Description}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            card.Actions.Add(new AdaptiveSubmitAction()
            {
                Title = "Update Incident",
                Data = new AdaptiveCardValue<TaskModuleMetadata>()
                {
                    Data = new TaskModuleMetadata()
                    {
                        TaskModuleFlowType = TeamsFlowType.CreateTicket_Form.ToString(),
                        FlowData = new Dictionary<string, object>
                        {
                            { "IncidentDetails", details }
                        },
                        Submit = true
                    }
                }
            });

            card.Actions.Add(new AdaptiveSubmitAction()
            {
                Title = "Delete Incident",
                Data = new AdaptiveCardValue<TaskModuleMetadata>()
                {
                    Data = new TaskModuleMetadata()
                    {
                        TaskModuleFlowType = TeamsFlowType.CreateTicket_Form.ToString(),
                        FlowData = new Dictionary<string, object>
                        {
                            { "IncidentId", details.Number }
                        },
                        Submit = true
                    }
                }
            });

            return card;
        }
    }
}
