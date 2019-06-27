using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace VirtualAssistantSample.Dialogs
{
	public class DeviceStartDialog : ComponentDialog
	{
		private readonly SkillDialog todoSkillDialog;
		private readonly SkillDialog poiSkillDialog;
		private IStatePropertyAccessor<SkillContext> _skillContextAccessor;

		public DeviceStartDialog(
			List<SkillDialog> skillDialogs,
			UserState userState)
			: base(nameof(DeviceStartDialog))
		{
			_skillContextAccessor = userState.CreateProperty<SkillContext>(nameof(SkillContext));

			if (skillDialogs == null || skillDialogs.Count == 0)
			{
				throw new ArgumentNullException(nameof(skillDialogs));
			}

			todoSkillDialog = skillDialogs.Find(s => s.Id == "toDoSkill");
			poiSkillDialog = skillDialogs.Find(s => s.Id == "pointOfInterestSkill");

			var waterfall = new WaterfallStep[]
			{
				CheckReminder,
				FindPOI
			};

			AddDialog(todoSkillDialog);
			AddDialog(poiSkillDialog);
			AddDialog(new WaterfallDialog("deviceStartDialog", waterfall));

			InitialDialogId = "deviceStartDialog";
		}

		protected async Task<DialogTurnResult> CheckReminder(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
		{
			return await sc.BeginDialogAsync(todoSkillDialog.Id);
		}

		protected async Task<DialogTurnResult> FindPOI(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
		{
			try
			{
				if (sc.Result != null)
				{
					var result = ((List<Activity>)sc.Result)[0];
					var entities = result.SemanticAction.Entities;
					await sc.Context.SendActivityAsync($"Don't forget to get {entities["reminders"].Properties["reminder"]} on your way home.");

					var skillContext = await _skillContextAccessor.GetAsync(sc.Context, () => new SkillContext());

					// VA knows user's location (from user profile)
					dynamic location = new JObject();
					location.Latitude = 47.623325;
					location.Longitude = -122.310920;

					if (skillContext.ContainsKey("location"))
					{
						skillContext["location"] = location;
					}
					else
					{
						skillContext.Add("location", location);
					}

					// VA knows this is about grocery so keyword is Safeway
					dynamic keyword = new JObject();
					keyword.Keyword = "Safeway";
					if (skillContext.ContainsKey("keyword"))
					{
						skillContext["keyword"] = keyword;
					}
					else
					{
						skillContext.Add("keyword", keyword);
					}

					return await sc.BeginDialogAsync(poiSkillDialog.Id);
				}
				else
				{
					return new DialogTurnResult(DialogTurnStatus.Complete);
				}
			}
			catch
			{
				return new DialogTurnResult(DialogTurnStatus.Cancelled);
			}
		}
	}
}