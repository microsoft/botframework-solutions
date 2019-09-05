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
using Microsoft.Bot.Builder.Solutions.Responses;
using VirtualAssistantSample.Models;

namespace VirtualAssistantSample.Dialogs
{
    public class SummaryDialog : ComponentDialog
    {
        private readonly SkillDialog todoSkillDialog;
        private readonly SkillDialog calendarDialog;
        private readonly SkillDialog emailDialog;
        private IStatePropertyAccessor<SkillContext> _skillContextAccessor;
        private ResponseManager _responseManager;
        private List<Dictionary<string, Entity>> _entitiesList;
        public SummaryDialog(
            List<SkillDialog> skillDialogs,
            UserState userState,
            ResponseManager responseManager)
            : base(nameof(SummaryDialog))
        {

            _skillContextAccessor = userState.CreateProperty<SkillContext>(nameof(SkillContext));

            if (skillDialogs == null || skillDialogs.Count == 0)
            {
                throw new ArgumentNullException(nameof(skillDialogs));
            }

            todoSkillDialog = skillDialogs.Find(s => s.Id == "toDoSkill");
            calendarDialog = skillDialogs.Find(s => s.Id == "calendarSkill");
            emailDialog = skillDialogs.Find(s => s.Id == "emailSkill");

            _responseManager = responseManager;
            _entitiesList = new List<Dictionary<string, Entity>>();
            var waterfall = new WaterfallStep[]
            {
                CheckReminderCalendar,
                CheckReminderEmail,
                CheckReminderTodo,
                ShowReminderCard
            };

            AddDialog(todoSkillDialog);
            AddDialog(emailDialog);
            AddDialog(calendarDialog);
            AddDialog(new WaterfallDialog("deviceStartDialog", waterfall));

            InitialDialogId = "deviceStartDialog";
        }

        protected async Task<DialogTurnResult> CheckReminderTodo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (sc.Result != null)
            {
                _entitiesList.Add((Dictionary<string, Entity>)sc.Result);
            }

            SkillDialogOption skillDialogOption = new SkillDialogOption() { Action = "toDoSkill_summary" };
            return await sc.BeginDialogAsync(todoSkillDialog.Id, skillDialogOption);
        }

        protected async Task<DialogTurnResult> CheckReminderCalendar(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (sc.Result != null)
            {
                _entitiesList.Add((Dictionary<string, Entity>)sc.Result);
            }

            SkillDialogOption skillDialogOption = new SkillDialogOption() { Action = "calendarskill_summary" };
            return await sc.BeginDialogAsync(calendarDialog.Id, skillDialogOption);
        }

        protected async Task<DialogTurnResult> CheckReminderEmail(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (sc.Result != null)
            {
                _entitiesList.Add((Dictionary<string, Entity>)sc.Result);
            }

            SkillDialogOption skillDialogOption = new SkillDialogOption() { Action = "emailskill_summary" };
            return await sc.BeginDialogAsync(emailDialog.Id, skillDialogOption);
        }

        protected async Task<DialogTurnResult> ShowReminderCard(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (sc.Result != null)
                {
                    _entitiesList.Add((Dictionary<string, Entity>)sc.Result);
                }

                if (_entitiesList.Count > 0)
                {
                    var totalEventCount = 0;
                    var totalEventKinds = 0;
                    var totalShowEventCount = 0;
                    var eventItemList = new List<Card>();

                    foreach (var entities in _entitiesList)
                    {
                        var totalCount = 0;
                        var showCount = 0;
                        JArray items = JArray.FromObject(entities["summary"].Properties["items"]);

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
                        if (entities["summary"].Properties.ContainsKey("totalCount"))
                        {
                            totalCount = Convert.ToInt32(entities["summary"].Properties["totalCount"]);
                        }

                        var indicator = showCount + "/" + totalCount;
                        string titles = string.Join("\r\n", summaries);
                        eventItemList.Add(new Card()
                        {
                            Name = "SummaryItem",
                            Data = new SummaryItemCardData
                            {
                                Head = Convert.ToString(entities["summary"].Properties["name"]),
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

                    return await sc.EndDialogAsync(true);
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