using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Builder.Solutions.Responses;
using VirtualAssistantSample.Models;
using VirtualAssistantSample.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using VirtualAssistantSample.Responses.Summary;

namespace VirtualAssistantSample.Dialogs
{
    public class SummaryDialog : ComponentDialog
    {
        private IStatePropertyAccessor<SummaryState> _summaryStateAccessor;
        private IStatePropertyAccessor<SkillContext> _skillContextAccessor;
        private ResponseManager _responseManager;
        private SummaryState _state;
        private SkillContext _skillContext;
        private BotSettings _settings;

        public SummaryDialog(
            BotSettings settings,
            List<SkillDialog> skillDialogs,
            UserState userState,
            ResponseManager responseManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(SummaryDialog))
        {
            _summaryStateAccessor = userState.CreateProperty<SummaryState>(nameof(SummaryState));
            _skillContextAccessor = userState.CreateProperty<SkillContext>(nameof(SkillContext));
            _responseManager = responseManager;
            _settings = settings;
            TelemetryClient = telemetryClient;

            foreach (var skillDialog in skillDialogs)
            {
                AddDialog(skillDialog);
            }

            var collectSkills = new WaterfallStep[]
            {
                Init
            };

            var collectSummary = new WaterfallStep[]
            {
               GetSummary,
               AfterGetSummary
            };

            var showReminderCard = new WaterfallStep[]
            {
                ShowReminderCard
            };

            AddDialog(new WaterfallDialog(DialogIds.CollectSkills, collectSkills));
            AddDialog(new WaterfallDialog(DialogIds.CollectSummary, collectSummary));
            AddDialog(new WaterfallDialog(DialogIds.ShowReminderCard, showReminderCard));

            InitialDialogId = DialogIds.CollectSkills;
        }

        protected async Task<DialogTurnResult> Init(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                _state = await _summaryStateAccessor.GetAsync(sc.Context, () => new SummaryState());
                _state.Init();
                /*
                var TimeZone = "timezone";
                var timezone = "Pacific Standard Time";
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                var timeZoneObj = new JObject();
                timeZoneObj.Add(TimeZone, JToken.FromObject(tz));

                _skillContext = await _skillContextAccessor.GetAsync(sc.Context, () => new SkillContext());
                if (_skillContext.ContainsKey(TimeZone))
                {
                    _skillContext[TimeZone] = timeZoneObj;
                }
                else
                {
                    _skillContext.Add(TimeZone, timeZoneObj);
                }
                */

                foreach (var skill in _settings.Skills)
                {
                    var actionID = skill.Actions.SingleOrDefault(a => a.Definition.Triggers.Events != null && a.Definition.Triggers.Events.Any(e => e.Name == SummaryEvent.Name))?.Id;
                    if (actionID != null)
                    {
                        _state.SummaryInfos.Add(new SummaryState.SummaryInfo() { ActionIds = actionID, SkillIds = skill.Id, SkillResults = null });
                        break;
                    }
                }

                return await sc.ReplaceDialogAsync(DialogIds.CollectSummary);
            }
            catch
            {
                return new DialogTurnResult(DialogTurnStatus.Cancelled);
            }

        }

        protected async Task<DialogTurnResult> GetSummary(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                _state = await _summaryStateAccessor.GetAsync(sc.Context);
                if (_state.SkillIndex < _state.SummaryInfos.Count)
                {
                    var summaryInfo = _state.SummaryInfos[_state.SkillIndex];
                    SkillDialogOption skillDialogOption = new SkillDialogOption() { Action = summaryInfo.ActionIds };
                    return await sc.BeginDialogAsync(summaryInfo.SkillIds, skillDialogOption);
                }
                else
                {
                    return await sc.ReplaceDialogAsync(DialogIds.ShowReminderCard);
                }
            }
            catch
            {
                return new DialogTurnResult(DialogTurnStatus.Cancelled);
            }
        }

        protected async Task<DialogTurnResult> AfterGetSummary(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                _state = await _summaryStateAccessor.GetAsync(sc.Context);
                if (sc.Result != null)
                {
                    _state.SummaryInfos[_state.SkillIndex].SkillResults = (Dictionary<string, Entity>)sc.Result;
                }

                _state.SkillIndex += 1;
                return await sc.ReplaceDialogAsync(DialogIds.CollectSummary);
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
                _state = await _summaryStateAccessor.GetAsync(sc.Context);

                var eventItemList = new List<Card>();
                foreach (var infos in _state.SummaryInfos)
                {
                    eventItemList = await GetSummaryResult(sc.Context, infos.SkillResults, eventItemList);
                }

                var overviewCard = GetSummaryCard(_state.TotalShowEventCount, _state.TotalEventCount, _state.TotalEventKinds);
                await sc.Context.SendActivityAsync(_responseManager.GetCardResponse(null, overviewCard, null, "EventItemContainer", eventItemList));
                return await sc.EndDialogAsync(true);
            }
            catch
            {
                return new DialogTurnResult(DialogTurnStatus.Cancelled);
            }
        }

        protected async Task<List<Card>> GetSummaryResult(ITurnContext context, Dictionary<string, Entity> results, List<Card> eventItemList)
        {
            var state = await _summaryStateAccessor.GetAsync(context);
            foreach (var entity in results)
            {
                string entityName = entity.Key;
                var result = JsonConvert.DeserializeObject<SummaryResultModel>(entity.Value.Properties.ToString());
                int showCount = 0;
                if (result.Items != null && result.Items.Any())
                {
                    List<string> titles = new List<string>();
                    foreach (var item in result.Items)
                    {
                        var index = result.Items.IndexOf(item);
                        if (index < 3)
                        {
                            titles.Add((index + 1) + ": " + item.Title);
                            showCount += 1;
                        }
                    }

                    int totalCount = result.TotalCount > showCount ? result.TotalCount : showCount;
                    string cardName = result.Name ?? entityName;
                    eventItemList.Add(GetItemCard(cardName, titles, showCount, totalCount));
                    state.TotalShowEventCount += showCount;
                    state.TotalEventCount += totalCount;
                    state.TotalEventKinds += 1;
                }
            }

            return eventItemList;
        }

        private Card GetItemCard(string name, List<string> titles, int showCount, int totalCount)
        {
            string title = string.Join("\r\n", titles);
            var indicator = showCount + "/" + totalCount;
            return new Card()
            {
                Name = SummaryStrings.SUMMARY_ITEM_NAME,
                Data = new SummaryItemCardData
                {
                    Head = name,
                    HeadColor = SummaryStrings.SUMMARY_ITEM_COLOR,
                    Title = title,
                    Indicator = indicator,
                    IsSubtle = true
                }
            };
        }

        private Card GetSummaryCard(int totalShowEventCount, int totalEventCount, int totalEventKinds)
        {
            var overviewIndicator = totalShowEventCount + "/" + totalEventCount;
            return new Card()
            {
                Name = SummaryStrings.SUMMARY_CARD_NAME,
                Data = new SummaryCardData()
                {
                    Title = SummaryStrings.SUMMARY_CARD_TITLE,
                    TotalEventKinds = totalEventKinds.ToString(),
                    TotalEventCount = totalEventCount.ToString(),
                    TotalEventKindUnit = SummaryStrings.EVENT_KINDS_PROMPT,
                    TotalEventCountUnit = SummaryStrings.EVENT_COUNT_PROMPT,
                    Indicator = overviewIndicator
                }
            };
        }

        private class DialogIds
        {
            public const string CollectSkills = "collectSkills";
            public const string CollectSummary = "collectSummary";
            public const string ShowReminderCard = "showReminderCard";
        }

        private class SummaryEvent
        {
            public const string Name = "SummaryEvent";
        }
    }
}