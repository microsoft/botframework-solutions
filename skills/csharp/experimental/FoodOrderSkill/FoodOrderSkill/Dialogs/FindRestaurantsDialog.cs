using FoodOrderSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FoodOrderSkill.Dialogs
{
    public class FindRestaurantsDialog : SkillDialogBase
    {
        public TakeAwayService _takeAwayService;

        public FindRestaurantsDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient,
            TakeAwayService takeAwayService)
            : base(nameof(FindRestaurantsDialog), serviceProvider, telemetryClient)
        {
            var steps = new WaterfallStep[]
            {
                RenderNearbyRestaurants
            };

            _takeAwayService = takeAwayService;

            AddDialog(new WaterfallDialog(nameof(FindRestaurantsDialog), steps));
            AddDialog(new ChoicePrompt(DialogIds.RenderNearbyRestaurants));
            InitialDialogId = nameof(FindRestaurantsDialog);
        }

        public async Task<DialogTurnResult> RenderNearbyRestaurants (WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            string neardyRestaurantsRaw = await _takeAwayService.getRestaurants();
            XmlSerializer serializer = new XmlSerializer(typeof(getRestaurants));
            StringReader rdr = new StringReader(neardyRestaurantsRaw);
            getRestaurants takeAwayRestaurants = (getRestaurants)serializer.Deserialize(rdr);


            return null;
        }

        private class DialogIds
        {
            public const string RenderNearbyRestaurants = "renderNearbyRestaurants";
        }
    }
}
