using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using VirtualAssistantSample.Responses.Main;
using AdaptiveCards;
using Microsoft.Bot.Builder.Solutions.Responses;
using VirtualAssistantSample.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Authentication;

namespace VirtualAssistantSample.Dialogs
{
    public class DeviceStartDialog : ComponentDialog
    {
        private readonly SkillDialog todoSkillDialog;
        private readonly SkillDialog poiSkillDialog;
        private readonly SkillDialog calendarDialog;
        private readonly SkillDialog emailDialog;
        private IStatePropertyAccessor<SkillContext> _skillContextAccessor;
        private ResponseManager _responseManager;
        private List<Activity> summaryList;
        public DeviceStartDialog(
            List<SkillDialog> skillDialogs,
            UserState userState,
            ResponseManager responseManager)
            : base(nameof(DeviceStartDialog))
        {
            _skillContextAccessor = userState.CreateProperty<SkillContext>(nameof(SkillContext));

            if (skillDialogs == null || skillDialogs.Count == 0)
            {
                throw new ArgumentNullException(nameof(skillDialogs));
            }

            todoSkillDialog = skillDialogs.Find(s => s.Id == "toDoSkill");
            poiSkillDialog = skillDialogs.Find(s => s.Id == "pointOfInterestSkill");
            calendarDialog = skillDialogs.Find(s => s.Id == "calendarSkill");
            emailDialog = skillDialogs.Find(s => s.Id == "emailSkill");


            _responseManager = responseManager;

            summaryList = new List<Activity>();
            var waterfall = new WaterfallStep[]
            {
                CheckReminderCalendar,
                SaveSummaries,
                CheckReminderEmail,
                SaveSummaries,
                CheckReminderTodo,
                SaveSummaries,
                ShowReminderCard
            };

            AddDialog(todoSkillDialog);
            AddDialog(poiSkillDialog);
            AddDialog(calendarDialog);
            AddDialog(new WaterfallDialog("deviceStartDialog", waterfall));

            InitialDialogId = "deviceStartDialog";
        }

        protected async Task<DialogTurnResult> CheckReminderTodo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(todoSkillDialog.Id);
        }

        protected async Task<DialogTurnResult> CheckReminderCalendar(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(calendarDialog.Id);
        }

        protected async Task<DialogTurnResult> CheckReminderEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(emailDialog.Id);
        }

        protected async Task<DialogTurnResult> SaveSummaries(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (sc.Result != null)
                {
                    summaryList.Add(((List<Activity>)sc.Result)[0]);
                    return await sc.NextAsync();
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

        protected async Task<DialogTurnResult> ShowReminderCard(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (summaryList.Count > 0)
                {
                    var totalEventCount = 0;
                    var totalEventKinds = 0;
                    var totalShowEventCount = 0;
                    var eventItemList = new List<Card>();

                    foreach (var result in summaryList)
                    {
                        var totalCount = 0;
                        var showCount = 0;
                        var entities = result.SemanticAction.Entities;
                        var name = result.Name;
                        JArray items = JArray.FromObject(entities[name].Properties["items"]);

                        if (items.Count <= 0)
                        {
                            continue;
                        }

                        List<string> summaries = new List<string>();
                        foreach (var item in items)
                        {
                            var index = items.IndexOf(item);
                            if (index < 3)
                            {
                                summaries.Add((index + 1) + ": " + item["title"]);
                                showCount += 1;
                            }
                        }

                        totalCount = showCount;
                        if (entities[name].Properties.ContainsKey("totalCount"))
                        {
                            totalCount = Convert.ToInt32(entities[name].Properties["totalCount"]);
                        }

                        var indicator = showCount + "/" + totalCount;
                        string titles = string.Join("\r\n", summaries);
                        eventItemList.Add(new Card()
                        {
                            Name = "SummaryItem",
                            Data = new SummaryItemCardData
                            {
                                Head = name,
                                HeadColor = "Dark",
                                Title = titles,
                                Location = null,
                                Indicator = indicator,
                                IsSubtle = true
                            }
                        });

                        totalShowEventCount += showCount;
                        totalEventCount += totalCount;
                        totalEventKinds += 1;
                    }

                    var overviewIndicator = totalShowEventCount + "/" + totalEventCount;
                    var overviewCard = new Card()
                    {
                        Name = "SummaryCard",
                        Data = new SummaryCardData()
                        {
                            Title = "Your Schedules",
                            TotalEventKinds = totalEventKinds.ToString(),
                            TotalEventCount = totalEventCount.ToString(),
                            TotalEventKindUnit = "kinds of event",
                            TotalEventCountUnit = "number of event",
                            Provider = "Microsoft Graph",
                            Indicator = overviewIndicator
                        }
                    };

                    var response = _responseManager.GetCardResponse(null, overviewCard, null, "EventItemContainer", eventItemList);
                    await sc.Context.SendActivityAsync(response);

                    return new DialogTurnResult(DialogTurnStatus.Complete);
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