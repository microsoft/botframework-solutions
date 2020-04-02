namespace ITSMSkill.Dialogs.Teams
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AdaptiveCards;
    using ITSMSkill.Extensions.Teams;
    using ITSMSkill.Extensions.Teams.TaskModule;
    using ITSMSkill.Utilities;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;

    public class TicketDialogHelper
    {
        public static AdaptiveCard CreateIncidentAdaptiveCard()
        {
            try
            {
                // Json Card for creating incident
                AdaptiveCard adaptiveCard = AdaptiveCardHelper.GetCardFromJson("Dialogs/Teams/Resources/CreateIncident.json");

                adaptiveCard.Actions.Add(new AdaptiveSubmitAction()
                {
                    Title = "SubmitIncident",
                    Data = new AdaptiveCardValue<TaskModuleMetadata>()
                    {
                        Data = new TaskModuleMetadata()
                        {
                            TaskModuleFlowType = TeamsFlowType.CreateTicket_Form.ToString(),
                            Submit = true
                        }
                    }
                });

                return adaptiveCard;
            }
            catch (AdaptiveSerializationException e)
            {
                // handle JSON parsing error
                // or, re-throw
                throw;
            }
        }

        // <returns> Adaptive Card.</returns>
        public static AdaptiveCard ServiceNowTickHubAdaptiveCard()
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
                                Text = "Please Click Create Ticket To Create New Incident",
                                Size = AdaptiveTextSize.Small,
                                Weight = AdaptiveTextWeight.Bolder,
                                Color = AdaptiveTextColor.Accent,
                                Wrap = true
                            }
                        },
                }
            };

            var columnSet = new AdaptiveColumnSet
            {
                Columns = columns,
                Separator = true
            };

            var list = new List<AdaptiveElement>
            {
                columnSet
            };

            card.Body.AddRange(list);
            card?.Actions.Add(new AdaptiveSubmitAction()
            {
                Title = "Create Ticket",
                Data = new AdaptiveCardValue<TaskModuleMetadata>()
                {
                    Data = new TaskModuleMetadata()
                    {
                        TaskModuleFlowType = TeamsFlowType.CreateTicket_Form.ToString()
                    }
                }
            });

            return card;
        }
    }
}