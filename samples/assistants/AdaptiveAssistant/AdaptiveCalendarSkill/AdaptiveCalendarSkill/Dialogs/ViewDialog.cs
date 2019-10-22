using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCalendarSkill.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.LanguageGeneration.Generators;
using Microsoft.Bot.Builder.LanguageGeneration.Templates;
using Microsoft.Bot.Builder.Solutions.Authentication;

namespace AdaptiveCalendarSkill.Dialogs
{
    public class ViewDialog : ComponentDialog
    {
        private int _displayCount = 5;

        public ViewDialog(
            BotServices services,
            MultiProviderAuthDialog oauthDialog)
            : base(nameof(ViewDialog))
        {
            var showMeetingsDialog = new AdaptiveDialog("showMeetingsDialog")
            {
                Recognizer = services.CognitiveModelSets["en"].LuisRecognizers["Calendar"],
                Generator = new ResourceMultiLanguageGenerator("ViewDialog.lg"),
                AutoEndDialog = false,
                Triggers =
                {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            new BeginDialog(oauthDialog.Id)
                            {
                                ResultProperty = "user.token"
                            },
                            new SetProperty()
                            {
                                Property = "dialog.currentDate",
                                Value = $"formatDateTime(\'{DateTime.Today.ToString("yyyy-MM-ddTHH:mm:ss")}\')"
                            },
                            new HttpRequest()
                            {
                                ResultProperty = "dialog.meetingListResult",
                                Url = "https://graph.microsoft.com/v1.0/me/calendarview?isallday=false&startdatetime={dialog.currentDate}&enddatetime={addDays(dialog.currentDate, 1)}&orderby=start/datetime DESC",
                                Method = HttpRequest.HttpMethod.GET,
                                ResponseType = HttpRequest.ResponseTypes.Json,
                                Headers = new Dictionary<string, string>()
                                {
                                    ["Authorization"] = "Bearer {user.token.tokenResponse.token}",
                                },
                            },
                            new TraceActivity(),
                            new IfCondition()
                            {
                                Condition = "dialog.meetingListResult.content.value != null && count(dialog.meetingListResult.content.value) > 0",
                                Actions =
                                {
                                    new IfCondition()
                                    {
                                        Condition = $"(conversation.meetingListPage * {_displayCount} + {_displayCount}) < count(dialog.meetingListResult.content.value)",
                                        Actions =
                                        {
                                            new SendActivity($"[MeetingList(dialog.meetingListResult.content.value, conversation.meetingListPage * {_displayCount}, conversation.meetingListPage * {_displayCount} + {_displayCount})]"),
                                        },
                                        ElseActions =
                                        {
                                            new SendActivity($"[MeetingList(dialog.meetingListResult.content.value, conversation.meetingListPage * {_displayCount}, count(dialog.meetingListResult.content.value))]"),
                                        }
                                    },
                                    new SendActivity($"[Meeting-List-Actions({_displayCount})]")
                                },
                                ElseActions =
                                {
                                    new SendActivity("[NoEntries]"),
                                    new EndDialog()
                                }
                            }
                        }
                    },
                    new OnIntent("Help")
                    {
                        Actions =
                        {
                            new SendActivity("[HelpViewMeeting]")
                        }
                    },
                    new OnIntent("Cancel")
                    {
                        Actions =
                        {
                            new SendActivity("[CancelViewMeeting]"),
                            new CancelAllDialogs()
                        }
                    },
                    new OnIntent("ShowPrevious")
                    {
                        Actions =
                        {
                            new IfCondition()
                            {
                                Condition = " 0 < conversation.meetingListPage",
                                Actions =
                                {
                                    new SetProperty()
                                    {
                                        Property = "conversation.meetingListPage",
                                        Value = "conversation.meetingListPage - 1"
                                    },
                                    new RepeatDialog()
                                },
                                ElseActions =
                                {
                                    new SendActivity("[FirstPage]"),
                                }
                            }
                        }
                    },
                    new OnIntent("ShowNext")
                    {
                        Actions =
                        {
                            new IfCondition()
                            {
                                Condition = $"conversation.meetingListPage * {_displayCount} + {_displayCount} < count(dialog.meetingListResult.content.value)"
                                            + $"|| count(dialog.meetingListResult.content.value) % {_displayCount} > 0",
                                Actions =
                                {
                                    new SetProperty()
                                    {
                                        Property = "conversation.meetingListPage",
                                        Value = "conversation.meetingListPage + 1"
                                    },
                                    new RepeatDialog()
                                },
                                ElseActions =
                                {
                                    new SendActivity("[LastPage]")
                                }
                            }
                        }
                    },
                    new OnIntent("SelectItem")
                    {
                        Actions =
                        {
                            new SetProperty()
                            {
                                Value = "@ordinal",
                                Property = "turn.selectMeeting_ordinal"
                            },
                            new SetProperty()
                            {
                                Value = "@number",
                                Property = "turn.selectMeeting_number"
                            },
                            new IfCondition()
                            {
                                Condition = "turn.selectMeeting_ordinal != null",
                                Actions =
                                {   
                                    new SetProperty()
                                    {
                                        Property = "dialog.selectIndex",
                                        Value = $"int(conversation.meetingListPage * {_displayCount}) + int(turn.selectMeeting_ordinal - 1)"
                                    },
                                    new IfCondition()
                                    {
                                        Condition = "dialog.meetingListResult.content.value[dialog.selectIndex] != null",
                                        Actions =
                                        {
                                            new SendActivity("[MeetingDetail(dialog.meetingListResult.content.value[dialog.selectIndex])]"),
                                            new SetProperty()
                                            {
                                                Property = "conversation.focusedMeeting",
                                                Value = "dialog.meetingListResult.content.value[dialog.selectIndex]"
                                            }
                                        },
                                        ElseActions =
                                        {
                                            new SendActivity("[ViewEmptyEntry]"),
                                        }
                                    },
                                },
                                ElseActions =
                                {
                                    new IfCondition()
                                    {
                                        // prevent "select the third one"
                                        Condition = "turn.selectMeeting_number != null",
                                        Actions =
                                        {
                                            new SetProperty()
                                            {
                                                Property = "dialog.selectIndex",
                                                Value = $"int(conversation.meetingListPage * {_displayCount}) + int(turn.selectMeeting_number - 1)"
                                            },
                                            new IfCondition()
                                            {
                                                Condition = "dialog.meetingListResult.content.value[dialog.selectIndex] != null",
                                                Actions =
                                                {
                                                    new SendActivity("[MeetingDetail(dialog.meetingListResult.content.value[dialog.selectIndex])]"),
                                                    new SetProperty()
                                                    {
                                                        Property = "conversation.focusedMeeting",
                                                        Value = "dialog.meetingListResult.content.value[dialog.selectIndex]"
                                                    }
                                                },
                                                ElseActions =
                                                {
                                                    new SendActivity("[ViewEmptyEntry]"),
                                                }
                                            },
                                        }
                                    },
                                }
                            },
                        }
                    },
                }
            };

            AddDialog(showMeetingsDialog);
            AddDialog(oauthDialog);
        }

        protected override Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            return base.EndComponentAsync(outerDc, result, cancellationToken);
        }
    }
}